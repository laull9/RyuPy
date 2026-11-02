#!/usr/bin/env python3
"""固定 Python/JIT/真实 NativeAOT 三臂验收；失败保留日志，仅删除本次自建目录。"""
import argparse
import json
import os
from pathlib import Path
import platform
import re
import signal
import shutil
import subprocess
import tempfile


MODES = {"all": [], "argv": ["argv", "", "a b", "中文", "--flag"],
         "exit": ["exit"], "error": ["error"]}
IMAGE = "mcr.microsoft.com/dotnet/runtime-deps:10.0.14-noble"


def traceback_signature(stderr):
    """仅排除路径前缀/源码展示；保留每个帧名、行号、函数及最终错误。"""
    text = stderr.decode("utf-8", errors="strict")
    frames = [(Path(name).name, int(line), function) for name, line, function in
              re.findall(r'File "([^"]+)", line (\d+), in ([^\n]+)', text)]
    lines = text.rstrip().splitlines()
    if not frames or not lines or lines[-1] != "SampleError: aot-user-error":
        raise AssertionError(f"异常报告不满足公开 Python 栈协议：{text}")
    if "Unhandled exception" in text:
        raise AssertionError("泄漏 CLR 异常")
    return frames, lines[-1]


class Runner:
    def __init__(self, logs):
        self.logs = logs

    def run(self, label, command, *, cwd, env=None, check=False, timeout=180):
        prefix = self.logs / label
        prefix.with_suffix(".command.json").write_text(
            json.dumps(command, ensure_ascii=False, indent=2), encoding="utf-8")
        # 独占会话/进程组覆盖 CLI→dotnet→ILC；超时不能只杀最外层CLI。
        process = subprocess.Popen(command, cwd=cwd, env=env, stdout=subprocess.PIPE,
                                   stderr=subprocess.PIPE, start_new_session=True)
        try:
            stdout, stderr = process.communicate(timeout=timeout)
        except subprocess.TimeoutExpired:
            try:
                os.killpg(process.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
            stdout, stderr = process.communicate()
            prefix.with_suffix(".stdout").write_bytes(stdout)
            prefix.with_suffix(".stderr").write_bytes(stderr)
            prefix.with_suffix(".status.json").write_text(json.dumps({
                "timeout": True, "exit": process.returncode, "killed_process_group": process.pid}))
            raise
        result = subprocess.CompletedProcess(command, process.returncode, stdout, stderr)
        prefix.with_suffix(".stdout").write_bytes(result.stdout)
        prefix.with_suffix(".stderr").write_bytes(result.stderr)
        prefix.with_suffix(".status.json").write_text(json.dumps({"exit": result.returncode}))
        if check and result.returncode:
            raise RuntimeError(f"{label} 失败，exit={result.returncode}；见 {prefix}.stderr")
        return result


def compare(reference, actual, mode, *, python=False):
    expected_code = {"exit": 7, "error": 1}.get(mode, 0)
    assert reference.returncode == actual.returncode == expected_code, mode
    assert reference.stdout == actual.stdout, f"{mode}: stdout 不同"
    if python and mode == "error":
        assert traceback_signature(reference.stderr) == traceback_signature(actual.stderr)
    else:
        assert reference.stderr == actual.stderr, f"{mode}: stderr 不同"
    if mode != "error":
        assert not actual.stderr, f"{mode}: 非预期 stderr"


def executable(value):
    found = shutil.which(value)
    if not found:
        raise FileNotFoundError(f"未找到执行工具：{value}")
    return str(Path(found).resolve())


def published_native(stderr, output):
    """只认本次成功 CLI 的唯一原生发布行，不从旧文件或 glob 猜产物。"""
    generated = [line.removeprefix("已生成 ") for line in
                 stderr.decode("utf-8", errors="strict").splitlines()
                 if line.startswith("已生成 ")]
    candidates = [Path(value) for value in generated if not value.endswith(".dll")]
    if len(candidates) != 1:
        raise AssertionError(f"CLI 未报告唯一 native 产物：{generated}")
    native = candidates[0]
    if not native.is_absolute() or not native.is_file():
        raise AssertionError(f"CLI native 不是现存绝对文件：{native}")
    resolved = native.resolve()
    relative = resolved.relative_to(output.resolve())
    if (len(relative.parts) != 3 or not relative.parts[0].startswith("ryupy-aot-")
            or relative.parts[1] != "publish"):
        raise AssertionError(f"native 不满足 driver 独占工作目录合同：{resolved}")
    return resolved


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--python", default="python3.12")
    parser.add_argument("--dotnet", default=os.environ.get("DOTNET", "dotnet"))
    parser.add_argument("--ryupy", default="target/debug/ryupy")
    parser.add_argument("--rid", default="linux-x64", choices=["linux-x64"])
    parser.add_argument("--prepare-only", action="store_true")
    args = parser.parse_args()
    if platform.system() != "Linux" or platform.machine() != "x86_64":
        raise RuntimeError("本验收仅支持真实 Linux x86_64 宿主，不提供交叉发布验收")
    args.out.mkdir(parents=True, exist_ok=True)
    # 唯一自建子目录拥有删除权；不清理用户给定目录或其它历史运行。
    work = Path(tempfile.mkdtemp(prefix="aot-smoke-", dir=args.out.resolve()))
    logs = work / "logs"
    logs.mkdir()
    print(f"本轮产物与日志：{work}", flush=True)
    run = Runner(logs)
    python, dotnet, ryupy = map(executable, [args.python, args.dotnet, args.ryupy])
    version = run.run("python-version", [python, "-c",
                      "import sys; print(sys.implementation.name); print('.'.join(map(str,sys.version_info[:3])))"],
                      cwd=work, check=True)
    assert version.stdout.strip() == b"cpython\n3.12.13", "必须使用固定 CPython 3.12.13"
    source = work / "源码 & input with spaces"
    il = work / "产物 & output with spaces"
    source.mkdir()
    for name in ("main.py", "state.py"):
        shutil.copy2(Path(__file__).parent / "fixtures" / name, source / name)
    run.run("compile", [ryupy, "build", str(source / "main.py"), "--out-dir", str(il)],
            cwd=work, check=True, timeout=600)
    baseline, jit = {}, {}
    for mode, values in MODES.items():
        baseline[mode] = run.run(f"python-{mode}", [python, str(source / "main.py"), *values], cwd=work)
        jit[mode] = run.run(f"jit-{mode}", [dotnet, str(il / "main.dll"), *values], cwd=work)
        compare(baseline[mode], jit[mode], mode, python=True)
    if args.prepare_only:
        (work / "result.json").write_text('{"prepare_only": true, "python_jit": "passed"}')
        return
    # 真正走公开 CLI/driver AOT 路径，不能退回测试脚本自造宿主。
    published = run.run("publish", [ryupy, "build", str(source / "main.py"),
                                  "--out-dir", str(il), "--aot"],
                        cwd=work, env={**os.environ, "DOTNET": dotnet},
                        check=True, timeout=1800)
    native = published_native(published.stderr, il)
    publish = native.parent
    # driver 保留成功 SDK 原始双流；清理构建树前必须归档，不靠 CLI 摘要。
    for name in ("publish.stdout.log", "publish.stderr.log"):
        shutil.copy2(publish.parent / name, logs / ("driver-" + name))
    relocated = work / "relocated"
    relocated.mkdir()
    binary = relocated / native.name
    shutil.copy2(native, binary)
    with binary.open("rb") as stream:
        assert stream.read(4) == b"\x7fELF", "发布产物必须是真实 ELF 原生文件"
    # Linux 系统库不打包；仅发布目录确有的本机共享库允许随程序搬迁。
    for library in publish.glob("*.so*"):
        if library.is_file():
            shutil.copy2(library, relocated / library.name)
    assert not list(relocated.glob("*.dll")) and not list(relocated.glob("*.json"))
    run.run("file", [executable("file"), str(binary)], cwd=work, check=True)
    run.run("ldd", [executable("ldd"), str(binary)], cwd=work, check=True)
    # 留日志和搬迁产物；删除的每个路径都由本轮脚本创建。
    for directory in (source, il):
        shutil.rmtree(directory)
    clean = {"PATH": "/usr/bin:/bin", "HOME": str(work), "LANG": "C.UTF-8"}
    for mode, values in MODES.items():
        native = run.run(f"native-{mode}", [str(binary), *values], cwd=relocated, env=clean)
        compare(jit[mode], native, mode)
    docker = executable("docker")
    # 容器只挂载搬迁产物；镜像不包含 SDK/runtime/Python，运行时禁网。
    script = ('for tool in dotnet python python3 python3.12; do '
              'if command -v "$tool" >/dev/null 2>&1; then exit 97; fi; done; '
              'binary=$1; shift; exec "$binary" "$@"')
    for mode, values in MODES.items():
        result = run.run(f"docker-{mode}", [docker, "run", "--rm", "--network", "none",
                         "--read-only", "--mount", f"type=bind,src={relocated},dst=/app,readonly",
                         "--workdir", "/app", "--entrypoint", "/bin/sh", IMAGE,
                         "-c", script, "aot-smoke", "/app/" + binary.name, *values], cwd=work, timeout=300)
        compare(jit[mode], result, mode)
    (work / "result.json").write_text(json.dumps({"python_jit_native_relocated": "passed",
                                                "rid": args.rid, "image": IMAGE}, indent=2))


if __name__ == "__main__":
    main()
