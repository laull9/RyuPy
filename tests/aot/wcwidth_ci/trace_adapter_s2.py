#!/usr/bin/env python3
"""S2a真实隔离JIT门；保留命令/产物/原流，不用probe替代任何旧项目或Native门。"""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "target/trace-adapter-s2"
FIXTURES = ROOT / "tests/fixtures/trace_adapter_s2"
CASES = ("explicit_inner", "explicit_module", "recursive", "reused", "native_local", "native_outer")


def write(name, data):
    (OUT / name).write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n")


def run(label, argv, env, expected=0):
    write(label + ".command.json", {"argv": [str(x) for x in argv], "cwd": str(ROOT),
          "CARGO_TARGET_DIR": env.get("CARGO_TARGET_DIR"), "RYUPY_RUNTIME_DIR": env.get("RYUPY_RUNTIME_DIR")})
    with (OUT / (label + ".stdout")).open("wb") as stdout, (OUT / (label + ".stderr")).open("wb") as stderr:
        result = subprocess.run(argv, cwd=ROOT, env=env, stdout=stdout, stderr=stderr, check=False)
    write(label + ".exit.json", {"returncode": result.returncode})
    print(f"{label}: exit={result.returncode}", flush=True)
    if expected is None:
        if result.returncode == 0:
            raise RuntimeError(f"{label}必须明确拒绝")
    elif result.returncode != expected:
        raise RuntimeError(f"{label}失败，见原流；不重试、不改oracle")


def unique(folder, pattern):
    matches = list(folder.glob(pattern))
    if len(matches) != 1:
        raise RuntimeError(f"产物不唯一: {folder}/{pattern}: {matches}")
    return matches[0]


def digest(path):
    data = path.read_bytes()
    return {"path": str(path), "bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()}


def main():
    OUT.mkdir(parents=True, exist_ok=False)
    env = os.environ.copy()
    dotnet = env.get("DOTNET", "dotnet")
    write("source-inputs.json", [digest(p) for p in sorted(FIXTURES.iterdir()) if p.is_file()])
    run("git-head", ["git", "rev-parse", "HEAD", "HEAD^{tree}"], env)
    run("git-status", ["git", "status", "--porcelain"], env)
    if (OUT / "git-status.stdout").read_text().strip():
        raise RuntimeError("CI源码不是clean冻结提交")
    # CP已按单次预算在冻结前本地核验。本job不再次运行CP、不自动更新expected。
    reference = json.loads((FIXTURES / "cp-reference.json").read_text())
    if reference["python_version"] != "3.12.13" or set(reference["runs"]) != set(CASES):
        raise RuntimeError("CP冻结版本/案例集合不符")
    for name, expected_hash in reference["inputs"].items():
        if hashlib.sha256((FIXTURES / name).read_bytes()).hexdigest() != expected_hash:
            raise RuntimeError("CP冻结输入发生改变: " + name)
    for name, result in reference["runs"].items():
        if result["exit"] != 0 or result["stderr"] or json.loads(result["stdout"]) != json.loads((FIXTURES / (name + ".expected.json")).read_text()):
            raise RuntimeError("冻结CP原流与期望不符: " + name)
    default = OUT / "default-runtime"
    probe = OUT / "probe-runtime"
    tests = ROOT / "runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj"
    runtime = ROOT / "runtime/RyuPy.Runtime/RyuPy.Runtime.csproj"
    run("default-runtime", [dotnet, "build", tests, "-c", "Release", "--artifacts-path", default, "-nologo"], env)
    run("default-normalization", [dotnet, "test", tests, "-c", "Release", "--artifacts-path", default,
        "--no-build", "--no-restore", "--filter", "FullyQualifiedName~PyRaiseNormalizationTests",
        "--logger", "trx;LogFileName=normalization.trx", "--results-directory", OUT / "trx"], env)
    tree = ET.parse(OUT / "trx/normalization.trx")
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    rows = tree.findall(".//t:UnitTestResult", ns)
    expected = {"PrepareRaisePreservesIdentityExistingFramesAndContext", "PrepareRaiseRejectsNonExceptionWithoutChangingCurrentContext"}
    names = {row.attrib["testName"].split(".")[-1] for row in rows}
    if len(rows) != 2 or names != expected or any(row.attrib["outcome"] != "Passed" for row in rows):
        raise RuntimeError("默认归一化focused测试未精确2/2零skip")
    run("probe-runtime", [dotnet, "build", runtime, "-c", "Release", "--artifacts-path", probe,
        "-p:TraceAdapterTest=true", "-nologo"], env)
    probe_tests = OUT / "probe-tests"
    run("native-adapter-tests", [dotnet, "test", tests, "-c", "Release", "--artifacts-path", probe_tests,
        "-p:TraceAdapterTest=true", "--filter", "FullyQualifiedName~PyTraceNativeAdapterTests",
        "--logger", "trx;LogFileName=native-adapter.trx", "--results-directory", OUT / "trx-native"], env)
    native_rows = ET.parse(OUT / "trx-native/native-adapter.trx").findall(".//t:UnitTestResult", ns)
    native_expected = {"NativeSourcePreservesIdentityOrderAndContext",
        "NativeReceiveRejectsUnknownSourceAndDuplicateBoundary", "NativeBirthRejectsWrongConsumedAndExplicitRevokedGrants",
        "NativeTicketsRejectStaleActivationScopeAndColdEpoch", "NativeOldFlightFinishesAndUnfinishedCloseReleasesScope"}
    if len(native_rows) != 5 or {row.attrib["testName"].split(".")[-1] for row in native_rows} != native_expected or any(row.attrib["outcome"] != "Passed" for row in native_rows):
        raise RuntimeError("native adapter反例未精确5/5零skip")
    default_dll = unique(default / "bin/RyuPy.Runtime", "**/RyuPy.Runtime.dll")
    probe_dll = unique(probe / "bin/RyuPy.Runtime", "**/RyuPy.Runtime.dll")
    write("runtime-identities.json", [digest(default_dll), digest(probe_dll)])
    normal_env = dict(env, CARGO_TARGET_DIR=str(OUT / "cargo-default"), RYUPY_RUNTIME_DIR=str(default_dll.parent))
    probe_env = dict(env, CARGO_TARGET_DIR=str(OUT / "cargo"), RYUPY_RUNTIME_DIR=str(probe_dll.parent))
    run("default-compiler", ["cargo", "build", "-p", "ryupy-cli", "--locked"], normal_env)
    run("probe-compiler", ["cargo", "build", "-p", "ryupy-cli", "--features", "trace-adapter-test", "--locked"], probe_env)
    write("compiler-identities.json", [digest(OUT / "cargo-default/debug/ryupy"), digest(OUT / "cargo/debug/ryupy")])
    for name in ("trace_adapter_abi_rejects_wrong_arguments_results_and_array_calls",
                 "trace_adapter_real_typed_cfg_receives_before_pop_with_exact_caught_local",
                 "trace_adapter_admission_rejects_unimplemented_semantics",
                 "trace_adapter_native_typed_cfg_keeps_real_call_and_handle_before_pop"):
        run(name, ["cargo", "test", "-p", "ryupy-mir", "--features", "trace-adapter-test", "--locked",
            "--lib", "trace_adapter_test_tests::" + name, "--", "--exact", "--nocapture"], probe_env)
        if "1 passed; 0 failed" not in (OUT / (name + ".stdout")).read_text():
            raise RuntimeError("精确Rust测试未执行: " + name)
    run("driver-stages", ["cargo", "test", "-p", "ryupy-driver", "--features", "trace-adapter-test", "--locked",
        "--test", "trace_adapter_s2", "--", "--nocapture"], probe_env)
    if "1 passed; 0 failed" not in (OUT / "driver-stages.stdout").read_text():
        raise RuntimeError("真实driver阶段测试未执行")
    default_cli = OUT / "cargo-default/debug/ryupy"
    probe_cli = OUT / "cargo/debug/ryupy"
    run("default-fixture", [default_cli, "build", FIXTURES / "explicit_inner.py", "--out-dir", OUT / "default-generated"], normal_env)
    default_generated = OUT / "default-generated/explicit_inner.dll"
    for name in CASES:
        generated = OUT / "generated" / name
        run(name + "-compile", [probe_cli, "build", FIXTURES / (name + ".py"), "--out-dir", generated], probe_env)
        dll = generated / (name + ".dll")
        for stage in ("hir", "types", "mir", "cil"):
            run(name + "-" + stage, [probe_cli, "dump", FIXTURES / (name + ".py"), "--emit", stage], probe_env)
        host_artifacts = OUT / "hosts" / name
        run(name + "-host-build", [dotnet, "build", ROOT / "tests/trace_adapter_s2_host/TraceAdapterHost.csproj",
            "-c", "Release", "--artifacts-path", host_artifacts,
            "-p:ProbeRuntime=" + str(probe_dll), "-p:ProbeGenerated=" + str(dll), "-nologo"], probe_env)
        host = unique(host_artifacts / "bin/TraceAdapterHost", "**/TraceAdapterHost.dll")
        run(name + "-jit", [dotnet, host, FIXTURES / (name + ".expected.json"), OUT / "observed" / name,
            probe_dll, dll, default_dll, default_generated], probe_env)
        actual = json.loads((OUT / "observed" / name / "actual.json").read_text())
        if actual != json.loads((FIXTURES / (name + ".expected.json")).read_text()):
            raise RuntimeError("真实host结果与冻结CP不符: " + name)
    for name in ("bare_reject", "cause_reject", "generator_reject", "nested_handler_reject"):
        run(name, [probe_cli, "build", FIXTURES / (name + ".py"), "--out-dir", OUT / "rejected" / name], probe_env, expected=None)
        if "S2a probe" not in (OUT / (name + ".stderr")).read_text():
            raise RuntimeError("拒绝不是预期S2a准入诊断")
    write("artifacts.json", [digest(p) for p in (default_cli, probe_cli, default_dll, probe_dll, default_generated)])
    write("result.json", {"status": "passed", "cases": list(CASES), "scope": "S2a test-only JIT/CFG/ABI; production closed; original generator failure not repaired; Native not run"})


if __name__ == "__main__":
    try:
        main()
    except Exception as error:
        if OUT.exists():
            write("failure.json", {"type": type(error).__name__, "message": str(error)})
        raise
