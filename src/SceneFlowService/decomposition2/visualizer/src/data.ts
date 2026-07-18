export type Bounds = {
  center: [number, number, number];
  extents: [number, number, number];
  min: [number, number, number];
  max: [number, number, number];
};

export type Manifest = {
  scene: string;
  scenePath?: string;
  bounds: Bounds;
  pointCount: number;
  objectCount: number;
  nodeCount: number;
  maxPoints: number;
  generatedAt: string;
  files: {
    nodes: string;
    objects: string;
    points: string;
  };
};

export type SceneNode = {
  id: number;
  parentId: number | null;
  type: string;
  label: string;
  bounds: Bounds;
  objectIds: string[];
  children: number[];
};

export type VisualObject = {
  id: string;
  nodeId: number;
  bounds: Bounds;
  debugPath: string;
  pointStart: number;
  pointCount: number;
  vertexCount: number;
};

export type SceneData = {
  manifest: Manifest;
  rootId: number;
  nodes: SceneNode[];
  objects: VisualObject[];
  points: Float32Array;
  nodeById: Map<number, SceneNode>;
  objectById: Map<string, VisualObject>;
  objectsByNode: Map<number, VisualObject[]>;
};

async function fetchJson<T>(url: string): Promise<T> {
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`${url}: ${response.status} ${response.statusText}`);
  }
  return (await response.json()) as T;
}

export async function loadSceneData(sceneName: string): Promise<SceneData> {
  const baseUrl = `/data/${sceneName}`;
  const manifest = await fetchJson<Manifest>(`${baseUrl}/manifest.json`);
  const [nodePayload, objectPayload, pointResponse] = await Promise.all([
    fetchJson<{ rootId: number; nodes: SceneNode[] }>(`${baseUrl}/${manifest.files.nodes}`),
    fetchJson<{ objects: VisualObject[] }>(`${baseUrl}/${manifest.files.objects}`),
    fetch(`${baseUrl}/${manifest.files.points}`),
  ]);

  if (!pointResponse.ok) {
    throw new Error(`${manifest.files.points}: ${pointResponse.status} ${pointResponse.statusText}`);
  }

  const pointBuffer = await pointResponse.arrayBuffer();
  const points = new Float32Array(pointBuffer);
  if (points.length !== manifest.pointCount * 3) {
    throw new Error(
      `Point buffer size mismatch: expected ${manifest.pointCount * 3} floats, got ${points.length}`,
    );
  }

  const nodeById = new Map(nodePayload.nodes.map((node) => [node.id, node]));
  const objectById = new Map(objectPayload.objects.map((object) => [object.id, object]));
  const objectsByNode = new Map<number, VisualObject[]>();
  for (const object of objectPayload.objects) {
    const list = objectsByNode.get(object.nodeId) ?? [];
    list.push(object);
    objectsByNode.set(object.nodeId, list);
  }

  return {
    manifest,
    rootId: nodePayload.rootId,
    nodes: nodePayload.nodes,
    objects: objectPayload.objects,
    points,
    nodeById,
    objectById,
    objectsByNode,
  };
}

export function collectSubtreeNodeIds(scene: SceneData, nodeId: number): Set<number> {
  const result = new Set<number>();
  const stack = [nodeId];
  while (stack.length > 0) {
    const currentId = stack.pop();
    if (currentId === undefined || result.has(currentId)) {
      continue;
    }
    result.add(currentId);
    const node = scene.nodeById.get(currentId);
    if (node) {
      stack.push(...node.children);
    }
  }
  return result;
}

export function collectSubtreeObjects(scene: SceneData, nodeId: number): VisualObject[] {
  const nodeIds = collectSubtreeNodeIds(scene, nodeId);
  const objects: VisualObject[] = [];
  for (const currentId of nodeIds) {
    objects.push(...(scene.objectsByNode.get(currentId) ?? []));
  }
  return objects;
}

export function ancestorChain(scene: SceneData, nodeId: number): number[] {
  const chain: number[] = [];
  let current = scene.nodeById.get(nodeId);
  while (current) {
    chain.push(current.id);
    if (current.parentId === null) {
      break;
    }
    current = scene.nodeById.get(current.parentId);
  }
  return chain;
}
