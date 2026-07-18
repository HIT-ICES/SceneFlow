from __future__ import annotations

import argparse
import json
import math
import sys
from array import array
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

try:
    import orjson
except ImportError:  # pragma: no cover - fallback for running outside the service venv.
    orjson = None


DECOMPOSITION2_DIR = Path(__file__).resolve().parents[1]
SERVICE_DIR = DECOMPOSITION2_DIR.parent
DATA_DIR = SERVICE_DIR / "data"
RESULTS_DIR = SERVICE_DIR / "results"
VISUALIZER_DIR = DECOMPOSITION2_DIR / "visualizer"
DEFAULT_OUTPUT_ROOT = VISUALIZER_DIR / "public" / "data"
DEFAULT_SCENES = ("sfclassroom", "sfapartments")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Prepare lightweight point-cloud assets for the SceneFlow split visualizer."
    )
    parser.add_argument(
        "--scene",
        action="append",
        choices=[*DEFAULT_SCENES, "all"],
        help="Scene to prepare. May be repeated. Defaults to all known scenes.",
    )
    parser.add_argument(
        "--max-points",
        type=int,
        default=800_000,
        help="Maximum sampled points per scene.",
    )
    parser.add_argument(
        "--min-points-per-object",
        type=int,
        default=8,
        help="Preferred minimum sampled points for each object with vertices.",
    )
    parser.add_argument(
        "--output-root",
        type=Path,
        default=DEFAULT_OUTPUT_ROOT,
        help="Output root for generated visualizer data.",
    )
    return parser.parse_args()


def load_json(path: Path) -> Any:
    with path.open("rb") as f:
        payload = f.read()
    if orjson is not None:
        return orjson.loads(payload)
    return json.loads(payload.decode("utf-8"))


def write_json(path: Path, payload: Any) -> None:
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")


def scene_names(requested: list[str] | None) -> list[str]:
    if not requested or "all" in requested:
        return list(DEFAULT_SCENES)
    seen: set[str] = set()
    names: list[str] = []
    for name in requested:
        if name not in seen:
            names.append(name)
            seen.add(name)
    return names


def allocate_sample_counts(
    vertex_counts: list[int],
    *,
    max_points: int,
    min_points_per_object: int,
) -> list[int]:
    if max_points <= 0:
        raise ValueError("--max-points must be positive")
    if min_points_per_object <= 0:
        raise ValueError("--min-points-per-object must be positive")

    total_vertices = sum(vertex_counts)
    if total_vertices <= max_points:
        return list(vertex_counts)

    positive_indexes = [i for i, count in enumerate(vertex_counts) if count > 0]
    if not positive_indexes:
        return [0] * len(vertex_counts)

    counts = [0] * len(vertex_counts)
    if len(positive_indexes) > max_points:
        for index in positive_indexes[:max_points]:
            counts[index] = 1
        return counts

    base_total = 0
    for index in positive_indexes:
        count = min(vertex_counts[index], min_points_per_object)
        counts[index] = count
        base_total += count

    if base_total > max_points:
        removable = sorted(
            positive_indexes,
            key=lambda i: (counts[i], vertex_counts[i], -i),
            reverse=True,
        )
        while base_total > max_points:
            changed = False
            for index in removable:
                if counts[index] > 1:
                    counts[index] -= 1
                    base_total -= 1
                    changed = True
                    if base_total == max_points:
                        break
            if not changed:
                break
        return counts

    remaining = max_points - base_total
    capacities = [max(0, vertex_counts[i] - counts[i]) for i in range(len(vertex_counts))]
    capacity_total = sum(capacities)
    if remaining <= 0 or capacity_total <= 0:
        return counts

    fractional: list[tuple[float, int]] = []
    assigned = 0
    for index, capacity in enumerate(capacities):
        if capacity <= 0:
            continue
        exact = remaining * capacity / capacity_total
        extra = min(capacity, int(math.floor(exact)))
        counts[index] += extra
        assigned += extra
        fractional.append((exact - extra, index))

    leftover = remaining - assigned
    for _, index in sorted(fractional, key=lambda item: (-item[0], item[1])):
        if leftover <= 0:
            break
        if counts[index] < vertex_counts[index]:
            counts[index] += 1
            leftover -= 1

    return counts


def sampled_indexes(vertex_count: int, sample_count: int) -> list[int]:
    if sample_count <= 0:
        return []
    if sample_count >= vertex_count:
        return list(range(vertex_count))
    if sample_count == 1:
        return [vertex_count // 2]
    last = vertex_count - 1
    return [round(i * last / (sample_count - 1)) for i in range(sample_count)]


def object_assignment_fallback(nodes: list[dict[str, Any]]) -> dict[str, int]:
    assignments: dict[str, int] = {}
    for node in nodes:
        node_id = int(node["id"])
        for object_id in node.get("objectIds", []):
            assignments[str(object_id)] = node_id
    return assignments


def normalize_node(node: dict[str, Any]) -> dict[str, Any]:
    return {
        "id": int(node["id"]),
        "parentId": None if node.get("parentId") is None else int(node["parentId"]),
        "type": node.get("type", "unknown"),
        "label": node.get("label", ""),
        "bounds": node.get("bounds"),
        "objectIds": [str(object_id) for object_id in node.get("objectIds", [])],
        "children": [int(child_id) for child_id in node.get("children", [])],
    }


def validate_tree(root_id: int, nodes: list[dict[str, Any]]) -> None:
    node_by_id = {node["id"]: node for node in nodes}
    node_ids = set(node_by_id)
    if root_id not in node_ids:
        raise ValueError(f"rootId {root_id} is not present in tree nodes")
    for node in nodes:
        parent_id = node["parentId"]
        if parent_id is not None and parent_id not in node_ids:
            raise ValueError(f"node {node['id']} references missing parent {parent_id}")
        for child_id in node["children"]:
            if child_id not in node_ids:
                raise ValueError(f"node {node['id']} references missing child {child_id}")
            child = node_by_id[child_id]
            if child["parentId"] != node["id"]:
                raise ValueError(
                    f"node {node['id']} lists child {child_id}, but child parent is {child['parentId']}"
                )


def write_points_and_objects(
    *,
    geometry_objects: list[dict[str, Any]],
    assignments: dict[str, int],
    root_id: int,
    point_counts: list[int],
    points_path: Path,
) -> tuple[list[dict[str, Any]], int]:
    point_start = 0
    visual_objects: list[dict[str, Any]] = []
    with points_path.open("wb") as points_file:
        for obj, point_count in zip(geometry_objects, point_counts, strict=True):
            object_id = str(obj["id"])
            vertices = obj.get("vertices") or []
            floats = array("f")
            for index in sampled_indexes(len(vertices), point_count):
                vertex = vertices[index]
                floats.extend((float(vertex[0]), float(vertex[1]), float(vertex[2])))
            if sys.byteorder != "little":
                floats.byteswap()
            floats.tofile(points_file)

            visual_objects.append(
                {
                    "id": object_id,
                    "nodeId": int(assignments.get(object_id, root_id)),
                    "bounds": obj.get("bounds"),
                    "debugPath": obj.get("debugPath", ""),
                    "pointStart": point_start,
                    "pointCount": point_count,
                    "vertexCount": len(vertices),
                }
            )
            point_start += point_count
    return visual_objects, point_start


def prepare_scene(
    scene_name: str,
    *,
    max_points: int,
    min_points_per_object: int,
    output_root: Path,
) -> None:
    geometry_path = DATA_DIR / f"{scene_name}.geometry.json"
    tree_path = RESULTS_DIR / scene_name / "scene_tree.json"
    output_dir = output_root / scene_name

    if not geometry_path.exists():
        raise FileNotFoundError(f"Geometry input not found: {geometry_path}")
    if not tree_path.exists():
        raise FileNotFoundError(f"Scene tree input not found: {tree_path}")

    print(f"[load] {scene_name}: {geometry_path}")
    geometry = load_json(geometry_path)
    if geometry.get("exporterVersion") != 1:
        raise ValueError(f"Unsupported geometry exporter version in {geometry_path}")
    geometry_objects = geometry.get("objects")
    if not isinstance(geometry_objects, list):
        raise ValueError(f"Invalid geometry objects in {geometry_path}")

    print(f"[load] {scene_name}: {tree_path}")
    tree_payload = load_json(tree_path)
    tree = tree_payload["tree"]
    root_id = int(tree["rootId"])
    nodes = [normalize_node(node) for node in tree["nodes"]]
    validate_tree(root_id, nodes)

    assignments = {
        str(object_id): int(node_id)
        for object_id, node_id in tree_payload.get("objectAssignments", {}).items()
    }
    if not assignments:
        assignments = object_assignment_fallback(nodes)

    vertex_counts = [len(obj.get("vertices") or []) for obj in geometry_objects]
    point_counts = allocate_sample_counts(
        vertex_counts,
        max_points=max_points,
        min_points_per_object=min_points_per_object,
    )

    output_dir.mkdir(parents=True, exist_ok=True)
    points_path = output_dir / "points.bin"
    objects, point_count = write_points_and_objects(
        geometry_objects=geometry_objects,
        assignments=assignments,
        root_id=root_id,
        point_counts=point_counts,
        points_path=points_path,
    )
    if point_count > max_points:
        raise RuntimeError(f"{scene_name} generated {point_count} points, exceeding {max_points}")

    manifest = {
        "scene": scene_name,
        "scenePath": geometry.get("scenePath"),
        "bounds": geometry.get("sceneBounds"),
        "pointCount": point_count,
        "objectCount": len(objects),
        "nodeCount": len(nodes),
        "maxPoints": max_points,
        "generatedAt": datetime.now(timezone.utc).isoformat(),
        "files": {
            "nodes": "nodes.json",
            "objects": "objects.json",
            "points": "points.bin",
        },
    }
    write_json(output_dir / "manifest.json", manifest)
    write_json(output_dir / "nodes.json", {"rootId": root_id, "nodes": nodes})
    write_json(output_dir / "objects.json", {"objects": objects})
    print(
        f"[ok] {scene_name}: {point_count} points, {len(objects)} objects, {len(nodes)} nodes -> {output_dir}"
    )


def main() -> None:
    args = parse_args()
    for scene_name in scene_names(args.scene):
        prepare_scene(
            scene_name,
            max_points=args.max_points,
            min_points_per_object=args.min_points_per_object,
            output_root=args.output_root.resolve(),
        )


if __name__ == "__main__":
    main()
