from __future__ import annotations

import argparse
import csv
from pathlib import Path

import orjson

from clusters.dbscan_calibration import (
    calibrate_dbscan,
    candidate_labels,
    load_calibration_scene,
    pipeline_mode,
    reduce_points,
    sort_calibration_results,
    summarize_labels,
)
from utils import cluster_utils


DATASETS = (
    ("classroom", Path("data/classroom.gobj_info.json")),
    ("apartments", Path("data/apartments.gobj_info.json")),
    ("bistro", Path("data/bistro.gobj_info.json")),
    ("hogwarts", Path("data/hogwarts.gobj_info.json")),
)


def load_scene(path: Path) -> dict:
    with path.open("rb") as f:
        return orjson.loads(f.read())


def parse_float_values(spec: str) -> list[float]:
    if ":" not in spec:
        return [round(float(item), 6) for item in spec.split(",") if item.strip()]

    parts = spec.split(":")
    if len(parts) != 3:
        raise ValueError(f"range spec should be start:stop:step, got: {spec}")
    start, stop, step = (float(part) for part in parts)
    if step <= 0:
        raise ValueError("range step must be positive")

    values = []
    value = start
    while value <= stop + step * 0.5:
        values.append(round(value, 6))
        value += step
    return values


def parse_int_values(spec: str) -> list[int]:
    return [int(item) for item in spec.split(",") if item.strip()]


def print_scene_summary(name: str, pipeline: str, scene) -> None:
    points = reduce_points(scene.points_3d, pipeline)
    print(f"\n== {name} ==", flush=True)
    print(f"pipeline: {pipeline}", flush=True)
    print(f"object-downsampled points: {scene.object_downsampled_points:,}", flush=True)
    print(f"unique global cells: {scene.unique_global_cells:,}", flush=True)
    print(f"duplicate global cell ratio: {scene.duplicate_ratio:.2%}", flush=True)
    print(f"points for DBSCAN: {len(points):,}", flush=True)
    print(f"bounds min/max: {scene.bounds_min.round(3).tolist()} -> {scene.bounds_max.round(3).tolist()}", flush=True)


def export_cluster_figure(
    name: str,
    pipeline: str,
    scene,
    labels,
    summary: dict,
    *,
    out_dir: Path,
    step: float,
) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)
    stem = f"{name}_{pipeline}_dbscan_eps{summary['eps']}_min{summary['min_samples']}"
    png_path = out_dir / f"{stem}.png"
    cluster_utils.save_figure_image(scene.points_3d, labels, filename=png_path)

    json_path = out_dir / f"{stem}.summary.json"
    json_path.write_bytes(orjson.dumps({
        "dataset": name,
        "pipeline": pipeline,
        "mode": pipeline_mode(pipeline),
        "step": step,
        "point_count_clustered": int(len(scene.points_3d)),
        "point_count_rendered": int(len(scene.points_3d)),
        "object_downsampled_points": int(scene.object_downsampled_points),
        "unique_global_cells": int(scene.unique_global_cells),
        "duplicate_ratio": scene.duplicate_ratio,
        "renderer": "utils.cluster_utils.save_figure_image",
        "labels": "clusters.dbscan post_process" if pipeline == "legacy_xz" else "raw DBSCAN",
        **summary,
    }, option=orjson.OPT_INDENT_2))
    print(f"exported: {png_path}", flush=True)
    print(f"summary:  {json_path}", flush=True)


def write_grid_results(out_dir: Path, rows: list[dict]) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)
    csv_path = out_dir / "grid_results.csv"
    json_path = out_dir / "grid_results.json"

    fieldnames = [
        "rank",
        "dataset",
        "pipeline",
        "eps",
        "min_samples",
        "clusters",
        "noise_ratio",
        "post_noise_ratio",
        "largest_ratio",
        "small_ratio",
        "score",
        "score_labels",
        "png",
        "summary_json",
    ]
    with csv_path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames, extrasaction="ignore")
        writer.writeheader()
        writer.writerows(rows)

    json_path.write_bytes(orjson.dumps(rows, option=orjson.OPT_INDENT_2))
    print(f"grid csv:  {csv_path}", flush=True)
    print(f"grid json: {json_path}", flush=True)


def process_grid_search(
    name: str,
    path: Path,
    *,
    step: float,
    max_points: int | None,
    pipeline: str,
    seed: int,
    eps_values: list[float],
    min_samples_candidates: list[int],
    out_dir: Path,
) -> None:
    scene = load_calibration_scene(
        load_scene(path),
        step=step,
        max_points=max_points,
        seed=seed,
        pipeline=pipeline,
    )
    points = reduce_points(scene.points_3d, pipeline)
    print_scene_summary(name, pipeline, scene)

    rows = []
    total = len(eps_values) * len(min_samples_candidates)
    index = 0
    for min_samples in min_samples_candidates:
        for eps in eps_values:
            index += 1
            print(f"[{index}/{total}] eps={eps}, min_samples={min_samples}", flush=True)
            labels, raw_labels = candidate_labels(
                scene,
                pipeline=pipeline,
                eps=eps,
                min_samples=min_samples,
            )
            summary = summarize_labels(
                points,
                labels,
                eps=eps,
                min_samples=min_samples,
                noise_labels=raw_labels,
            )
            summary["score_labels"] = (
                "raw noise + clusters.dbscan post_process"
                if pipeline == "legacy_xz"
                else "raw DBSCAN"
            )
            export_cluster_figure(name, pipeline, scene, labels, summary, out_dir=out_dir, step=step)
            stem = f"{name}_{pipeline}_dbscan_eps{summary['eps']}_min{summary['min_samples']}"
            rows.append({
                "dataset": name,
                "pipeline": pipeline,
                **summary,
                "png": str(out_dir / f"{stem}.png"),
                "summary_json": str(out_dir / f"{stem}.summary.json"),
            })

    rows = sort_calibration_results(rows)
    for rank, row in enumerate(rows, start=1):
        row["rank"] = rank
    write_grid_results(out_dir, rows)

    print("top grid candidates:", flush=True)
    for item in rows[:10]:
        print(
            f"  #{item['rank']:03d} eps={item['eps']:.3f}, min_samples={item['min_samples']}, "
            f"clusters={item['clusters']}, noise={item['noise_ratio']:.1%}, "
            f"post_noise={item['post_noise_ratio']:.1%}, largest={item['largest_ratio']:.1%}, "
            f"small={item['small_ratio']:.1%}, score={item['score']:.3f}",
            flush=True,
        )


def process_dataset(
    name: str,
    path: Path,
    *,
    step: float,
    max_points: int | None,
    pipeline: str,
    seed: int,
    sample_for_knee: int,
    export_figures: bool,
    out_dir: Path,
) -> None:
    scene = load_calibration_scene(
        load_scene(path),
        step=step,
        max_points=max_points,
        seed=seed,
        pipeline=pipeline,
    )
    points = reduce_points(scene.points_3d, pipeline)
    print_scene_summary(name, pipeline, scene)

    results = calibrate_dbscan(
        scene,
        step=step,
        pipeline=pipeline,
        seed=seed,
        sample_for_knee=sample_for_knee,
        progress=lambda message: print(message, flush=True),
    )
    print("top candidates:", flush=True)
    for item in results[:5]:
        print(
            f"  eps={item['eps']:.3f}, min_samples={item['min_samples']}, "
            f"clusters={item['clusters']}, noise={item['noise_ratio']:.1%}, "
            f"post_noise={item['post_noise_ratio']:.1%}, "
            f"largest={item['largest_ratio']:.1%}, small={item['small_ratio']:.1%}, "
            f"score={item['score']:.3f}",
            flush=True,
        )

    if not export_figures:
        return

    best = results[0]
    print(
        f"auto-selected: eps={best['eps']}, min_samples={best['min_samples']}, score={best['score']:.3f}",
        flush=True,
    )
    if best["clusters"] == 0:
        print("skip export: all automatic candidates are all-noise results", flush=True)
        return
    labels, raw_labels = candidate_labels(
        scene,
        pipeline=pipeline,
        eps=best["eps"],
        min_samples=best["min_samples"],
    )
    final_summary = summarize_labels(
        points,
        labels,
        eps=best["eps"],
        min_samples=best["min_samples"],
        noise_labels=raw_labels,
    )
    final_summary["selection_score"] = best["score"]
    final_summary["score_labels"] = best["score_labels"]
    export_cluster_figure(name, pipeline, scene, labels, final_summary, out_dir=out_dir, step=step)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--step", type=float, default=0.5)
    parser.add_argument("--max-points", type=int, default=None)
    parser.add_argument("--sample-for-knee", type=int, default=50_000)
    parser.add_argument("--pipeline", choices=("legacy_xz", "dedup_xyz"), default="legacy_xz")
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--dataset", choices=[name for name, _ in DATASETS] + ["all"], default="all")
    parser.add_argument("--export-figures", action="store_true")
    parser.add_argument("--out-dir", type=Path, default=Path("figures/auto"))
    parser.add_argument("--grid-search", action="store_true")
    parser.add_argument("--eps-values", default="0.50:1.50:0.05")
    parser.add_argument("--min-samples-candidates", default="40,60,80,100,150,200")
    args = parser.parse_args()

    for name, path in DATASETS:
        if args.dataset not in ("all", name):
            continue
        if args.grid_search:
            process_grid_search(
                name,
                path,
                step=args.step,
                max_points=args.max_points,
                pipeline=args.pipeline,
                seed=args.seed,
                eps_values=parse_float_values(args.eps_values),
                min_samples_candidates=parse_int_values(args.min_samples_candidates),
                out_dir=args.out_dir,
            )
            continue
        process_dataset(
            name,
            path,
            step=args.step,
            max_points=args.max_points,
            pipeline=args.pipeline,
            seed=args.seed,
            sample_for_knee=args.sample_for_knee,
            export_figures=args.export_figures,
            out_dir=args.out_dir,
        )


if __name__ == "__main__":
    main()
