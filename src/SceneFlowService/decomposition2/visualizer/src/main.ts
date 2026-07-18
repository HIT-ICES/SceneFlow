import "./styles.css";
import { loadSceneData, SceneData } from "./data";
import { DisplayMode, SceneViewer } from "./scene-viewer";
import { selectedStats, TreePanel } from "./tree";

const SCENES = ["sfclassroom", "sfapartments"];

const app = document.querySelector<HTMLDivElement>("#app");
if (!app) {
  throw new Error("App root not found");
}

app.innerHTML = `
  <main class="shell">
    <header class="toolbar">
      <div class="toolbar-group">
        <label>
          <span>场景</span>
          <select id="scene-select"></select>
        </label>
        <label>
          <span>显示</span>
          <select id="display-mode">
            <option value="dim-unselected">灰显非选中</option>
            <option value="all">全场景</option>
            <option value="selected-only">只显示选中子树</option>
          </select>
        </label>
        <label class="range-label">
          <span>点大小</span>
          <input id="point-size" type="range" min="0.8" max="7" step="0.2" value="2" />
        </label>
        <button id="reset-camera" type="button">重置相机</button>
      </div>
      <div id="scene-status" class="scene-status">加载中</div>
    </header>
    <section class="workspace">
      <div id="viewer" class="viewer"></div>
      <aside class="side-panel">
        <div class="selection-card">
          <div class="selection-title">当前节点</div>
          <div id="selection-info" class="selection-info">未选择节点</div>
        </div>
        <div id="tree-panel" class="tree-panel"></div>
      </aside>
    </section>
  </main>
`;

const sceneSelect = document.querySelector<HTMLSelectElement>("#scene-select")!;
const displayMode = document.querySelector<HTMLSelectElement>("#display-mode")!;
const pointSize = document.querySelector<HTMLInputElement>("#point-size")!;
const resetCamera = document.querySelector<HTMLButtonElement>("#reset-camera")!;
const status = document.querySelector<HTMLDivElement>("#scene-status")!;
const selectionInfo = document.querySelector<HTMLDivElement>("#selection-info")!;
const viewerElement = document.querySelector<HTMLDivElement>("#viewer")!;
const treeElement = document.querySelector<HTMLDivElement>("#tree-panel")!;

for (const sceneName of SCENES) {
  const option = document.createElement("option");
  option.value = sceneName;
  option.textContent = sceneName;
  sceneSelect.append(option);
}

let currentScene: SceneData | null = null;
let selectedNodeId: number | null = null;

const viewer = new SceneViewer(viewerElement);
const tree = new TreePanel(treeElement, (nodeId, focus) => {
  selectedNodeId = nodeId;
  viewer.selectNode(nodeId, focus);
  if (currentScene) {
    selectionInfo.textContent = selectedStats(currentScene, nodeId);
  }
});

async function loadScene(sceneName: string): Promise<void> {
  status.textContent = `加载 ${sceneName}`;
  selectionInfo.textContent = "未选择节点";
  selectedNodeId = null;
  try {
    const scene = await loadSceneData(sceneName);
    currentScene = scene;
    viewer.setSceneData(scene);
    viewer.setDisplayMode(displayMode.value as DisplayMode);
    viewer.setPointSize(Number(pointSize.value));
    tree.setScene(scene);
    tree.selectNode(null);
    status.textContent = `${scene.manifest.pointCount.toLocaleString()} points · ${scene.manifest.objectCount.toLocaleString()} objects · ${scene.manifest.nodeCount.toLocaleString()} nodes`;
  } catch (error) {
    currentScene = null;
    status.textContent = error instanceof Error ? error.message : String(error);
    treeElement.innerHTML = "";
  }
}

sceneSelect.addEventListener("change", () => {
  void loadScene(sceneSelect.value);
});

displayMode.addEventListener("change", () => {
  viewer.setDisplayMode(displayMode.value as DisplayMode);
  if (selectedNodeId !== null) {
    viewer.selectNode(selectedNodeId, false);
  }
});

pointSize.addEventListener("input", () => {
  viewer.setPointSize(Number(pointSize.value));
});

resetCamera.addEventListener("click", () => {
  viewer.resetCamera();
});

void loadScene(SCENES[0]);
