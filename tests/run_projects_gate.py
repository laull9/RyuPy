#!/usr/bin/env python3
"""运行固定20入口并检查保留/晋升门；不接受离线旧报告，不下载、不修改 golden 或保护合同。"""

import argparse
from collections import Counter
import datetime
import json
import math
import os
from pathlib import Path
import shutil
import signal
import subprocess
import sys
import time
import uuid

from pinned_python import resolve
from project_gate_contract import (GateError, audit_sources, digest, evaluate,
                                   load_inputs, validate_result)

ROOT = Path(__file__).resolve().parent.parent
MAX_STREAM = 8 * 1024 * 1024


def write_json(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def run_process(command, cwd, env, prefix, timeout, watch):
    """保留原字节流；限时/超量时终止并回收进程组，绝不把截断流当作通过证据。"""
    if os.name != "posix":
        raise GateError("当前门禁的进程组监管只验收 POSIX 平台")
    start = time.monotonic()
    reason = None
    with prefix.with_suffix(".stdout").open("wb") as out, prefix.with_suffix(".stderr").open("wb") as err:
        process = subprocess.Popen(command, cwd=cwd, env=env, stdin=subprocess.DEVNULL,
                                   stdout=out, stderr=err, start_new_session=True)
        try:
            while process.poll() is None:
                if time.monotonic() - start > timeout:
                    reason = "timeout"
                elif any(p.stat().st_size > MAX_STREAM for p in watch.rglob("*")
                         if p.is_file() and p.suffix in (".stdout", ".stderr", ".txt")):
                    reason = "stream-limit"
                if reason:
                    break
                time.sleep(0.02)
        finally:
            # 正常根进程退出也清理遗留同组子进程，防止它们在核验之后继续改证据。
            try:
                os.killpg(process.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
            process.wait()
    if any(p.stat().st_size > MAX_STREAM for p in watch.rglob("*")
           if p.is_file() and p.suffix in (".stdout", ".stderr", ".txt")):
        reason = reason or "stream-limit"
    result = {"command": [str(v) for v in command], "exit": process.returncode,
              "failure": reason, "elapsed_seconds": time.monotonic() - start,
              "raw_streams_complete": reason is None}
    write_json(prefix.with_suffix(".json"), result)
    if reason or process.returncode != 0:
        raise GateError(f"进程失败：{prefix.name} exit={process.returncode} {reason or ''}")
    return result


def candidate_identity(root):
    """允许未提交候选；记录 HEAD 与候选实际文件，运行前后必须一致。"""
    def git(*args):
        return subprocess.check_output(["git", "-C", str(root), *args])
    paths = set(git("ls-files", "-z").decode().split("\0"))
    paths.update(git("ls-files", "--others", "--exclude-standard", "-z").decode().split("\0"))
    files = {name: digest(root / name) if (root / name).is_file() else "missing"
             for name in sorted(paths) if name}
    return {"head": git("rev-parse", "HEAD").decode().strip(),
            "status": git("status", "--porcelain=v1", "--untracked-files=all").decode(), "files": files}


def executable(value):
    found = shutil.which(str(value))
    if not found:
        raise GateError(f"找不到可执行文件：{value}")
    return Path(found).resolve()


def execute(options, output):
    contract, projects = load_inputs(ROOT)
    store = options.source_store.resolve()
    runtime = options.runtime.resolve()
    runner, python, dotnet = map(executable, (options.runner, options.python, options.dotnet))
    tools = {str(path): digest(path) for path in (runner, python, dotnet, runtime)}
    identity = candidate_identity(ROOT)
    sources, roots = audit_sources(ROOT, store, projects)
    run_id = uuid.uuid4().hex
    # 白名单继承宿主定位信息，不继承 PYTHONPATH、RYUPY_*、COMPlus_*、tier/PGO/实验开关。
    env = {key: value for key, value in os.environ.items()
           if key in {"PATH", "HOME", "USER", "SYSTEMROOT", "DOTNET_ROOT", "DOTNET_ROOT_X64", "TMPDIR"}}
    env.update(PYTHONDONTWRITEBYTECODE="1", PYTHONHASHSEED="0", PYTHON_COLORS="0", NO_COLOR="1",
               DOTNET=str(dotnet), RYUPY_RUNTIME_DIR=str(runtime.parent))
    cp_env = dict(env, PYTHONPATH=os.pathsep.join(roots))
    metadata = {"schema": 1, "run_id": run_id, "mode": "default-jit", "completed": False,
                "started": datetime.datetime.now(datetime.timezone.utc).isoformat(),
                "tools": tools, "source_store": str(store), "candidate": identity,
                "source_files": sum(map(len, sources.values())), "contract": contract}
    write_json(output / "run.json", metadata)
    write_json(output / "sources.json", sources)
    run_process([python, "-S", "-B", "-c", "import platform; print(platform.python_version()); print(platform.python_implementation())"],
                output, cp_env, output / "python-version", 10, output)
    version = (output / "python-version.stdout").read_text().splitlines()
    if version != [(ROOT / "tests/pinned-python.txt").read_text().strip(), "CPython"]:
        raise GateError(f"解释器不是固定 CPython：{version}")
    rows = []
    for project in projects:
        pid = project["id"]
        case = output / pid
        case.mkdir()
        entry = ROOT / "tests/projects" / project["entry"]
        golden = ROOT / "tests/projects/baselines" / pid / "stdout.txt"
        run_process([python, "-S", "-B", entry], case, cp_env, case / "cp", 60, case)
        if (case / "cp.stdout").read_bytes() != golden.read_bytes():
            raise GateError(f"CP stdout 不等于原 golden：{pid}")
        run_process([runner, "--only", pid, "--source-store", store, "--out-dir", case / "native",
                     "--run-id", run_id], ROOT, env, case / "runner", options.timeout, case)
        lines = (case / "native/projects.jsonl").read_text(encoding="utf-8").splitlines()
        if len(lines) != 1:
            raise GateError(f"每项运行必须恰好一条完整结果：{pid}")
        row = json.loads(lines[0])
        if not isinstance(row, dict):
            raise GateError(f"项目报告必须是JSON对象：{pid}")
        directory = case / "native" / pid
        validate_result(row, project, run_id, directory, golden, tools[str(runtime)])
        row["evidence"] = {str(p.relative_to(output)): digest(p) for p in sorted(case.rglob("*")) if p.is_file()}
        rows.append(row)
        write_json(output / "matrix.partial.json", rows)
        print(f"{pid}: {row['status']} {row['detail']}", flush=True)
    if candidate_identity(ROOT) != identity:
        raise GateError("运行中候选工作树发生改变")
    if any(digest(Path(path)) != sha for path, sha in tools.items()):
        raise GateError("运行中工具发生改变")
    after, _ = audit_sources(ROOT, store, projects)
    if after != sources:
        raise GateError("运行中源码发生改变")
    # 防止结束前原流被覆盖；错误一律保留部分矩阵，不发布成功摘要。
    for row in rows:
        if any(digest(output / name) != sha for name, sha in row["evidence"].items()):
            raise GateError("运行中证据文件发生改变")
    summary = evaluate(contract, rows)
    summary.update(valid=True, counts=dict(Counter(row["status"] for row in rows)),
                   run_id=run_id, cp_passed=20)
    write_json(output / "matrix.json", rows)
    metadata.update(completed=True, ended=datetime.datetime.now(datetime.timezone.utc).isoformat())
    write_json(output / "run.json", metadata)
    write_json(output / "summary.json", summary)
    print(json.dumps(summary, ensure_ascii=False), flush=True)
    print("保留通过只表示当前十项（含恢复后的 progress）逐ID未丢失；不证明新项目或AOT/显式配置。")
    return 0 if summary["preserve_pass" if options.gate == "preserve" else "promotion_pass"] else 1


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--gate", choices=("preserve", "promote"), required=True)
    parser.add_argument("--runner", required=True, help="本候选构建的 projects 二进制；make 目标自动构建")
    parser.add_argument("--source-store", type=Path, default=ROOT / "third_party/projects")
    parser.add_argument("--runtime", type=Path, default=ROOT / "runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll")
    parser.add_argument("--python", default=resolve())
    parser.add_argument("--dotnet", default=os.environ.get("DOTNET", "dotnet"))
    parser.add_argument("--output", type=Path, help="必须不存在的证据目录；默认创建 target/e2e/project-gates/<随机ID>")
    parser.add_argument("--timeout", type=float, default=180, help="每项目编译+JIT的秒数上限")
    options = parser.parse_args()
    output = (options.output or ROOT / "target/e2e/project-gates" / uuid.uuid4().hex).resolve()
    try:
        if not math.isfinite(options.timeout) or options.timeout <= 0:
            raise GateError("timeout 必须为正")
        output.parent.mkdir(parents=True, exist_ok=True)
        output.mkdir()  # 不覆盖任何已有证据，即便已有运行失败。
    except (OSError, GateError) as error:
        print(f"门禁未启动：{error}", file=sys.stderr)
        return 2
    try:
        return execute(options, output)
    except (OSError, ValueError, KeyError, subprocess.SubprocessError) as error:
        try:
            write_json(output / "summary.json", {"valid": False, "preserve_pass": False,
                       "promotion_pass": False, "error": str(error)})
        except OSError:
            # 磁盘/权限错误连失败摘要也可能写不出，但退出码仍然必须是失败。
            pass
        print(f"门禁无效/失败：{error}；证据：{output}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
