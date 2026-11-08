"""S3候选的hosted Rust前置：保留首次原流，格式失败不阻止获取编译反馈。"""
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import signal
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
OUTPUT = ROOT / "target/trace-rust-preflight"
EXPECTED = {
    "domain": [
        "original_frames_has_closed_value_and_error_flow",
        "source_names_and_module_aliases_do_not_grant_capabilities",
        "ordinary_parameter_and_return_wrappers_preserve_generator_identity",
        "unsupported_definitions_are_not_skipped_as_unused",
        "unknown_shadow_and_non_generator_inputs_are_rejected",
        "budget_overflow_is_absorbing_and_bottom_is_distinct",
    ],
    "lowering": ["audit_build_and_link_preserve_real_generator_and_parameter_origins"],
}


def capture(directory, label, argv, cwd, timeout=900):
    """每次只执行一次；真实stdout/stderr分开保存，异常同样留下明确exit。"""
    prefix = directory / label
    prefix.with_suffix(".argv.json").write_text(
        json.dumps({"argv": argv, "cwd": str(cwd), "timeout_seconds": timeout}, indent=2) + "\n"
    )
    with prefix.with_suffix(".stdout").open("wb") as stdout, prefix.with_suffix(".stderr").open("wb") as stderr:
        try:
            process = subprocess.Popen(argv, cwd=cwd, stdout=stdout, stderr=stderr, start_new_session=True)
            try:
                code = process.wait(timeout=timeout)
            except subprocess.TimeoutExpired:
                try:
                    os.killpg(process.pid, signal.SIGKILL)
                except ProcessLookupError:
                    pass
                process.wait()
                stderr.write(b"\npreflight: process group timed out and was killed\n")
                code = 124
        except OSError as error:
            stderr.write((f"\npreflight: {error}\n").encode())
            code = 127
    prefix.with_suffix(".exit").write_text(f"{code}\n")
    return code


def tests_complete(text, group):
    """不仅要求退出0，还核验每个完整测试身份、数量以及无ignored/filtered伪通过。"""
    module = "trace_domain_tests" if group == "domain" else "trace_lowering_tests"
    expected = {f"{module}::{name}" for name in EXPECTED[group]}
    observed = re.findall(r"^test (\S+) \.\.\. ok$", text, re.MULTILINE)
    summaries = re.findall(
        r"^test result: ok\. (\d+) passed; (\d+) failed; (\d+) ignored; (\d+) measured; \d+ filtered out;",
        text, re.MULTILINE,
    )
    return (
        len(observed) == len(expected) and set(observed) == expected
        and any(tuple(map(int, counts)) == (len(expected), 0, 0, 0) for counts in summaries)
    )


def aggregate(codes, outputs):
    required = {"cargo-version", "rustc-version", "rustfmt-version", "git-head", "git-tree", "git-status", "git-head-after", "git-tree-after", "git-status-after", "fmt", "check", "domain", "lowering"}
    failures = sorted(name for name in required if codes.get(name) != 0)
    for group in EXPECTED:
        if codes.get(group) == 0 and not tests_complete(outputs.get(group, ""), group):
            failures.append(f"{group}: incomplete test identities/counts")
    # checkout必须是实际提交；脏工作树不允许冒充该git identity的受测源码。
    if codes.get("git-status") == 0 and outputs.get("git-status", "").strip():
        failures.append("git-status: dirty checkout")
    if outputs.get("git-status-after", "").strip():
        failures.append("git-status-after: dirty checkout")
    for identity in ("git-head", "git-tree"):
        if outputs.get(identity) != outputs.get(identity + "-after"):
            failures.append(identity + ": changed during checks")
    return {"success": not failures, "failures": failures, "exit_codes": codes}


def verify_tools(directory):
    """拒绝回落到hosted系统Rust，实际工具必须来自已核SHA的隔离安装。"""
    root = Path(os.environ["TRACE_RUST_ROOT"]).resolve()
    tools = {}
    for name in ("cargo", "rustc", "rustfmt", "cargo-fmt"):
        path = Path(shutil.which(name) or "/missing-tool").resolve()
        if path != root / "bin" / name:
            raise RuntimeError(f"not the pinned Rust tool: {name}: {path}")
        tools[name] = {"path": str(path), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}
    (directory / "tools.json").write_text(json.dumps(tools, indent=2) + "\n")


def main():
    OUTPUT.mkdir(parents=True, exist_ok=False)
    (OUTPUT / "hosted.json").write_text(json.dumps({
        key: os.environ.get(key) for key in ("GITHUB_SHA", "GITHUB_REF", "GITHUB_RUN_ID", "GITHUB_RUN_ATTEMPT", "RUNNER_OS")
    }, indent=2) + "\n")
    verify_tools(OUTPUT)
    codes = {}
    stages = [
        ("cargo-version", ["cargo", "--version"]),
        ("rustc-version", ["rustc", "-Vv"]),
        ("rustfmt-version", ["rustfmt", "--version"]),
        ("git-head", ["git", "rev-parse", "HEAD"]),
        ("git-tree", ["git", "rev-parse", "HEAD^{tree}"]),
        ("git-status", ["git", "status", "--porcelain", "--untracked-files=all"]),
        ("fmt", ["cargo", "fmt", "--all", "--", "--check"]),
        ("check", ["cargo", "check", "-p", "ryupy-mir", "--tests", "--locked"]),
    ]
    for label, argv in stages:
        timeout = 900 if label == "check" else 180 if label == "fmt" else 60
        codes[label] = capture(OUTPUT, label, argv, ROOT, timeout=timeout)
    # 格式检查失败仍执行check；编译成功则两组测试都执行，第一组失败不吞第二组反馈。
    for group, module in (("domain", "trace_domain_tests"), ("lowering", "trace_lowering_tests")):
        if codes["check"] == 0:
            argv = ["cargo", "test", "-p", "ryupy-mir", "--locked", module, "--", "--color", "never", "--test-threads=1"]
            codes[group] = capture(OUTPUT, group, argv, ROOT, timeout=600)
        else:
            codes[group] = None
            (OUTPUT / f"{group}.skipped.json").write_text(json.dumps({"reason": "cargo check failed", "check_exit": codes["check"]}) + "\n")
    for label, argv in (
        ("git-head-after", ["git", "rev-parse", "HEAD"]),
        ("git-tree-after", ["git", "rev-parse", "HEAD^{tree}"]),
        ("git-status-after", ["git", "status", "--porcelain", "--untracked-files=all"]),
    ):
        codes[label] = capture(OUTPUT, label, argv, ROOT, timeout=60)
    outputs = {name: (OUTPUT / f"{name}.stdout").read_text(errors="replace") for name in codes if codes[name] is not None}
    report = aggregate(codes, outputs)
    expected_head = os.environ.get("GITHUB_SHA")
    if not expected_head or outputs.get("git-head", "").strip() != expected_head:
        report["failures"].append("git-head: missing or mismatched GITHUB_SHA")
        report["success"] = False
    (OUTPUT / "result.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))
    return 0 if report["success"] else 1


if __name__ == "__main__":
    sys.exit(main())
