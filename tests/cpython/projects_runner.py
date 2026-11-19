# -*- coding: utf-8 -*-
"""CPython Provider 20个固定项目的验收执行驱动。

执行方式：
    python3 tests/cpython/projects_runner.py --mode jit
    python3 tests/cpython/projects_runner.py --mode aot --project six
    python3 tests/cpython/projects_runner.py --mode all --output-json results.json

核心规范：
1. 保留 manifest.tsv / lock.tsv 中的原始版本、原入口、原业务输入；
2. 每次同次生成 CPython 3.12.13 Oracle，采用独立隔离 cwd 与私有分发；
3. 严格逐项记录并比对真实 stdout/stderr/退出码；
4. 区分编译失败 (COMPILE_FAIL)、运行失败 (RUNTIME_FAIL) 与环境阻塞 (ENV_BLOCKED)；
5. 所有临时文件均在仓库外生成并在运行完毕后自动清理。
"""

import argparse
import json
import os
import re
from pathlib import Path
import shutil
import sys
import tempfile
import time
from typing import Dict, List, Optional

# 确保仓库根目录在 sys.path 中
_REPO_ROOT = Path(__file__).resolve().parents[2]
if str(_REPO_ROOT) not in sys.path:
    sys.path.insert(0, str(_REPO_ROOT))

# 引用 projects_support 支持
from tests.cpython.projects_support import (
    ProjectSpec,
    load_manifest,
    get_project_by_id,
    prepare_clean_distribution,
    run_oracle_process,
    run_ryupy_process,
)
from tests.cpython.support import external_workspace, ROOT


def parse_arguments():
    parser = argparse.ArgumentParser(description="RyuPy CPython Provider 20项目验收驱动")
    parser.add_argument(
        "--mode",
        choices=["jit", "aot", "reloc", "all"],
        default="jit",
        help="验收模式: jit (JIT运行), aot (NativeAOT构建运行), reloc (搬迁后运行), all (全部)",
    )
    parser.add_argument(
        "--project",
        type=str,
        default=None,
        help="指定单个测试项目 ID (如 six, markdown, attrs)，默认跑全量20项",
    )
    parser.add_argument(
        "--output-json",
        type=str,
        default=None,
        help="指定测试结果输出 JSON 文件路径",
    )
    parser.add_argument(
        "--keep-temp",
        action="store_true",
        help="调试选项：保留仓库外临时目录不清理",
    )
    return parser.parse_args()


def normalize_warning_paths(text, aliases):
    """只规范已知载荷的警告头路径，保留警告类型、行号、正文及所有其他stderr字节。"""
    result = []
    for line in text.splitlines(keepends=True):
        for path, alias in sorted(aliases.items(), key=lambda pair: len(pair[0]), reverse=True):
            if line.startswith(path) and re.match(r'(?:/[^:\n]*)?:[0-9]+: [A-Za-z]+Warning:', line[len(path):]):
                line = alias + line[len(path):]
                break
        result.append(line)
    return ''.join(result)


def run_single_test(
    project: ProjectSpec,
    mode: str,
    keep_temp: bool = False,
) -> Dict:
    """运行单个项目的单个模式测试，返回详细结果字典。"""
    # 建立仓库外顶级隔离临时工作区
    workspace = external_workspace(f"ryupy-proj-{project.project_id}-{mode}-")
    work_dir = workspace / "work"
    dist_dir = workspace / "dist"
    work_dir.mkdir(parents=True, exist_ok=True)

    start_time = time.time()
    result = {
        "project_id": project.project_id,
        "category": project.category,
        "version": project.version,
        "entry": project.entry_rel,
        "mode": mode,
        "status": "UNKNOWN",
        "oracle_exit": -1,
        "oracle_stdout": "",
        "oracle_stderr": "",
        "ryupy_exit": -1,
        "ryupy_stdout": "",
        "ryupy_stderr": "",
        "stdout_matched": False,
        "failure_reason": "",
        "duration_sec": 0.0,
    }

    try:
        # 1. 准备私有纯净 CPython 发行目录
        prepare_clean_distribution(dist_dir)

        # 2. 运行 Oracle（CPython 3.12.13）
        oracle_work = workspace / "oracle_cwd"
        oracle_work.mkdir(parents=True, exist_ok=True)
        o_exit, o_stdout, o_stderr = run_oracle_process(project, oracle_work)
        result["oracle_exit"] = o_exit
        result["oracle_stdout"] = o_stdout
        result["oracle_stderr"] = o_stderr

        if o_exit != 0:
            result["status"] = "ENV_BLOCKED"
            result["failure_reason"] = f"CPython Oracle自身运行失败(exit={o_exit}): {o_stderr[:200]}"
            return result

        # 3. 运行 RyuPy（独立 cwd）
        ryupy_cwd = workspace / "ryupy_cwd"
        ryupy_cwd.mkdir(parents=True, exist_ok=True)
        r_exit, r_stdout, r_stderr, aliases = run_ryupy_process(project, ryupy_cwd, dist_dir, mode=mode)
        result["ryupy_exit"] = r_exit
        result["ryupy_stdout"] = r_stdout
        result["ryupy_stderr"] = r_stderr

        # 4. 判定与比对
        if r_stderr.startswith("[BUILD FAILED]"):
            result["status"] = "COMPILE_FAIL"
            result["failure_reason"] = f"AOT 编译失败: {r_stderr[:200]}"
        elif r_exit != 0:
            result["status"] = "RUNTIME_FAIL"
            result["failure_reason"] = f"运行时退出码非0(exit={r_exit}): {r_stderr[:200]}"
        else:
            # 严格 stdout 字符一致性比对
            matched = (o_stdout == r_stdout and o_exit == r_exit
                       and normalize_warning_paths(o_stderr, aliases) == normalize_warning_paths(r_stderr, aliases))
            oracle_files = {str(path.relative_to(oracle_work)): path.read_bytes() for path in oracle_work.rglob("*") if path.is_file()}
            actual_files = {str(path.relative_to(ryupy_cwd / "run")): path.read_bytes() for path in (ryupy_cwd / "run").rglob("*") if path.is_file()}
            matched = matched and oracle_files == actual_files
            result["stdout_matched"] = matched
            if matched:
                result["status"] = "PASS"
            else:
                result["status"] = "RUNTIME_FAIL"
                result["failure_reason"] = "三流/退出码或文件副作用与CPython不一致"

    except Exception as exc:
        result["status"] = "ENV_BLOCKED"
        result["failure_reason"] = f"测试执行异常: {str(exc)}"

    finally:
        result["duration_sec"] = round(time.time() - start_time, 2)
        if not keep_temp and workspace.exists():
            try:
                shutil.rmtree(workspace)
            except Exception:
                pass

    return result


def format_table_row(items: List[str], widths: List[int]) -> str:
    """生成格式整齐的表格行。"""
    row = []
    for item, width in zip(items, widths):
        item_str = str(item)
        if len(item_str) > width:
            item_str = item_str[: width - 3] + "..."
        row.append(item_str.ljust(width))
    return " | ".join(row)


def main():
    args = parse_arguments()
    all_projects = load_manifest()

    if args.project:
        target_projects = [get_project_by_id(args.project)]
    else:
        target_projects = all_projects

    modes_to_run = ["jit", "aot", "reloc"] if args.mode == "all" else [args.mode]
    results = []

    print("=" * 80)
    print(f"RyuPy CPython Provider 固定20项目验收测试启动")
    print(f"模式: {modes_to_run} | 被测项目数: {len(target_projects)}")
    print("=" * 80)

    for p_idx, project in enumerate(target_projects, start=1):
        for mode in modes_to_run:
            print(f"[{p_idx:02d}/{len(target_projects):02d}] 正在执行 {project.project_id} (mode={mode})...", end="", flush=True)
            res = run_single_test(project, mode, keep_temp=args.keep_temp)
            results.append(res)
            print(f" -> {res['status']} ({res['duration_sec']}s)")
            if res["status"] != "PASS":
                print(f"     原因: {res['failure_reason']}")

    # 打印汇总表格
    print("\n" + "=" * 95)
    headers = ["ID", "Category", "Mode", "Status", "ExitCode", "Matched", "Duration", "Notes"]
    col_widths = [18, 16, 7, 14, 8, 8, 10, 20]
    print(format_table_row(headers, col_widths))
    print("-" * 95)

    passed_count = sum(1 for r in results if r["status"] == "PASS")
    total_count = len(results)

    for r in results:
        matched_str = "YES" if r["stdout_matched"] else "NO"
        notes = r["failure_reason"] if r["status"] != "PASS" else "OK"
        row = [
            r["project_id"],
            r["category"],
            r["mode"],
            r["status"],
            str(r["ryupy_exit"]),
            matched_str,
            f"{r['duration_sec']}s",
            notes,
        ]
        print(format_table_row(row, col_widths))

    print("-" * 95)
    print(f"总结: 总计执行 {total_count} 项, 通过 {passed_count} 项, 失败 {total_count - passed_count} 项")
    print("=" * 95)

    if args.output_json:
        out_p = Path(args.output_json).resolve()
        assert not out_p.is_relative_to(ROOT), f"验收输出必须位于源码仓库外，禁止写回仓库: {out_p}"
        out_p.parent.mkdir(parents=True, exist_ok=True)
        out_p.write_text(json.dumps(results, indent=2, ensure_ascii=False), encoding="utf-8")
        print(f"结果已写入(仓库外): {out_p}")

    # 已知失败仍保留非零退出；验收报告逐项目登记，不用白名单把失败门涂绿。
    sys.exit(1 if any(result['status'] != 'PASS' for result in results) else 0)


if __name__ == "__main__":
    main()
