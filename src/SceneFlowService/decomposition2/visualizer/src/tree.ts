import { SceneData, SceneNode, VisualObject, ancestorChain, collectSubtreeObjects } from "./data";

export type TreeSelectionHandler = (nodeId: number, focus: boolean) => void;

const FILTER_TYPES = ["block", "floor", "zone", "open_area_cell"];

export class TreePanel {
  private readonly container: HTMLElement;
  private readonly onSelect: TreeSelectionHandler;
  private scene: SceneData | null = null;
  private selectedNodeId: number | null = null;
  private expanded = new Set<number>();
  private enabledTypes = new Set(FILTER_TYPES);
  private searchTerm = "";

  constructor(container: HTMLElement, onSelect: TreeSelectionHandler) {
    this.container = container;
    this.onSelect = onSelect;
  }

  setScene(scene: SceneData): void {
    this.scene = scene;
    this.selectedNodeId = null;
    this.expanded = new Set([scene.rootId]);
    for (const node of scene.nodes) {
      if (node.type === "block" || node.type === "floor") {
        this.expanded.add(node.id);
      }
    }
    this.render();
  }

  selectNode(nodeId: number | null): void {
    if (nodeId !== null) {
      this.selectedNodeId = nodeId;
      if (this.scene) {
        for (const ancestor of ancestorChain(this.scene, nodeId)) {
          this.expanded.add(ancestor);
        }
      }
    } else {
      this.selectedNodeId = null;
    }
    this.render();
  }

  private render(): void {
    if (!this.scene) {
      this.container.innerHTML = "";
      return;
    }

    this.container.innerHTML = "";
    const tools = document.createElement("div");
    tools.className = "tree-tools";

    const search = document.createElement("input");
    search.type = "search";
    search.placeholder = "搜索 node id / object id";
    search.value = this.searchTerm;
    search.addEventListener("input", () => {
      this.searchTerm = search.value.trim().toLowerCase();
      this.expandSearchMatches();
      this.render();
    });
    tools.append(search);

    const filters = document.createElement("div");
    filters.className = "type-filters";
    for (const type of FILTER_TYPES) {
      const label = document.createElement("label");
      const input = document.createElement("input");
      input.type = "checkbox";
      input.checked = this.enabledTypes.has(type);
      input.addEventListener("change", () => {
        if (input.checked) {
          this.enabledTypes.add(type);
        } else {
          this.enabledTypes.delete(type);
        }
        this.render();
      });
      label.append(input, document.createTextNode(type));
      filters.append(label);
    }
    tools.append(filters);
    this.container.append(tools);

    const tree = document.createElement("div");
    tree.className = "tree-list";
    const rootNode = this.scene.nodeById.get(this.scene.rootId);
    if (rootNode) {
      this.renderNode(tree, rootNode, 0);
    }
    this.container.append(tree);
  }

  private renderNode(parent: HTMLElement, node: SceneNode, depth: number): boolean {
    if (!this.scene) {
      return false;
    }

    const children = node.children
      .map((childId) => this.scene?.nodeById.get(childId))
      .filter((child): child is SceneNode => Boolean(child));

    const childContainers: HTMLElement[] = [];
    let hasVisibleChild = false;
    for (const child of children) {
      const childContainer = document.createElement("div");
      const childVisible = this.renderNode(childContainer, child, depth + 1);
      if (childVisible) {
        hasVisibleChild = true;
        childContainers.push(childContainer);
      }
    }

    const selfMatches = this.nodeMatches(node);
    const typeVisible = node.type === "root" || this.enabledTypes.has(node.type);
    const visible = typeVisible && (selfMatches || hasVisibleChild || this.searchTerm === "");
    if (!visible) {
      return false;
    }

    const row = document.createElement("button");
    row.className = "tree-row";
    if (node.id === this.selectedNodeId) {
      row.classList.add("is-selected");
    }
    row.style.setProperty("--depth", String(depth));

    const hasChildren = children.length > 0;
    const expander = document.createElement("span");
    expander.className = "tree-expander";
    expander.textContent = hasChildren ? (this.expanded.has(node.id) ? "▾" : "▸") : "";
    expander.addEventListener("click", (event) => {
      event.stopPropagation();
      this.toggleExpanded(node.id);
    });

    const main = document.createElement("span");
    main.className = "tree-main";
    main.textContent = `${node.id} · ${node.type} · ${node.label}`;

    const meta = document.createElement("span");
    meta.className = "tree-meta";
    meta.textContent = `${node.objectIds.length} obj · ${node.children.length} child`;

    row.append(expander, main, meta);
    row.addEventListener("click", () => {
      this.selectedNodeId = node.id;
      this.expanded.add(node.id);
      this.onSelect(node.id, false);
      this.render();
    });
    row.addEventListener("dblclick", () => {
      this.selectedNodeId = node.id;
      this.expanded.add(node.id);
      this.onSelect(node.id, true);
      this.render();
    });
    parent.append(row);

    if (this.expanded.has(node.id) || this.searchTerm !== "") {
      for (const childContainer of childContainers) {
        parent.append(childContainer);
      }
    }
    return true;
  }

  private toggleExpanded(nodeId: number): void {
    if (this.expanded.has(nodeId)) {
      this.expanded.delete(nodeId);
    } else {
      this.expanded.add(nodeId);
    }
    this.render();
  }

  private nodeMatches(node: SceneNode): boolean {
    if (!this.scene || this.searchTerm === "") {
      return true;
    }
    if (String(node.id).includes(this.searchTerm)) {
      return true;
    }
    if (node.label.toLowerCase().includes(this.searchTerm) || node.type.toLowerCase().includes(this.searchTerm)) {
      return true;
    }
    const object = this.scene.objectById.get(this.searchTerm);
    if (object) {
      return ancestorChain(this.scene, object.nodeId).includes(node.id);
    }
    return node.objectIds.some((objectId) => objectId.toLowerCase().includes(this.searchTerm));
  }

  private expandSearchMatches(): void {
    if (!this.scene || this.searchTerm === "") {
      return;
    }
    const object = this.scene.objectById.get(this.searchTerm);
    if (object) {
      for (const ancestor of ancestorChain(this.scene, object.nodeId)) {
        this.expanded.add(ancestor);
      }
      return;
    }
    for (const node of this.scene.nodes) {
      if (this.nodeMatches(node)) {
        for (const ancestor of ancestorChain(this.scene, node.id)) {
          this.expanded.add(ancestor);
        }
      }
    }
  }
}

export function selectedStats(scene: SceneData, nodeId: number | null): string {
  if (nodeId === null) {
    return "未选择节点";
  }
  const node = scene.nodeById.get(nodeId);
  if (!node) {
    return "节点不存在";
  }
  const objects = collectSubtreeObjects(scene, nodeId);
  return `${node.id} · ${node.type} · ${objects.length} subtree obj · ${node.children.length} child`;
}
