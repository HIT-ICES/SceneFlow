from __future__ import annotations

import math
from collections.abc import Callable, Sequence
from dataclasses import dataclass
from typing import Literal

import numpy as np
from sklearn.cluster import DBSCAN
from sklearn.neighbors import NearestNeighbors

from utils import cluster_utils
from utils.dataset import load_voxels


PipelineName = Literal["legacy_xz", "dedup_xyz"]
N_JOBS = -1
DEFAULT_SAMPLE_FOR_KNEE = 50_000
DEFAULT_SEED = 42


@dataclass(frozen=True)
class CalibrationScene:
    points_3d: np.ndarray
    object_ids: np.ndarray | None
    object_downsampled_points: int
    unique_global_cells: int
    duplicate_ratio: float
    bounds_min: np.ndarray
    bounds_max: np.ndarray


def sample_rows(
    points: np.ndarray,
    ids: np.ndarray | None,
    *,
    max_points: int | None,
    seed: int,
) -> tuple[np.ndarray, np.ndarray | None]:
    if max_points is None or len(points) <= max_points:
        return points, ids
    rng = np.random.default_rng(seed)
    idx = rng.choice(len(points), size=max_points, replace=False)
    return points[idx], None if ids is None else ids[idx]


def make_calibration_scene(
    points_3d: np.ndarray,
    object_ids: np.ndarray | None,
    *,
    step: float,
    object_downsampled_points: int | None = None,
    unique_global_cells: int | None = None,
    bounds_min: np.ndarray | None = None,
    bounds_max: np.ndarray | None = None,
) -> CalibrationScene:
    points = np.asarray(points_3d, dtype=np.float32)
    if points.ndim != 2 or points.shape[1] != 3:
        raise ValueError(f"points_3d should have shape (n, 3), got {points.shape}")
    if len(points) == 0:
        raise ValueError("no points available for DBSCAN calibration")
    if step <= 0:
        raise ValueError("step must be positive")

    object_count = int(object_downsampled_points or len(points))
    if unique_global_cells is None:
        keys = np.rint(points / step).astype(np.int64)
        unique_global_cells = int(len(np.unique(keys, axis=0)))
    duplicate_ratio = 1.0 - unique_global_cells / max(1, object_count)

    return CalibrationScene(
        points_3d=points,
        object_ids=None if object_ids is None else np.asarray(object_ids),
        object_downsampled_points=object_count,
        unique_global_cells=int(unique_global_cells),
        duplicate_ratio=float(duplicate_ratio),
        bounds_min=np.asarray(bounds_min, dtype=np.float32) if bounds_min is not None else points.min(axis=0),
        bounds_max=np.asarray(bounds_max, dtype=np.float32) if bounds_max is not None else points.max(axis=0),
    )


def load_legacy_scene(
    raw_data: dict,
    *,
    step: float,
    max_points: int | None,
    seed: int,
) -> CalibrationScene:
    points, object_ids = load_voxels(raw_data, downsample_step=step)
    points, object_ids = sample_rows(points, object_ids, max_points=max_points, seed=seed)
    return make_calibration_scene(points, object_ids, step=step)


def load_dedup_scene(
    raw_data: dict,
    *,
    step: float,
    max_points: int | None,
    seed: int,
    global_dedup: bool = True,
) -> CalibrationScene:
    per_object_cells: list[np.ndarray] = []
    object_downsampled_points = 0

    for obj in raw_data.get("objects", []):
        voxels = obj.get("voxels")
        if not voxels:
            continue
        cells = np.rint(np.asarray(voxels, dtype=np.float32) / step).astype(np.int64)
        cells = np.unique(cells, axis=0)
        if len(cells) == 0:
            continue
        per_object_cells.append(cells)
        object_downsampled_points += len(cells)

    if not per_object_cells:
        raise ValueError("no voxel data found")

    all_cells = np.concatenate(per_object_cells, axis=0)
    unique_cells = np.unique(all_cells, axis=0)
    cluster_cells = unique_cells if global_dedup else all_cells

    if max_points is not None and len(cluster_cells) > max_points:
        rng = np.random.default_rng(seed)
        cluster_cells = cluster_cells[rng.choice(len(cluster_cells), size=max_points, replace=False)]

    return make_calibration_scene(
        cluster_cells.astype(np.float32) * step,
        None,
        step=step,
        object_downsampled_points=object_downsampled_points,
        unique_global_cells=len(unique_cells),
        bounds_min=unique_cells.min(axis=0).astype(np.float32) * step,
        bounds_max=unique_cells.max(axis=0).astype(np.float32) * step,
    )


def load_calibration_scene(
    raw_data: dict,
    *,
    step: float,
    max_points: int | None,
    seed: int,
    pipeline: PipelineName,
) -> CalibrationScene:
    if pipeline == "legacy_xz":
        return load_legacy_scene(raw_data, step=step, max_points=max_points, seed=seed)
    if pipeline == "dedup_xyz":
        return load_dedup_scene(raw_data, step=step, max_points=max_points, seed=seed)
    raise ValueError(f"unsupported pipeline: {pipeline}")


def pipeline_mode(pipeline: PipelineName) -> Literal["xz", "xyz"]:
    if pipeline == "legacy_xz":
        return "xz"
    if pipeline == "dedup_xyz":
        return "xyz"
    raise ValueError(f"unsupported pipeline: {pipeline}")


def pipeline_min_samples_candidates(pipeline: PipelineName) -> tuple[int, ...]:
    if pipeline == "legacy_xz":
        return 20, 40, 60, 80, 100, 150, 200
    if pipeline == "dedup_xyz":
        return 4, 6, 8, 10, 12, 16
    raise ValueError(f"unsupported pipeline: {pipeline}")


def reduce_points(points_3d: np.ndarray, pipeline: PipelineName) -> np.ndarray:
    if pipeline_mode(pipeline) == "xz":
        return points_3d[:, [0, 2]]
    return points_3d


def estimate_knee_eps(
    points: np.ndarray,
    *,
    min_samples: int,
    sample_for_knee: int,
    seed: int,
) -> float:
    if len(points) > sample_for_knee:
        rng = np.random.default_rng(seed)
        points = points[rng.choice(len(points), size=sample_for_knee, replace=False)]

    neighbors = min(min_samples + 1, len(points))
    nbrs = NearestNeighbors(n_neighbors=neighbors, algorithm="auto", n_jobs=N_JOBS)
    nbrs.fit(points)
    distances, _ = nbrs.kneighbors(points)
    kdist = np.sort(distances[:, -1])

    x = np.linspace(0.0, 1.0, len(kdist), dtype=np.float32)
    y_min = float(kdist[0])
    y_max = float(kdist[-1])
    if math.isclose(y_min, y_max):
        return y_max

    y = (kdist - y_min) / (y_max - y_min)
    return float(kdist[int(np.argmax(y - x))])


def candidate_eps_values(eps0: float, *, step: float, pipeline: PipelineName) -> list[float]:
    upper = 4.0 * step
    neighbor_radius = math.sqrt(2.0) * step if pipeline_mode(pipeline) == "xz" else math.sqrt(3.0) * step
    values = [
        step,
        neighbor_radius,
        2.0 * step,
        3.0 * step,
        4.0 * step,
        *(eps0 * factor for factor in (0.75, 0.9, 1.0, 1.1, 1.25)),
    ]
    values = [min(upper, max(step, value)) for value in values]
    return sorted(set(round(value, 6) for value in values))


def run_dbscan(points: np.ndarray, *, eps: float, min_samples: int) -> np.ndarray:
    return DBSCAN(eps=eps, min_samples=min_samples, n_jobs=N_JOBS).fit_predict(points)


def score_summary(
    *,
    noise_ratio: float,
    largest_ratio: float,
    small_ratio: float,
    cluster_count: int,
) -> float:
    return (
        1.0
        - 0.2 * max(0.0, noise_ratio - 0.60)
        - 2.0 * max(0.0, largest_ratio - 0.75)
        - 0.25 * small_ratio
        - (0.25 if cluster_count <= 1 else 0.0)
    )


def summarize_labels(
    points: np.ndarray,
    labels: np.ndarray,
    *,
    eps: float,
    min_samples: int,
    noise_labels: np.ndarray | None = None,
) -> dict:
    post_noise_ratio = float(np.mean(labels == -1))
    noise_ratio = float(np.mean(noise_labels == -1)) if noise_labels is not None else post_noise_ratio
    cluster_labels, counts = np.unique(labels[labels != -1], return_counts=True)
    cluster_count = int(len(cluster_labels))
    largest_ratio = float(counts.max() / len(points)) if cluster_count else 0.0
    small_threshold = max(32, int(0.001 * len(points)))
    small_ratio = float(np.sum(counts < small_threshold) / cluster_count) if cluster_count else 1.0

    return {
        "eps": float(eps),
        "min_samples": int(min_samples),
        "clusters": cluster_count,
        "noise_ratio": noise_ratio,
        "post_noise_ratio": post_noise_ratio,
        "largest_ratio": largest_ratio,
        "small_ratio": small_ratio,
        "score": score_summary(
            noise_ratio=noise_ratio,
            largest_ratio=largest_ratio,
            small_ratio=small_ratio,
            cluster_count=cluster_count,
        ),
    }


def post_process_legacy_labels(scene: CalibrationScene, labels: np.ndarray) -> np.ndarray:
    if scene.object_ids is None:
        raise ValueError("legacy_xz requires object ids for compare-compatible post-processing")
    if np.all(labels == -1):
        return labels
    labels = cluster_utils.cluster_noise_points(data_v=scene.points_3d, labels=labels)
    labels = cluster_utils.unify_by_id(scene.object_ids, labels)
    return cluster_utils.remap_labels(labels)


def candidate_labels(
    scene: CalibrationScene,
    *,
    pipeline: PipelineName,
    eps: float,
    min_samples: int,
) -> tuple[np.ndarray, np.ndarray]:
    points = reduce_points(scene.points_3d, pipeline)
    labels = run_dbscan(points, eps=eps, min_samples=min_samples)
    if pipeline == "legacy_xz":
        return post_process_legacy_labels(scene, labels), labels
    return labels, labels


def candidate_summary(
    scene: CalibrationScene,
    *,
    pipeline: PipelineName,
    eps: float,
    min_samples: int,
) -> dict:
    points = reduce_points(scene.points_3d, pipeline)
    labels, raw_labels = candidate_labels(scene, pipeline=pipeline, eps=eps, min_samples=min_samples)
    summary = summarize_labels(points, labels, eps=eps, min_samples=min_samples, noise_labels=raw_labels)
    summary["score_labels"] = (
        "raw noise + clusters.dbscan post_process"
        if pipeline == "legacy_xz"
        else "raw DBSCAN"
    )
    return summary


def sort_calibration_results(results: list[dict]) -> list[dict]:
    return sorted(results, key=lambda item: (item["clusters"] > 0, item["score"]), reverse=True)


def calibrate_dbscan(
    scene: CalibrationScene,
    *,
    step: float,
    pipeline: PipelineName = "legacy_xz",
    seed: int = DEFAULT_SEED,
    sample_for_knee: int = DEFAULT_SAMPLE_FOR_KNEE,
    min_samples_candidates: Sequence[int] | None = None,
    progress: Callable[[str], None] | None = None,
) -> list[dict]:
    points = reduce_points(scene.points_3d, pipeline)
    results = []
    for min_samples in min_samples_candidates or pipeline_min_samples_candidates(pipeline):
        eps0 = estimate_knee_eps(points, min_samples=min_samples, sample_for_knee=sample_for_knee, seed=seed)
        eps_values = candidate_eps_values(eps0, step=step, pipeline=pipeline)
        if progress is not None:
            progress(f"min_samples={min_samples}: knee_eps={eps0:.3f}, candidates={eps_values}")
        for eps in eps_values:
            results.append(candidate_summary(scene, pipeline=pipeline, eps=eps, min_samples=min_samples))

    return sort_calibration_results(results)


def evaluate_dbscan_grid(
    scene: CalibrationScene,
    *,
    pipeline: PipelineName,
    eps_values: Sequence[float],
    min_samples_candidates: Sequence[int],
    progress: Callable[[str], None] | None = None,
) -> list[dict]:
    rows = []
    total = len(eps_values) * len(min_samples_candidates)
    index = 0
    for min_samples in min_samples_candidates:
        for eps in eps_values:
            index += 1
            if progress is not None:
                progress(f"[{index}/{total}] eps={eps}, min_samples={min_samples}")
            rows.append(candidate_summary(scene, pipeline=pipeline, eps=eps, min_samples=min_samples))
    return sort_calibration_results(rows)


def select_dbscan_parameters(
    points_3d: np.ndarray,
    object_ids: np.ndarray | None,
    *,
    step: float,
    pipeline: PipelineName = "legacy_xz",
    seed: int = DEFAULT_SEED,
    sample_for_knee: int = DEFAULT_SAMPLE_FOR_KNEE,
    min_samples_candidates: Sequence[int] | None = None,
    progress: Callable[[str], None] | None = None,
) -> dict:
    scene = make_calibration_scene(points_3d, object_ids, step=step)
    results = calibrate_dbscan(
        scene,
        step=step,
        pipeline=pipeline,
        seed=seed,
        sample_for_knee=sample_for_knee,
        min_samples_candidates=min_samples_candidates,
        progress=progress,
    )
    if not results:
        raise ValueError("DBSCAN calibration produced no candidates")

    best = dict(results[0])
    best["candidates"] = results
    return best
