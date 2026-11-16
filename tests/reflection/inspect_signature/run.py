#!/usr/bin/env python3
"""ADR0219真实消费验收：同次oracle、CIL回调、JIT/AOT删输入搬迁，产物仅位于仓库外。"""
from pathlib import Path
import argparse
import json
import os
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent


def run(command, cwd, env, log, timeout=900):
    result = subprocess.run(list(map(str, command)), cwd=cwd, env=env, capture_output=True, timeout=timeout)
    log.with_suffix(".stdout").write_bytes(result.stdout)
    log.with_suffix(".stderr").write_bytes(result.stderr)
    assert result.returncode == 0, (command, result.returncode, result.stdout[-3000:], result.stderr[-6000:])
    return result


def check_case(args, case, work, logs):
    inputs = work / (case + "-input")
    inputs.mkdir()
    consumer = case in ("attrs", "pydantic", "fastapi")
    bridge = case in ("bridge", "atexit", "owner")
    if consumer:
        entry = inputs / (case + "_input.py")
        shutil.copyfile(HERE / "consumers" / entry.name, entry)
        shutil.copyfile(HERE / "consumers/compiled_callbacks.py", inputs / "compiled_callbacks.py")
        callback = "compiled_callbacks"
    elif bridge:
        entry = inputs / ({"atexit": "bridge_atexit_input.py", "owner": "bridge_owner_input.py"}.get(case, "bridge_input.py"))
        shutil.copyfile(HERE / entry.name, entry)
        shutil.copyfile(HERE / "bridge_callbacks.py", inputs / "bridge_callbacks.py")
        shutil.copyfile(HERE / "bridge_kind_callbacks.py", inputs / "bridge_kind_callbacks.py")
        callback = "bridge_callbacks"
    else:
        entry = inputs / (case + ".py")
        shutil.copyfile(ROOT / "tests/e2e/phase3" / entry.name, entry)
        callback = None
    python = args.python if consumer or bridge else args.core_python
    env = {k: v for k, v in os.environ.items() if k not in ("PYTHONPATH", "PYTHONHOME", "RYUPY_RUNTIME_DIR")}
    env.update(PYTHONDONTWRITEBYTECODE="1", PYTHON_COLORS="0", PYTHON=str(python), RUSTC_WRAPPER="")
    if consumer or bridge:
        env.update(VIRTUAL_ENV=str(python.parent.parent), PATH=str(python.parent) + os.pathsep + env.get("PATH", ""))
    expected = run([python, "-B", entry], inputs, env, logs / (case + "-oracle"))
    command = [args.ryupy]
    if callback:
        command += ["--cpython-provider", "auto", "--instance-module", callback]
    command += ["build", entry]
    if callback:
        command += ["--cpython-entry", "--include-module", callback, "--cpython-dynamic-module", "inspect"]
    else:
        command += ["--native"]
    if bridge:
        command += ["--include-module", "bridge_kind_callbacks"]
    if case == "fastapi":
        command += ["--cpython-dynamic-module", "httpx2", "--cpython-dynamic-module", "anyio._backends._asyncio"]
    jit = work / (case + "-jit-build")
    run(command + ["--out-dir", jit], inputs, env, logs / (case + "-jit-build"))
    if callback:
        plans = list(jit.glob("*.source-plan.json"))
        assert len(plans) == 1, plans
        report = json.loads(plans[0].read_text())
        assert report["modules"][callback]["provider"] == "user_source", report
        config = json.loads((jit / "ryupy-cpython.json").read_text())
        assert config["native_inspect_profile"] is True
    aot = work / (case + "-aot-build")
    if not args.jit_only:
        run(command + ["--aot", "--out-dir", aot], inputs, env, logs / (case + "-aot-build"))
    relocated = work / ("搬迁 " + case)
    relocated.mkdir()
    jit_dist = relocated / "jit"
    shutil.move(jit, jit_dist)
    aot_dist = relocated / "native"
    if not args.jit_only:
        shutil.move(aot / (entry.stem + ".dist"), aot_dist)
        shutil.rmtree(aot)
    shutil.rmtree(inputs)
    assert not inputs.exists() and not jit.exists() and not aot.exists()
    clean = {k: v for k, v in env.items() if k not in ("VIRTUAL_ENV", "PYTHON", "PYTHONPATH", "PYTHONHOME", "DYLD_LIBRARY_PATH", "LD_LIBRARY_PATH")}
    clean.update(PATH=str(work / "empty-path"), RYUPY_RUNTIME_DIR=str(work / "missing-runtime"))
    (work / "empty-path").mkdir(exist_ok=True)
    if callback:
        clean["RYUPY_CPYTHON_REPORT"] = str((logs / (case + "-jit-host.json")).absolute())
    actual = run([shutil.which("dotnet"), jit_dist / (entry.stem + ".dll"), *( ["--require-cil"] if callback else [])], relocated, clean, logs / (case + "-jit"))
    assert (actual.stdout, actual.stderr) == (expected.stdout, expected.stderr), (case, actual.stdout, actual.stderr, expected.stdout, expected.stderr)
    if not args.jit_only:
        executable = aot_dist / entry.stem
        if os.name == "nt":
            executable = executable.with_suffix(".exe")
        if callback:
            clean["RYUPY_CPYTHON_REPORT"] = str((logs / (case + "-aot-host.json")).absolute())
        actual = run([executable, *( ["--require-cil"] if callback else [])], relocated, clean, logs / (case + "-aot"))
        assert (actual.stdout, actual.stderr) == (expected.stdout, expected.stderr), (case, actual.stdout, actual.stderr, expected.stdout, expected.stderr)
    if callback:
        for mode in (["jit"] if args.jit_only else ["jit", "aot"]):
            stats = json.loads((logs / (case + "-" + mode + "-host.json")).read_text())
            assert stats["state"] == "closed", stats
            for key in ("active_leases", "handles", "managed_roots", "pending_releases"):
                assert stats[key] == 0, (key, stats)
            assert stats["synchronization_failure"] is None, stats
            lifecycle = stats["native_inspect_profile"]
            assert lifecycle["enabled"] is True and lifecycle["active"] == 0 and lifecycle["aliases_null"] == 1, lifecycle
            assert lifecycle["owners_created"] == lifecycle["owners_cleared"] == lifecycle["owners_freed"], lifecycle
            assert lifecycle["owners_created"] == (2 if case == "owner" else 1), lifecycle
            distribution = jit_dist if mode == "jit" else aot_dist
            for key in ("python_library", "shim_library"):
                assert Path(stats[key]).resolve().is_relative_to(distribution.resolve()), stats[key]
            for name, filename in stats["module_paths"].items():
                if filename and name != "__main__":
                    assert Path(filename).resolve().is_relative_to(distribution.resolve()), (name, filename)
    print(case + ": 同次CP预期、删input/build后JIT" + ("/AOT" if not args.jit_only else "") + "搬迁通过", flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ryupy", type=Path, default=ROOT / "target/debug/ryupy")
    parser.add_argument("--python", type=Path, required=True, help="含固定attrs23.2.0/Pydantic2.14.3/FastAPI0.158.1及httpx2 2.16.2的环境")
    parser.add_argument("--core-python", type=Path, required=True, help="固定CPython3.12.13")
    parser.add_argument("--case", action="append", choices=["bridge", "atexit", "owner", "attrs", "pydantic", "fastapi", "core"])
    parser.add_argument("--jit-only", action="store_true")
    parser.add_argument("--logs", type=Path, required=True)
    args = parser.parse_args()
    args.ryupy = args.ryupy.absolute()
    args.python = args.python.absolute()
    args.core_python = args.core_python.absolute()
    args.logs.mkdir(parents=True, exist_ok=True)
    assert not args.logs.absolute().is_relative_to(ROOT), "验收输出必须位于仓库外"
    cases = args.case or ["core", "bridge", "attrs", "pydantic", "fastapi"]
    if "core" in cases:
        cases.remove("core")
        cases[:0] = sorted(p.stem for p in (ROOT / "tests/e2e/phase3").glob("c2_inspect_signature_*.py"))
    with tempfile.TemporaryDirectory(prefix="ryupy-inspect-signature-") as directory:
        for case in cases:
            check_case(args, case, Path(directory), args.logs)


if __name__ == "__main__":
    main()
