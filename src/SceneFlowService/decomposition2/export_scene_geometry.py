from __future__ import annotations

import subprocess
from dataclasses import dataclass
from pathlib import Path


# =========================
# Global export config.
# Edit these values directly before running this file.
# =========================

UNITY_EXE = r"C:\Program Files\Unity\Hub\Editor\2022.3.53f1c1\Editor\Unity.exe"
FORCE_EXPORT = False
INCLUDE_INACTIVE = False
LOG_DIR = "SceneFlowService/results/geometry_export_logs"

DECOMPOSITION2_DIR = Path(__file__).resolve().parent
SERVICE_DIR = DECOMPOSITION2_DIR.parent
WORKSPACE_DIR = SERVICE_DIR.parent
DATA_DIR = SERVICE_DIR / "data"


@dataclass(frozen=True)
class SceneExportTarget:
    name: str
    project_path: Path
    scene_asset_path: str
    output_json: Path


TARGETS = (
    SceneExportTarget(
        name="sfclassroom",
        project_path=WORKSPACE_DIR / "Apps" / "SFClassroom",
        scene_asset_path="Assets/default.unity",
        output_json=DATA_DIR / "sfclassroom.geometry.json",
    ),
    SceneExportTarget(
        name="sfapartments",
        project_path=WORKSPACE_DIR / "Apps" / "SFApartments",
        scene_asset_path="Assets/Scenes/Apartments.unity",
        output_json=DATA_DIR / "sfapartments.geometry.json",
    ),
)


def validate_target(target: SceneExportTarget) -> None:
    if not target.project_path.exists():
        raise FileNotFoundError(f"Unity project not found: {target.project_path}")
    scene_path = target.project_path / target.scene_asset_path
    if not scene_path.exists():
        raise FileNotFoundError(f"Unity scene not found: {scene_path}")


def export_target(
    *,
    unity_exe: Path,
    target: SceneExportTarget,
    force: bool,
    include_inactive: bool,
    log_dir: Path,
) -> None:
    validate_target(target)
    target.output_json.parent.mkdir(parents=True, exist_ok=True)
    log_dir.mkdir(parents=True, exist_ok=True)

    if target.output_json.exists() and not force:
        print(f"[skip] {target.name}: {target.output_json} already exists")
        return

    log_file = log_dir / f"{target.name}.unity.log"
    command = [
        str(unity_exe),
        "-batchmode",
        "-quit",
        "-projectPath",
        str(target.project_path),
        "-executeMethod",
        "SceneFlowTools.Editor.SceneGeometryExportBatch.Run",
        "-scenePath",
        target.scene_asset_path,
        "-outputJson",
        str(target.output_json),
        "-includeInactive",
        str(include_inactive).lower(),
        "-meshOnly",
        "true",
        "-logFile",
        str(log_file),
    ]

    print(f"[run] {target.name}: {' '.join(command)}")
    completed = subprocess.run(command, cwd=WORKSPACE_DIR)
    if completed.returncode != 0:
        raise RuntimeError(
            f"Unity export failed for {target.name} with exit code {completed.returncode}. "
            f"See log: {log_file}"
        )
    print(f"[ok] {target.name}: {target.output_json}")


def main() -> None:
    unity_exe = Path(UNITY_EXE).resolve()
    if not unity_exe.exists():
        raise FileNotFoundError(f"Unity executable not found: {unity_exe}")

    for target in TARGETS:
        export_target(
            unity_exe=unity_exe,
            target=target,
            force=FORCE_EXPORT,
            include_inactive=INCLUDE_INACTIVE,
            log_dir=(WORKSPACE_DIR / LOG_DIR).resolve(),
        )


if __name__ == "__main__":
    main()
