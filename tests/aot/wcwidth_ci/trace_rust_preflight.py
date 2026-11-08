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
import time

import trace_frame_observation

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
    "frame": [
        "original_frames_has_pc_partitioned_retry_frame_obligations",
        "renamed_generator_and_factory_alias_keep_frame_proof",
        "retry_with_unknown_pc_is_rejected",
        "retry_without_caught_write_is_rejected",
        "wrong_restore_slot_and_frame_alias_are_rejected",
        "missing_spill_and_wrong_dispatch_map_are_rejected",
        "factory_result_overwrite_after_make_generator_is_rejected",
        "stale_copy_and_post_spill_local_write_are_rejected",
        "clearing_pending_caught_without_read_is_rejected",
        "caught_read_into_wrong_handler_local_is_rejected",
        "repeated_actual_match_block_converges_without_epoch_growth",
        "old_copy_never_revives_after_repeated_current_writes",
    ],
    "observer": [
        "bounded_encoding_checks_actual_bytes_and_errors",
        "failed_export_is_separate_from_analysis_result",
        "actual_inspection_is_unchanged_when_observation_stops",
        "record_and_event_limits_keep_decodable_prefixes",
    ],
    "guard-default": ["bounded_dynamic_except_regions_and_identity_flow"],
    "guard-pilot": ["bounded_dynamic_except_regions_and_identity_flow"],
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


MIR_MODULES = {"domain": "trace_domain_tests", "lowering": "trace_lowering_tests", "frame": "trace_frame_tests", "observer": "trace_frame::observer::tests"}
REQUIRED = {
    "cargo-version", "rustc-version", "rustfmt-version", "git-head", "git-tree", "git-status",
    "git-head-after", "git-tree-after", "git-status-after", "fmt", "check", "mir-list",
    "check-guard-default", "check-guard-pilot", *EXPECTED,
}
WORK_BUDGET_SECONDS = 1800


def expected_names(group):
    prefix = MIR_MODULES[group] + "::" if group in MIR_MODULES else ""
    return {prefix + name for name in EXPECTED[group]}


def listed_tests(text):
    """真实libtest清单决定filtered数量，不从源码grep或旧CI猜总测试数。"""
    names = re.findall(r"^(\S+): test$", text, re.MULTILINE)
    totals = re.findall(r"^(\d+) tests?, (\d+) benchmarks?$", text, re.MULTILINE)
    if len(totals) != 1 or tuple(map(int, totals[0])) != (len(names), 0) or len(set(names)) != len(names):
        return None
    return set(names)


def tests_complete(text, group, available):
    """单个test进程的完整身份、running/结果/filtered均精确匹配，零测试拒绝。"""
    expected = expected_names(group)
    observed = re.findall(r"^test (\S+) \.\.\. ok$", text, re.MULTILINE)
    running = re.findall(r"^running (\d+) tests?$", text, re.MULTILINE)
    summaries = re.findall(
        r"^test result: ok\. (\d+) passed; (\d+) failed; (\d+) ignored; (\d+) measured; (\d+) filtered out;",
        text, re.MULTILINE,
    )
    return (
        available is not None and expected <= available
        and len(observed) == len(expected) and set(observed) == expected
        and running == [str(len(expected))] and len(summaries) == 1
        and tuple(map(int, summaries[0])) == (len(expected), 0, 0, 0, len(available) - len(expected))
    )


def aggregate(codes, outputs):
    failures = sorted(name for name in REQUIRED if codes.get(name) != 0)
    available = listed_tests(outputs.get("mir-list", ""))
    if available is None:
        failures.append("mir-list: incomplete test list")
    for group in EXPECTED:
        universe = available if group in MIR_MODULES else expected_names(group)
        if codes.get(group) == 0 and not tests_complete(outputs.get(group, ""), group, universe):
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
    deadline = time.monotonic() + WORK_BUDGET_SECONDS
    (OUTPUT / "budget.json").write_text(json.dumps({
        "work_seconds": WORK_BUDGET_SECONDS, "final_identity_seconds": 180,
    }) + "\n")

    def skip(label, reason):
        codes[label] = None
        (OUTPUT / f"{label}.skipped.json").write_text(json.dumps({"reason": reason}) + "\n")

    def run(label, argv, timeout):
        remaining = int(deadline - time.monotonic())
        if remaining <= 0:
            skip(label, "bounded preflight work budget exhausted")
        else:
            codes[label] = capture(OUTPUT, label, argv, ROOT, timeout=min(timeout, remaining))

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
        run(label, argv, timeout)
    # 先真实枚举已编译library测试，精确核filtered；失败不能伪造成空清单成功。
    if codes["check"] == 0:
        run("mir-list", ["cargo", "test", "-p", "ryupy-mir", "--lib", "--locked", "--", "--list", "--color", "never"], 600)
    else:
        skip("mir-list", "MIR check failed")
    for group, module in MIR_MODULES.items():
        if codes.get("mir-list") == 0:
            argv = ["cargo", "test", "-p", "ryupy-mir", "--lib", "--locked", module,
                    "--", "--color", "never", "--test-threads=1", "--show-output"]
            if group == "frame":
                os.environ["RYUPY_FRAME_OBSERVATION"] = str((OUTPUT / "frame-observation").resolve())
            try:
                run(group, argv, 600)
            finally:
                if group == "frame":
                    os.environ.pop("RYUPY_FRAME_OBSERVATION", None)
        else:
            skip(group, "MIR test compilation/list failed")
    # 两模式分别check和启动新的test进程；默认失败不吞pilot反馈，反之亦然。
    for group in ("guard-default", "guard-pilot"):
        options = ["-p", "ryupy-driver", "--test", "dynamic_except_structure", "--locked", "--no-default-features"]
        if group == "guard-pilot":
            options += ["--features", "ryupy-driver/ordinary-dynamic-module-pilot"]
        check = "check-" + group
        run(check, ["cargo", "check", *options], 600)
        if codes[check] == 0:
            run(group, ["cargo", "test", *options, EXPECTED[group][0], "--", "--exact",
                        "--color", "never", "--test-threads=1", "--show-output"], 600)
        else:
            skip(group, "matching driver check failed or budget exhausted")
    for label, argv in (
        ("git-head-after", ["git", "rev-parse", "HEAD"]),
        ("git-tree-after", ["git", "rev-parse", "HEAD^{tree}"]),
        ("git-status-after", ["git", "status", "--porcelain", "--untracked-files=all"]),
    ):
        codes[label] = capture(OUTPUT, label, argv, ROOT, timeout=60)
    outputs = {name: (OUTPUT / f"{name}.stdout").read_text(errors="replace") for name in codes if codes[name] is not None}
    observation = trace_frame_observation.collect(ROOT, OUTPUT, outputs.get("git-head", ""), outputs.get("git-tree", ""))
    report = aggregate(codes, outputs)
    if not observation["complete"]:
        report["failures"].append("frame-observation: incomplete or failed; no automatic rerun")
        report["success"] = False
    expected_head = os.environ.get("GITHUB_SHA")
    if not expected_head or outputs.get("git-head", "").strip() != expected_head:
        report["failures"].append("git-head: missing or mismatched GITHUB_SHA")
        report["success"] = False
    (OUTPUT / "result.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))
    return 0 if report["success"] else 1


if __name__ == "__main__":
    sys.exit(main())
