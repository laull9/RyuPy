#!/usr/bin/env python3
"""独立活模块字典差分门：先生成固定 CPython 答案，再验证 JIT/AOT/删除输入搬迁。

本门保留 pending 输入的原始合同；显式调用会运行 pending，失败保持非零退出。
证据目录必须在仓库外，产品构建与发行输入在 finally 中清理。
"""
import argparse
import json
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT))

from tests.cpython.projects_support import (
    ProjectSpec, prepare_clean_distribution, run_oracle_process, run_ryupy_process,
)
from tests.cpython.support import external_workspace


def contract(kind="functions"):
    """手写探针使用同一套私有 payload、构建与搬迁协议，不修改固定20项目。"""
    fixtures = ROOT / "tests/cpython/fixtures"
    return ProjectSpec(
        project_id="live-module-dict", category="模块字典", pypi_name="bridge_fixture",
        version="handwritten", import_name="bridge_fixture", entry_rel="live_module_dict.py",
        test_scope="真实dict身份/双向写删/回调/别名/异常", deps_raw="-",
        entry_abs=fixtures / ("live_module_dict_entry.py" if kind == "entry" else "live_module_dict.py"), source_dir=fixtures,
    )


def run(cli, evidence, source_label, modes, kind="functions"):
    evidence = evidence.resolve()
    if evidence == ROOT or ROOT in evidence.parents:
        raise ValueError("验证证据必须保存到仓库外")
    evidence.mkdir(parents=True, exist_ok=True)
    success = True
    for mode in modes:
        workspace = external_workspace("ryupy-live-module-dict-")
        start = time.monotonic()
        try:
            oracle_cwd, actual_cwd = workspace / "oracle", workspace / "candidate"
            oracle_cwd.mkdir()
            actual_cwd.mkdir()
            project = contract(kind)
            oracle = run_oracle_process(project, oracle_cwd)
            if oracle[0] != 0:
                raise RuntimeError(f"固定 CPython 合同失败：{oracle}")
            prepare_clean_distribution(workspace / "dist")
            actual = run_ryupy_process(project, actual_cwd, workspace / "dist",
                                       mode=mode, ryupy_cli=cli)
            matched = tuple(actual[:3]) == oracle
            result = dict(source=source_label, cli=str(cli), mode=mode, oracle=oracle,
                          actual=actual, passed=matched,
                          pending="# ryupy: pending" in project.entry_abs.read_text(encoding="utf-8"),
                          duration_sec=round(time.monotonic() - start, 3))
            (evidence / f"{mode}.json").write_text(
                json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
            print(mode, "PASS" if matched else "FAIL", flush=True)
            success = success and matched
        finally:
            shutil.rmtree(workspace)
    return success


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cli", type=Path, required=True)
    parser.add_argument("--evidence", type=Path, required=True)
    parser.add_argument("--source-label", required=True)
    parser.add_argument("--contract", choices=["entry", "functions"], default="functions")
    parser.add_argument("--mode", choices=["jit", "aot", "reloc", "all"], default="all")
    args = parser.parse_args()
    modes = ["jit", "aot", "reloc"] if args.mode == "all" else [args.mode]
    return 0 if run(args.cli.resolve(), args.evidence, args.source_label, modes, args.contract) else 1


if __name__ == "__main__":
    raise SystemExit(main())
