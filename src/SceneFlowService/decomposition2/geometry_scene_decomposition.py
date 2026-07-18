from __future__ import annotations

import argparse
import json
import math
import struct
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

import matplotlib.pyplot as plt
import numpy as np
import orjson
from scipy import ndimage


DECOMPOSITION2_DIR = Path(__file__).resolve().parent
SERVICE_DIR = DECOMPOSITION2_DIR.parent
DATA_DIR = SERVICE_DIR / "data"
RESULTS_DIR = SERVICE_DIR / "results"
RUST_VOXELIZER_SOURCE = DECOMPOSITION2_DIR / "rust" / "geometry_voxelizer.rs"
RUST_VOXELIZER_EXE = SERVICE_DIR / "cache" / "geometry_voxelizer" / "geometry_voxelizer.exe"


DEFAULT_INPUTS = (
    DATA_DIR / "sfclassroom.geometry.json",
    DATA_DIR / "sfapartments.geometry.json",
)


@dataclass
class GridSpec:
    origin: np.ndarray
    voxel_size: float
    shape: tuple[int, int, int]

    def world_to_grid(self, points: np.ndarray) -> np.ndarray:
        return np.floor((points - self.origin) / self.voxel_size).astype(np.int32)

    def grid_to_world(self, indexes: np.ndarray) -> np.ndarray:
        return self.origin + (indexes.astype(np.float32) + 0.5) * self.voxel_size


@dataclass
class Zone:
    node_id: int
    floor_node_id: int
    zone_type: str
    floor_index: int
    level_y_index: int
    mask: np.ndarray
    bounds: dict[str, list[float]]


@dataclass
class TreeBuilder:
    nodes: list[dict[str, Any]] = field(default_factory=list)

    def add_node(
        self,
        *,
        node_type: str,
        parent_id: int | None,
        bounds: dict[str, list[float]],
        label: str,
    ) -> int:
        node_id = len(self.nodes)
        self.nodes.append(
            {
                "id": node_id,
                "parentId": parent_id,
                "type": node_type,
                "label": label,
                "bounds": bounds,
                "objectIds": [],
                "children": [],
            }
        )
        if parent_id is not None:
            self.nodes[parent_id]["children"].append(node_id)
        return node_id


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Geometry-only scene decomposition and visualization."
    )
    parser.add_argument("--input", type=Path, help="Input .geometry.json file.")
    parser.add_argument(
        "--all",
        action="store_true",
        help="Run all default geometry exports found in SceneFlowService/data.",
    )
    parser.add_argument("--output-dir", type=Path, help="Output directory.")
    parser.add_argument("--voxel-size", type=float, default=0.25)
    parser.add_argument("--agent-radius", type=float, default=0.35)
    parser.add_argument("--agent-height", type=float, default=1.8)
    parser.add_argument("--level-merge-height", type=float, default=0.75)
    parser.add_argument("--min-floor-area", type=float, default=4.0)
    parser.add_argument("--open-area-tile-size", type=float, default=12.0)
    parser.add_argument("--max-grid-cells", type=int, default=35_000_000)
    parser.add_argument("--max-triangles-per-object", type=int, default=20_000)
    parser.add_argument(
        "--voxelizer",
        choices=["auto", "rust", "python"],
        default="auto",
        help="Voxelization backend. Rust is much faster and used by default when available.",
    )
    parser.add_argument("--skip-visuals", action="store_true")
    return parser.parse_args()


def load_geometry(path: Path) -> dict[str, Any]:
    with path.open("rb") as f:
        data = orjson.loads(f.read())
    if data.get("exporterVersion") != 1:
        raise ValueError(f"Unsupported geometry export version in {path}")
    if "objects" not in data or "sceneBounds" not in data:
        raise ValueError(f"Invalid geometry export: {path}")
    return data


def choose_grid(scene_bounds: dict[str, Any], requested_voxel_size: float, max_grid_cells: int) -> GridSpec:
    bounds_min = np.asarray(scene_bounds["min"], dtype=np.float32)
    bounds_max = np.asarray(scene_bounds["max"], dtype=np.float32)
    extents = bounds_max - bounds_min
    voxel_size = requested_voxel_size
    shape = np.ceil(extents / voxel_size).astype(np.int64) + 5
    cells = int(np.prod(shape))
    if cells > max_grid_cells:
        scale = (cells / max_grid_cells) ** (1.0 / 3.0)
        voxel_size = requested_voxel_size * scale * 1.05
        shape = np.ceil(extents / voxel_size).astype(np.int64) + 5
    origin = bounds_min - voxel_size * 2.0
    return GridSpec(origin=origin, voxel_size=float(voxel_size), shape=tuple(int(x) for x in shape))


def voxelize_scene(
    objects: list[dict[str, Any]],
    grid: GridSpec,
    max_triangles_per_object: int,
    voxelizer: str,
) -> np.ndarray:
    if voxelizer in {"auto", "rust"}:
        try:
            return rust_voxelize_scene(objects, grid, max_triangles_per_object)
        except Exception as exc:
            if voxelizer == "rust":
                raise
            print(f"Rust voxelizer unavailable, falling back to Python: {exc}", file=sys.stderr)
    return python_voxelize_scene(objects, grid, max_triangles_per_object)


def python_voxelize_scene(
    objects: list[dict[str, Any]],
    grid: GridSpec,
    max_triangles_per_object: int,
) -> np.ndarray:
    occupancy = np.zeros(grid.shape, dtype=bool)
    for obj_index, obj in enumerate(objects):
        vertices = np.asarray(obj["vertices"], dtype=np.float32)
        triangles = np.asarray(obj["triangles"], dtype=np.int32).reshape(-1, 3)
        if vertices.size == 0 or triangles.size == 0:
            continue

        if len(triangles) > max_triangles_per_object:
            step = int(math.ceil(len(triangles) / max_triangles_per_object))
            triangles = triangles[::step]

        mark_triangle_samples(occupancy, grid, vertices, triangles)
        if obj_index % 250 == 0:
            print(f"voxelized {obj_index + 1}/{len(objects)} objects")
    return occupancy


def ensure_rust_voxelizer() -> Path:
    if not RUST_VOXELIZER_SOURCE.exists():
        raise FileNotFoundError(f"Rust voxelizer source not found: {RUST_VOXELIZER_SOURCE}")
    needs_compile = (
        not RUST_VOXELIZER_EXE.exists()
        or RUST_VOXELIZER_EXE.stat().st_mtime < RUST_VOXELIZER_SOURCE.stat().st_mtime
    )
    if needs_compile:
        RUST_VOXELIZER_EXE.parent.mkdir(parents=True, exist_ok=True)
        command = [
            "rustc",
            str(RUST_VOXELIZER_SOURCE),
            "-O",
            "-o",
            str(RUST_VOXELIZER_EXE),
        ]
        print(f"compiling Rust voxelizer: {' '.join(command)}")
        subprocess.run(command, cwd=SERVICE_DIR, check=True)
    return RUST_VOXELIZER_EXE


def rust_voxelize_scene(
    objects: list[dict[str, Any]],
    grid: GridSpec,
    max_triangles_per_object: int,
) -> np.ndarray:
    exe = ensure_rust_voxelizer()
    triangle_count = count_exported_triangles(objects, max_triangles_per_object)
    print(f"Rust voxelizer input triangles={triangle_count}")
    process = subprocess.Popen(
        [str(exe)],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    assert process.stdin is not None
    assert process.stdout is not None

    try:
        process.stdin.write(b"SFV1")
        process.stdin.write(struct.pack("<III", *grid.shape))
        process.stdin.write(struct.pack("<fff", *grid.origin.astype(np.float32).tolist()))
        process.stdin.write(struct.pack("<f", float(grid.voxel_size)))
        process.stdin.write(struct.pack("<Q", int(triangle_count)))
        for obj_index, obj in enumerate(objects):
            vertices = np.asarray(obj["vertices"], dtype="<f4")
            triangles = np.asarray(obj["triangles"], dtype=np.int32).reshape(-1, 3)
            if vertices.size == 0 or triangles.size == 0:
                continue
            if len(triangles) > max_triangles_per_object:
                step = int(math.ceil(len(triangles) / max_triangles_per_object))
                triangles = triangles[::step]
            tri_points = np.ascontiguousarray(vertices[triangles], dtype="<f4")
            process.stdin.write(tri_points.tobytes(order="C"))
            if obj_index % 1000 == 0:
                print(f"sent {obj_index + 1}/{len(objects)} objects to Rust voxelizer")
        process.stdin.close()

        header = process.stdout.read(8)
        if len(header) != 8:
            stderr = process.stderr.read().decode("utf-8", errors="replace")
            raise RuntimeError(f"Rust voxelizer did not return an index header. stderr={stderr}")
        index_count = struct.unpack("<Q", header)[0]
        raw_indexes = process.stdout.read(index_count * 4)
        if len(raw_indexes) != index_count * 4:
            raise RuntimeError(
                f"Rust voxelizer returned incomplete index data: "
                f"expected {index_count * 4}, got {len(raw_indexes)}"
            )
        stderr = process.stderr.read().decode("utf-8", errors="replace")
        return_code = process.wait()
        if return_code != 0:
            raise RuntimeError(f"Rust voxelizer failed with code {return_code}: {stderr}")

        indexes = np.frombuffer(raw_indexes, dtype="<u4")
        occupancy = np.zeros(grid.shape, dtype=bool)
        occupancy.ravel()[indexes.astype(np.intp, copy=False)] = True
        print(f"Rust voxelizer occupied voxels={len(indexes)}")
        return occupancy
    finally:
        if process.poll() is None:
            process.kill()


def count_exported_triangles(objects: list[dict[str, Any]], max_triangles_per_object: int) -> int:
    total = 0
    for obj in objects:
        triangles = len(obj["triangles"]) // 3
        if triangles > max_triangles_per_object:
            triangles = math.ceil(triangles / math.ceil(triangles / max_triangles_per_object))
        total += triangles
    return int(total)


def mark_triangle_samples(
    occupancy: np.ndarray,
    grid: GridSpec,
    vertices: np.ndarray,
    triangles: np.ndarray,
) -> None:
    for tri in triangles:
        pts = vertices[tri]
        edge_lengths = (
            np.linalg.norm(pts[0] - pts[1]),
            np.linalg.norm(pts[1] - pts[2]),
            np.linalg.norm(pts[2] - pts[0]),
        )
        steps = max(1, int(math.ceil(max(edge_lengths) / grid.voxel_size)))
        samples: list[np.ndarray] = [pts[0], pts[1], pts[2], pts.mean(axis=0)]
        for i in range(steps + 1):
            for j in range(steps + 1 - i):
                a = i / steps
                b = j / steps
                c = 1.0 - a - b
                samples.append(pts[0] * a + pts[1] * b + pts[2] * c)
        indexes = grid.world_to_grid(np.asarray(samples, dtype=np.float32))
        valid = np.all((indexes >= 0) & (indexes < np.asarray(grid.shape)), axis=1)
        indexes = indexes[valid]
        occupancy[indexes[:, 0], indexes[:, 1], indexes[:, 2]] = True


def horizontal_disk_structure(radius_voxels: int) -> np.ndarray:
    if radius_voxels <= 0:
        return np.ones((1, 1, 1), dtype=bool)
    coords = np.indices((radius_voxels * 2 + 1, 1, radius_voxels * 2 + 1))
    x = coords[0] - radius_voxels
    z = coords[2] - radius_voxels
    return (x * x + z * z) <= radius_voxels * radius_voxels


def build_walkable_surfaces(
    occupied: np.ndarray,
    inflated: np.ndarray,
    grid: GridSpec,
    agent_height: float,
) -> tuple[np.ndarray, np.ndarray]:
    clearance_steps = max(1, int(math.ceil(agent_height / grid.voxel_size)))
    support = occupied[:, :-clearance_steps, :]
    blocked_above = np.zeros_like(support)
    for offset in range(1, clearance_steps + 1):
        blocked_above |= inflated[:, offset : offset + support.shape[1], :]
    walkable = support & ~blocked_above
    counts_by_y = walkable.sum(axis=(0, 2))
    return walkable, counts_by_y


def detect_floor_levels(
    counts_by_y: np.ndarray,
    grid: GridSpec,
    min_floor_area: float,
    level_merge_height: float,
) -> list[int]:
    min_cells = max(4, int(min_floor_area / (grid.voxel_size * grid.voxel_size)))
    smoothed = ndimage.gaussian_filter1d(counts_by_y.astype(np.float32), sigma=1.5)
    local_max = smoothed == ndimage.maximum_filter1d(smoothed, size=5)
    candidates = np.where(local_max & (smoothed >= min_cells))[0].tolist()
    if not candidates and counts_by_y.max() > 0:
        candidates = [int(np.argmax(counts_by_y))]

    merged: list[int] = []
    merge_steps = max(1, int(math.ceil(level_merge_height / grid.voxel_size)))
    for idx in candidates:
        if not merged or idx - merged[-1] > merge_steps:
            merged.append(idx)
        elif counts_by_y[idx] > counts_by_y[merged[-1]]:
            merged[-1] = idx
    return merged


def connected_components_2d(mask: np.ndarray) -> tuple[np.ndarray, int]:
    structure = np.array([[0, 1, 0], [1, 1, 1], [0, 1, 0]], dtype=bool)
    return ndimage.label(mask, structure=structure)


def split_component_into_zones(
    component_mask: np.ndarray,
    *,
    open_area_tile_steps: int,
    min_zone_cells: int,
) -> list[tuple[str, np.ndarray]]:
    if component_mask.sum() <= min_zone_cells * 2:
        return [("zone", component_mask)]

    distance = ndimage.distance_transform_edt(component_mask)
    peak_threshold = max(2.0, float(distance.max()) * 0.45)
    peaks = component_mask & (distance == ndimage.maximum_filter(distance, size=9)) & (distance >= peak_threshold)
    seed_labels, seed_count = connected_components_2d(peaks)

    if seed_count >= 2:
        _, nearest = ndimage.distance_transform_edt(seed_labels == 0, return_indices=True)
        assigned = seed_labels[nearest[0], nearest[1]]
        zones = []
        for seed_id in range(1, seed_count + 1):
            zone = component_mask & (assigned == seed_id)
            if zone.sum() >= min_zone_cells:
                zones.append(("zone", zone))
        if zones:
            return zones

    return split_open_area(component_mask, open_area_tile_steps, min_zone_cells)


def split_open_area(
    component_mask: np.ndarray,
    open_area_tile_steps: int,
    min_zone_cells: int,
) -> list[tuple[str, np.ndarray]]:
    xs, zs = np.where(component_mask)
    x_min, x_max = int(xs.min()), int(xs.max()) + 1
    z_min, z_max = int(zs.min()), int(zs.max()) + 1
    zones: list[tuple[str, np.ndarray]] = []
    for x0 in range(x_min, x_max, open_area_tile_steps):
        for z0 in range(z_min, z_max, open_area_tile_steps):
            tile = np.zeros_like(component_mask)
            tile[x0 : x0 + open_area_tile_steps, z0 : z0 + open_area_tile_steps] = True
            zone = component_mask & tile
            if zone.sum() >= min_zone_cells:
                zones.append(("open_area_cell", zone))
    return zones or [("open_area_cell", component_mask)]


def bounds_from_mask(
    mask: np.ndarray,
    grid: GridSpec,
    *,
    y_min_index: int,
    y_max_index: int,
) -> dict[str, list[float]]:
    xs, zs = np.where(mask)
    min_idx = np.array([xs.min(), y_min_index, zs.min()], dtype=np.float32)
    max_idx = np.array([xs.max() + 1, y_max_index, zs.max() + 1], dtype=np.float32)
    min_world = grid.origin + min_idx * grid.voxel_size
    max_world = grid.origin + max_idx * grid.voxel_size
    return make_bounds(min_world, max_world)


def make_bounds(min_world: np.ndarray, max_world: np.ndarray) -> dict[str, list[float]]:
    center = (min_world + max_world) * 0.5
    extents = (max_world - min_world) * 0.5
    return {
        "center": center.astype(float).tolist(),
        "extents": extents.astype(float).tolist(),
        "min": min_world.astype(float).tolist(),
        "max": max_world.astype(float).tolist(),
    }


def object_bounds(obj: dict[str, Any]) -> tuple[np.ndarray, np.ndarray]:
    bounds = obj["bounds"]
    return np.asarray(bounds["min"], dtype=np.float32), np.asarray(bounds["max"], dtype=np.float32)


def assign_objects_to_tree(
    *,
    objects: list[dict[str, Any]],
    zones: list[Zone],
    root_id: int,
    tree: TreeBuilder,
    grid: GridSpec,
) -> dict[str, int]:
    assigned: dict[str, int] = {}
    for obj in objects:
        obj_id = obj["id"]
        b_min, b_max = object_bounds(obj)
        center = (b_min + b_max) * 0.5
        min_idx = np.floor((b_min - grid.origin) / grid.voxel_size).astype(int)
        max_idx = np.ceil((b_max - grid.origin) / grid.voxel_size).astype(int)
        min_idx = np.maximum(min_idx, 0)
        max_idx = np.minimum(max_idx, np.asarray(grid.shape) - 1)
        if np.any(max_idx <= min_idx):
            assigned[obj_id] = root_id
            continue

        best_zone: Zone | None = None
        best_overlap = 0
        nearest_zone: Zone | None = None
        nearest_zone_dist = float("inf")
        total_footprint = max(1, (max_idx[0] - min_idx[0] + 1) * (max_idx[2] - min_idx[2] + 1))

        for zone in zones:
            bounds = tree.nodes[zone.node_id]["bounds"]
            z_min = np.asarray(bounds["min"], dtype=np.float32)
            z_max = np.asarray(bounds["max"], dtype=np.float32)
            if b_max[1] < z_min[1] - grid.voxel_size * 2 or b_min[1] > z_max[1] + grid.voxel_size * 6:
                continue
            dist = rect_distance_xz(b_min, b_max, z_min, z_max)
            if dist < nearest_zone_dist:
                nearest_zone_dist = dist
                nearest_zone = zone
            overlap = int(
                np.count_nonzero(
                    zone.mask[min_idx[0] : max_idx[0] + 1, min_idx[2] : max_idx[2] + 1]
                )
            )
            if overlap > best_overlap:
                best_overlap = overlap
                best_zone = zone

        if best_zone is None or best_overlap == 0:
            if nearest_zone is not None and nearest_zone_dist <= max(2.0, grid.voxel_size * 6):
                assigned[obj_id] = upper_node_for_large_object(
                    footprint_cells=total_footprint,
                    zone=nearest_zone,
                    grid=grid,
                )
            else:
                assigned[obj_id] = root_id
            continue

        overlap_ratio = best_overlap / total_footprint
        if overlap_ratio >= 0.55:
            assigned[obj_id] = best_zone.node_id
        else:
            assigned[obj_id] = upper_node_for_large_object(
                footprint_cells=total_footprint,
                zone=best_zone,
                grid=grid,
            )

    for obj_id, node_id in assigned.items():
        tree.nodes[node_id]["objectIds"].append(obj_id)
    return assigned


def rect_distance_xz(
    a_min: np.ndarray,
    a_max: np.ndarray,
    b_min: np.ndarray,
    b_max: np.ndarray,
) -> float:
    dx = max(float(b_min[0] - a_max[0]), float(a_min[0] - b_max[0]), 0.0)
    dz = max(float(b_min[2] - a_max[2]), float(a_min[2] - b_max[2]), 0.0)
    return math.sqrt(dx * dx + dz * dz)


def upper_node_for_large_object(*, footprint_cells: int, zone: Zone, grid: GridSpec) -> int:
    large_object_cells = max(64, int(25.0 / (grid.voxel_size * grid.voxel_size)))
    if footprint_cells >= large_object_cells:
        return zone.floor_node_id
    return zone.node_id


def build_scene_tree(
    *,
    data: dict[str, Any],
    grid: GridSpec,
    walkable: np.ndarray,
    floor_levels: list[int],
    agent_height: float,
    open_area_tile_size: float,
) -> tuple[dict[str, Any], list[Zone], list[np.ndarray], dict[str, int]]:
    tree = TreeBuilder()
    root_id = tree.add_node(
        node_type="root",
        parent_id=None,
        bounds=data["sceneBounds"],
        label="root",
    )

    union_floor_mask = np.zeros((grid.shape[0], grid.shape[2]), dtype=bool)
    floor_masks: list[np.ndarray] = []
    level_tolerance = max(1, int(math.ceil(0.35 / grid.voxel_size)))
    for level in floor_levels:
        lo = max(0, level - level_tolerance)
        hi = min(walkable.shape[1], level + level_tolerance + 1)
        floor_mask = walkable[:, lo:hi, :].any(axis=1)
        floor_masks.append(floor_mask)
        union_floor_mask |= floor_mask

    block_labels, block_count = connected_components_2d(union_floor_mask)
    if block_count == 0:
        block_labels = np.ones_like(union_floor_mask, dtype=np.int32)
        block_count = 1

    zones: list[Zone] = []
    open_area_tile_steps = max(4, int(math.ceil(open_area_tile_size / grid.voxel_size)))
    min_zone_cells = max(8, int(4.0 / (grid.voxel_size * grid.voxel_size)))
    y_room_height = max(1, int(math.ceil(agent_height * 1.5 / grid.voxel_size)))

    for block_id in range(1, block_count + 1):
        block_mask = block_labels == block_id
        if not block_mask.any():
            continue
        block_node_id = tree.add_node(
            node_type="block",
            parent_id=root_id,
            bounds=bounds_from_mask(
                block_mask,
                grid,
                y_min_index=0,
                y_max_index=grid.shape[1] - 1,
            ),
            label=f"block_{block_id - 1}",
        )

        for floor_index, (level, floor_mask) in enumerate(zip(floor_levels, floor_masks)):
            current_floor_mask = floor_mask & block_mask
            if not current_floor_mask.any():
                continue
            floor_node_id = tree.add_node(
                node_type="floor",
                parent_id=block_node_id,
                bounds=bounds_from_mask(
                    current_floor_mask,
                    grid,
                    y_min_index=level,
                    y_max_index=min(grid.shape[1] - 1, level + y_room_height),
                ),
                label=f"floor_{floor_index}",
            )

            component_labels, component_count = connected_components_2d(current_floor_mask)
            for component_id in range(1, component_count + 1):
                component_mask = component_labels == component_id
                for zone_type, zone_mask in split_component_into_zones(
                    component_mask,
                    open_area_tile_steps=open_area_tile_steps,
                    min_zone_cells=min_zone_cells,
                ):
                    node_id = tree.add_node(
                        node_type=zone_type,
                        parent_id=floor_node_id,
                        bounds=bounds_from_mask(
                            zone_mask,
                            grid,
                            y_min_index=level,
                            y_max_index=min(grid.shape[1] - 1, level + y_room_height),
                        ),
                        label=f"{zone_type}_{len(zones)}",
                    )
                    zones.append(
                        Zone(
                            node_id=node_id,
                            floor_node_id=floor_node_id,
                            zone_type=zone_type,
                            floor_index=floor_index,
                            level_y_index=level,
                            mask=zone_mask,
                            bounds=tree.nodes[node_id]["bounds"],
                        )
                    )

    assignments = assign_objects_to_tree(
        objects=data["objects"],
        zones=zones,
        root_id=root_id,
        tree=tree,
        grid=grid,
    )

    return {"rootId": root_id, "nodes": tree.nodes}, zones, floor_masks, assignments


def write_visualizations(
    *,
    data: dict[str, Any],
    output_dir: Path,
    occupied: np.ndarray,
    inflated: np.ndarray,
    floor_masks: list[np.ndarray],
    zones: list[Zone],
    assignments: dict[str, int],
    grid: GridSpec,
) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)
    plot_mask(
        occupied.any(axis=1).T,
        output_dir / "top_down_occupancy.png",
        title="Top-down Occupancy",
    )
    plot_mask(
        (~inflated).any(axis=1).T,
        output_dir / "top_down_free_space.png",
        title="Top-down Free Space",
    )
    for floor_index, floor_mask in enumerate(floor_masks):
        plot_mask(
            floor_mask.T,
            output_dir / f"floor_{floor_index:02d}_free_space.png",
            title=f"Floor {floor_index} Free Space",
        )
    if zones:
        zone_image = np.zeros_like(zones[0].mask, dtype=np.int32)
        for i, zone in enumerate(zones, start=1):
            zone_image[zone.mask] = i
        plot_label_image(
            zone_image.T,
            output_dir / "room_zone_segmentation.png",
            title="Room / Zone Segmentation",
        )
    plot_object_assignments(
        data=data,
        assignments=assignments,
        grid=grid,
        path=output_dir / "object_assignment.png",
    )


def plot_mask(mask: np.ndarray, path: Path, title: str) -> None:
    plt.figure(figsize=(10, 10))
    plt.imshow(mask, origin="lower", cmap="gray")
    plt.title(title)
    plt.axis("off")
    plt.tight_layout()
    plt.savefig(path, dpi=160)
    plt.close()


def plot_label_image(labels: np.ndarray, path: Path, title: str) -> None:
    plt.figure(figsize=(10, 10))
    plt.imshow(labels, origin="lower", cmap="tab20")
    plt.title(title)
    plt.axis("off")
    plt.tight_layout()
    plt.savefig(path, dpi=160)
    plt.close()


def plot_object_assignments(
    *,
    data: dict[str, Any],
    assignments: dict[str, int],
    grid: GridSpec,
    path: Path,
) -> None:
    xs: list[float] = []
    zs: list[float] = []
    colors: list[int] = []
    for obj in data["objects"]:
        obj_id = obj["id"]
        if obj_id not in assignments:
            continue
        b_min, b_max = object_bounds(obj)
        center = (b_min + b_max) * 0.5
        index = grid.world_to_grid(center.reshape(1, 3))[0]
        xs.append(float(index[0]))
        zs.append(float(index[2]))
        colors.append(assignments[obj_id])

    plt.figure(figsize=(10, 10))
    plt.scatter(xs, zs, c=colors, s=5, cmap="tab20")
    plt.title("Object Assignment")
    plt.axis("equal")
    plt.axis("off")
    plt.tight_layout()
    plt.savefig(path, dpi=160)
    plt.close()


def decompose_scene(
    *,
    input_path: Path,
    output_dir: Path,
    voxel_size: float,
    agent_radius: float,
    agent_height: float,
    level_merge_height: float,
    min_floor_area: float,
    open_area_tile_size: float,
    max_grid_cells: int,
    max_triangles_per_object: int,
    voxelizer: str,
    skip_visuals: bool,
) -> None:
    data = load_geometry(input_path)
    output_dir.mkdir(parents=True, exist_ok=True)
    grid = choose_grid(data["sceneBounds"], voxel_size, max_grid_cells)
    print(f"grid shape={grid.shape}, voxel_size={grid.voxel_size:.3f}")

    occupied = voxelize_scene(data["objects"], grid, max_triangles_per_object, voxelizer)
    radius_steps = max(0, int(math.ceil(agent_radius / grid.voxel_size)))
    inflated = ndimage.binary_dilation(occupied, structure=horizontal_disk_structure(radius_steps))
    walkable, counts_by_y = build_walkable_surfaces(
        occupied=occupied,
        inflated=inflated,
        grid=grid,
        agent_height=agent_height,
    )
    floor_levels = detect_floor_levels(
        counts_by_y=counts_by_y,
        grid=grid,
        min_floor_area=min_floor_area,
        level_merge_height=level_merge_height,
    )
    if not floor_levels:
        raise RuntimeError("No floor levels detected from geometry.")

    scene_tree, zones, floor_masks, assignments = build_scene_tree(
        data=data,
        grid=grid,
        walkable=walkable,
        floor_levels=floor_levels,
        agent_height=agent_height,
        open_area_tile_size=open_area_tile_size,
    )
    result = {
        "metadata": {
            "input": str(input_path),
            "scenePath": data.get("scenePath"),
            "voxelSizeRequested": voxel_size,
            "voxelSizeActual": grid.voxel_size,
            "gridShape": list(grid.shape),
            "agentRadius": agent_radius,
            "agentHeight": agent_height,
            "floorLevels": [
                float(grid.origin[1] + (level + 0.5) * grid.voxel_size)
                for level in floor_levels
            ],
            "zoneCount": len(zones),
            "objectCount": len(data["objects"]),
        },
        "tree": scene_tree,
        "objectAssignments": assignments,
    }
    (output_dir / "scene_tree.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
    if not skip_visuals:
        write_visualizations(
            data=data,
            output_dir=output_dir,
            occupied=occupied,
            inflated=inflated,
            floor_masks=floor_masks,
            zones=zones,
            assignments=assignments,
            grid=grid,
        )
    print(f"wrote {output_dir / 'scene_tree.json'}")


def default_output_dir(input_path: Path) -> Path:
    name = input_path.name.removesuffix(".geometry.json")
    return RESULTS_DIR / name


def main() -> None:
    args = parse_args()
    inputs: list[Path]
    if args.all:
        inputs = [path for path in DEFAULT_INPUTS if path.exists()]
        if not inputs:
            raise FileNotFoundError("No default .geometry.json files found. Run decomposition2/export_scene_geometry.py first.")
    elif args.input:
        inputs = [args.input]
    else:
        raise ValueError("Pass --input <file> or --all.")

    for input_path in inputs:
        output_dir = args.output_dir if args.output_dir and len(inputs) == 1 else default_output_dir(input_path)
        decompose_scene(
            input_path=input_path,
            output_dir=output_dir,
            voxel_size=args.voxel_size,
            agent_radius=args.agent_radius,
            agent_height=args.agent_height,
            level_merge_height=args.level_merge_height,
            min_floor_area=args.min_floor_area,
            open_area_tile_size=args.open_area_tile_size,
            max_grid_cells=args.max_grid_cells,
            max_triangles_per_object=args.max_triangles_per_object,
            voxelizer=args.voxelizer,
            skip_visuals=args.skip_visuals,
        )


if __name__ == "__main__":
    main()
