"""配对AOT的实际SDK清单证据和本次独占构建树清理；不修改产品构建语义。"""
import json
from pathlib import Path
import shutil

MANIFEST = "ryupy-artifact.json"
SDK = "10.0.403"


def save_manifest(executable: Path, evidence: Path, arm: str, case: str):
    source = executable.parent / MANIFEST
    destination = evidence / case / arm / MANIFEST
    destination.parent.mkdir(parents=True, exist_ok=True)
    # 先保全原始清单，再校验，错误SDK仍有可审查证据。
    shutil.copyfile(source, destination)
    data = json.loads(destination.read_text())
    assert data["toolchain"]["sdk_version"] == SDK, "实际AOT SDK不符"
    assert data["toolchain"]["target_framework_major"] == 10
    return destination


def require_space(root: Path, minimum_bytes: int):
    free = shutil.disk_usage(root).free
    if free < minimum_bytes:
        raise RuntimeError(f"本次构建剩余空间不足：free={free}, required={minimum_bytes}")
    return free


def cleanup_case(case_root: Path, owned_root: Path):
    # 仅删除本次TemporaryDirectory中当前用例的直接子树，禁止链接和越界路径。
    assert not case_root.is_symlink(), "拒绝链接构建树"
    assert case_root.resolve().parent == owned_root.resolve(), "拒绝清理非本次用例树"
    shutil.rmtree(case_root)
