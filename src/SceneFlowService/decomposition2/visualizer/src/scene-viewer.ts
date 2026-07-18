import * as THREE from "three";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";
import { Bounds, SceneData, VisualObject, collectSubtreeObjects } from "./data";

export type DisplayMode = "all" | "selected-only" | "dim-unselected";

const PALETTE = [
  new THREE.Color("#2563eb"),
  new THREE.Color("#16a34a"),
  new THREE.Color("#dc2626"),
  new THREE.Color("#ca8a04"),
  new THREE.Color("#7c3aed"),
  new THREE.Color("#0891b2"),
  new THREE.Color("#db2777"),
  new THREE.Color("#4f46e5"),
  new THREE.Color("#65a30d"),
  new THREE.Color("#ea580c"),
];

export class SceneViewer {
  private readonly container: HTMLElement;
  private readonly scene = new THREE.Scene();
  private readonly camera = new THREE.PerspectiveCamera(55, 1, 0.01, 10_000);
  private readonly renderer = new THREE.WebGLRenderer({ antialias: true });
  private readonly controls: OrbitControls;
  private currentData: SceneData | null = null;
  private points: THREE.Points | null = null;
  private boundsHelper: THREE.Box3Helper | null = null;
  private material: THREE.ShaderMaterial | null = null;
  private resizeObserver: ResizeObserver;
  private animationFrame = 0;

  constructor(container: HTMLElement) {
    this.container = container;
    this.scene.background = new THREE.Color("#f6f7f9");
    this.camera.position.set(90, 80, 120);

    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.renderer.setSize(container.clientWidth, container.clientHeight);
    this.container.appendChild(this.renderer.domElement);

    this.controls = new OrbitControls(this.camera, this.renderer.domElement);
    this.controls.enableDamping = true;
    this.controls.dampingFactor = 0.08;
    this.controls.screenSpacePanning = true;

    const ambient = new THREE.AmbientLight("#ffffff", 1);
    this.scene.add(ambient);
    this.scene.add(new THREE.GridHelper(120, 24, "#c6ccd5", "#e2e5ea"));

    this.resizeObserver = new ResizeObserver(() => this.resize());
    this.resizeObserver.observe(container);
    this.resize();
    this.animate();
  }

  dispose(): void {
    cancelAnimationFrame(this.animationFrame);
    this.resizeObserver.disconnect();
    this.controls.dispose();
    this.renderer.dispose();
  }

  setSceneData(data: SceneData): void {
    this.currentData = data;
    this.clearScene();

    const pointCount = data.manifest.pointCount;
    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute("position", new THREE.BufferAttribute(data.points, 3));

    const colors = new Float32Array(pointCount * 3);
    this.fillBaseColors(data, colors);
    geometry.setAttribute("color", new THREE.BufferAttribute(colors, 3));

    const selected = new Float32Array(pointCount);
    geometry.setAttribute("selected", new THREE.BufferAttribute(selected, 1));

    this.material = new THREE.ShaderMaterial({
      uniforms: {
        pointSize: { value: 2.0 },
        displayMode: { value: 2 },
        hasSelection: { value: false },
        selectedColor: { value: new THREE.Color("#facc15") },
        dimColor: { value: new THREE.Color("#a5abb5") },
      },
      vertexShader: `
        attribute vec3 color;
        attribute float selected;
        varying vec3 vColor;
        varying float vSelected;
        uniform float pointSize;

        void main() {
          vColor = color;
          vSelected = selected;
          vec4 mvPosition = modelViewMatrix * vec4(position, 1.0);
          gl_PointSize = pointSize * (280.0 / max(80.0, -mvPosition.z));
          gl_Position = projectionMatrix * mvPosition;
        }
      `,
      fragmentShader: `
        varying vec3 vColor;
        varying float vSelected;
        uniform int displayMode;
        uniform bool hasSelection;
        uniform vec3 selectedColor;
        uniform vec3 dimColor;

        void main() {
          vec2 coord = gl_PointCoord - vec2(0.5);
          if (dot(coord, coord) > 0.25) discard;
          if (displayMode == 1 && hasSelection && vSelected < 0.5) discard;

          vec3 finalColor = vColor;
          if (hasSelection && vSelected > 0.5) {
            finalColor = selectedColor;
          } else if (displayMode == 2 && hasSelection) {
            finalColor = dimColor;
          }
          gl_FragColor = vec4(finalColor, 1.0);
        }
      `,
    });

    this.points = new THREE.Points(geometry, this.material);
    this.scene.add(this.points);
    this.focusBounds(data.manifest.bounds);
  }

  setPointSize(size: number): void {
    if (this.material) {
      this.material.uniforms.pointSize.value = size;
    }
  }

  setDisplayMode(mode: DisplayMode): void {
    if (!this.material) {
      return;
    }
    this.material.uniforms.displayMode.value = mode === "all" ? 0 : mode === "selected-only" ? 1 : 2;
  }

  resetCamera(): void {
    if (this.currentData) {
      this.focusBounds(this.currentData.manifest.bounds);
    }
  }

  selectNode(nodeId: number | null, focus = false): VisualObject[] {
    if (!this.currentData || !this.points || !this.material) {
      return [];
    }

    const selectedAttribute = this.points.geometry.getAttribute("selected") as THREE.BufferAttribute;
    const selected = selectedAttribute.array as Float32Array;
    selected.fill(0);

    let objects: VisualObject[] = [];
    let bounds: Bounds | null = null;
    if (nodeId !== null) {
      const node = this.currentData.nodeById.get(nodeId);
      if (node) {
        objects = collectSubtreeObjects(this.currentData, nodeId);
        bounds = node.bounds;
        for (const object of objects) {
          selected.fill(1, object.pointStart, object.pointStart + object.pointCount);
        }
      }
    }

    selectedAttribute.needsUpdate = true;
    this.material.uniforms.hasSelection.value = nodeId !== null;
    this.showBounds(bounds);
    if (bounds && focus) {
      this.focusBounds(bounds);
    }
    return objects;
  }

  private clearScene(): void {
    if (this.points) {
      this.scene.remove(this.points);
      this.points.geometry.dispose();
      const material = this.points.material;
      if (Array.isArray(material)) {
        material.forEach((entry) => entry.dispose());
      } else {
        material.dispose();
      }
      this.points = null;
    }
    this.showBounds(null);
  }

  private fillBaseColors(data: SceneData, colors: Float32Array): void {
    const topAncestorCache = new Map<number, number>();
    const topAncestor = (nodeId: number): number => {
      const cached = topAncestorCache.get(nodeId);
      if (cached !== undefined) {
        return cached;
      }
      let current = data.nodeById.get(nodeId);
      let candidate = data.rootId;
      while (current && current.parentId !== null) {
        candidate = current.id;
        const parent = data.nodeById.get(current.parentId);
        if (!parent || parent.parentId === null) {
          break;
        }
        current = parent;
      }
      topAncestorCache.set(nodeId, candidate);
      return candidate;
    };

    for (const object of data.objects) {
      const color = PALETTE[Math.abs(topAncestor(object.nodeId)) % PALETTE.length];
      for (let i = object.pointStart; i < object.pointStart + object.pointCount; i += 1) {
        colors[i * 3] = color.r;
        colors[i * 3 + 1] = color.g;
        colors[i * 3 + 2] = color.b;
      }
    }
  }

  private showBounds(bounds: Bounds | null): void {
    if (this.boundsHelper) {
      this.scene.remove(this.boundsHelper);
      this.boundsHelper.dispose();
      this.boundsHelper = null;
    }
    if (!bounds) {
      return;
    }
    const box = new THREE.Box3(
      new THREE.Vector3(...bounds.min),
      new THREE.Vector3(...bounds.max),
    );
    this.boundsHelper = new THREE.Box3Helper(box, "#111827");
    this.scene.add(this.boundsHelper);
  }

  private focusBounds(bounds: Bounds): void {
    const center = new THREE.Vector3(...bounds.center);
    const extents = new THREE.Vector3(...bounds.extents);
    const radius = Math.max(extents.length(), 1);
    const direction = new THREE.Vector3(1, 0.72, 1).normalize();
    this.camera.near = Math.max(0.01, radius / 500);
    this.camera.far = Math.max(1000, radius * 20);
    this.camera.position.copy(center).addScaledVector(direction, radius * 2.6);
    this.camera.updateProjectionMatrix();
    this.controls.target.copy(center);
    this.controls.update();
  }

  private resize(): void {
    const width = Math.max(1, this.container.clientWidth);
    const height = Math.max(1, this.container.clientHeight);
    this.camera.aspect = width / height;
    this.camera.updateProjectionMatrix();
    this.renderer.setSize(width, height, false);
  }

  private animate = (): void => {
    this.animationFrame = requestAnimationFrame(this.animate);
    this.controls.update();
    this.renderer.render(this.scene, this.camera);
  };
}
