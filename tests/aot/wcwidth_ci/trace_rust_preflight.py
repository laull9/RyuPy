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
    "dataflow": [
        "actual_mir_late_spill_write_is_rejected",
        "actual_mir_mixed_return_is_rejected",
        "actual_mir_diamond_reaches_fixed_point",
        "actual_mir_nested_frame_escape_is_rejected",
        "actual_ir_seed_first_arrival_and_late_must_join",
        "actual_ir_seed_raise_prepare_and_phase_split",
        "actual_ir_seed_growth_and_inner_work_are_bounded",
    ],
    "mechanical": [
        "bounded_cells_preserve_snapshots_and_failure_atomicity",
        "actual_layout_indices_match_first_match_linear_oracle",
    ],
    "observer": [
        "bounded_encoding_checks_actual_bytes_and_errors",
        "failed_export_is_separate_from_analysis_result",
        "actual_inspection_is_unchanged_when_observation_stops",
        "record_and_event_limits_keep_decodable_prefixes",
    ],
    "product": [
        "original_a_b_and_same_site_property_compile_through_final_link",
        "original_constructor_field_inputs_are_whole_product_rejections",
        "high_and_low_validation_are_distinct_and_dead_yield_keeps_real_generator_identity",
        "unknown_capability_system_exit_bare_and_protocol_injection_are_rejected",
        "explicit_or_possible_none_exception_argument_rejects_through_aliases",
        "sealed_link_rejects_changed_actual_source_global_owner_and_abi",
        "absent_certificate_cannot_enable_v1_and_prelink_proof_cannot_cover_new_code",
    ],
    "product-structure": [
        "actual_low_template_rejects_slot_pc_retry_handler_line_and_extra_source_mutations",
        "actual_factory_owner_and_fresh_box_are_checked_not_just_slot_count",
        "template_box_cannot_overwrite_original_dynamic_parameter",
        "high_contract_rejects_extra_push_and_pure_control_overwrite",
    ],
    "product-edge-witness": [
        "cleared_empty_bridge_is_accepted_using_capture_time_canonical_edge",
        "real_redirect_still_rejects_and_is_not_described_as_erased_bridge",
        "witness_bounds_chains_utf8_and_redacts_literal_contents",
        "dead_values_keep_original_nonempty_anchor_but_bypassing_it_rejects",
        "contract_rejects_bad_ids_before_stack_walk_and_arena_deletion",
        "bypassing_an_original_line_or_trace_effect_is_rejected",
        "simplify_cfg_clears_both_ordered_empty_branch_bridges",
    ],
    "product-edges": [
        "ordered_branch_targets_condition_and_direct_constant_fold_are_checked",
        "suspend_raw_fields_reject_new_empty_resume_prefix_and_other_changes",
        "full_region_membership_and_handler_boundaries_are_not_erased",
        "original_and_final_empty_cycles_are_rejected_even_after_an_anchor",
        "invalid_targets_regions_and_exclusive_end_are_errors_not_panics",
        "folding_then_bypassing_an_original_empty_branch_remains_rejected",
    ],
    "product-defaults": [
        "actual_null_slots_and_local_aliases_preserve_python_arity_not_owner",
        "receiver_requires_real_same_block_instance_not_labels_null_or_overwrite",
        "construction_checks_actual_ids_metadata_null_slots_and_hidden_owner",
        "cold_chain_rejects_unknown_steps_and_wrong_module_receiver",
        "private_slot_role_export_wrong_id_and_duplicate_publication_reject",
        "opaque_defaults_reject_other_consumers_and_control_escape",
        "zero_multi_renamed_and_aliased_functions_link_but_defaults_stay_outside",
    ],
    "product-initialization": [
        "actual_prefix_preserves_resets_opaque_path_and_optional_metadata",
        "missing_wrong_duplicate_and_late_role_publications_are_rejected",
        "helper_keys_modules_source_and_opaque_values_cannot_be_forged_or_escape",
        "role_guards_declarations_alias_exports_and_bad_ids_are_errors",
        "explicit_reads_definition_freezes_and_implicit_class_reads_require_publication",
        "entry_bypass_eh_cycles_and_internal_initializer_calls_are_rejected",
        "lowering_seal_preserves_all_non_generators_and_global_export_premises",
        "linked_registration_capability_and_real_maps_are_checked_before_certification",
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


MIR_MODULES = {
    "domain": "trace_domain_tests",
    "lowering": "trace_lowering_tests",
    "frame": "trace_frame_tests",
    "observer": "trace_frame::observer::tests",
    "dataflow": "trace_frame_dataflow::tests",
    "mechanical": "trace_frame_dataflow::mechanical_tests",
    "product": "generator_product_tests",
    "product-structure": "generator_product_structure_tests",
    "product-edge-witness": "generator_product_edge_witness::tests",
    "product-edges": "generator_product_edges::tests",
    "product-defaults": "generator_product_defaults_tests",
    "product-initialization": "generator_product_initialization_tests",
}
PRODUCT_GROUPS = ("product", "product-structure", "product-edge-witness", "product-edges", "product-defaults", "product-initialization")
REQUIRED = {
    "cargo-version", "rustc-version", "rustfmt-version", "git-head", "git-tree", "git-status",
    "git-head-after", "git-tree-after", "git-status-after", "fmt", "check", "mir-list",
    "check-guard-default", "check-guard-pilot", *EXPECTED,
}
WORK_BUDGET_SECONDS = 1800
EXPERIMENTAL = {
    "frame": {
        "original_frames_has_pc_partitioned_retry_frame_obligations",
        "renamed_generator_and_factory_alias_keep_frame_proof",
        "repeated_actual_match_block_converges_without_epoch_growth",
    },
    "dataflow": {"actual_mir_diamond_reaches_fixed_point"},
}


def isolated_names(group):
    return {MIR_MODULES[group] + "::" + name for name in EXPERIMENTAL.get(group, ())}


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


def product_filter(group, available):
    """先核真实list中substring会选中的完整集合，避免多跑/漏跑固定产品测试。"""
    pattern = MIR_MODULES[group] + "::"
    selected = {name for name in available if pattern in name} if available is not None else None
    return pattern if selected == expected_names(group) else None


def tests_complete(text, group, available):
    """单个test进程的完整身份、running/结果/filtered均精确匹配，零测试拒绝。"""
    expected = expected_names(group)
    isolated = isolated_names(group)
    observed = re.findall(r"^test (\S+) \.\.\. ok$", text, re.MULTILINE)
    ignored = re.findall(r"^test (\S+) \.\.\. ignored(?:, .*)?$", text, re.MULTILINE)
    running = re.findall(r"^running (\d+) tests?$", text, re.MULTILINE)
    summaries = re.findall(
        r"^test result: ok\. (\d+) passed; (\d+) failed; (\d+) ignored; (\d+) measured; (\d+) filtered out;",
        text, re.MULTILINE,
    )
    return (
        available is not None and expected <= available
        and len(observed) == len(expected - isolated) and set(observed) == expected - isolated
        and len(ignored) == len(isolated) and set(ignored) == isolated
        and running == [str(len(expected))] and len(summaries) == 1
        and tuple(map(int, summaries[0])) == (len(expected - isolated), 0, len(isolated), 0, len(available) - len(expected))
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
    isolated = sorted(set().union(*(isolated_names(group) for group in EXPERIMENTAL)))
    return {"success": not failures, "failures": failures, "exit_codes": codes,
            "isolated_experiments": {"status": "not-executed-known-failed", "count": len(isolated),
                                     "names": isolated, "command": "make trace-frame-proof-experiment"}}


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
    available = listed_tests((OUTPUT / "mir-list.stdout").read_text()) if codes.get("mir-list") == 0 else None
    for group, module in MIR_MODULES.items():
        if codes.get("mir-list") == 0:
            pattern = product_filter(group, available) if group in PRODUCT_GROUPS else module
            if pattern is None:
                skip(group, "actual MIR list does not match the fixed product test group")
                continue
            argv = ["cargo", "test", "-p", "ryupy-mir", "--lib", "--locked", pattern,
                    "--", "--color", "never", "--test-threads=1", "--show-output"]
            run(group, argv, 600)
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
    # 一次采样已结束；意外请求不能伪装为not-requested并被吞掉。
    unexpected = "RYUPY_FRAME_OBSERVATION" in os.environ
    observation = {"status": "unexpected-request" if unexpected else "not-requested",
                   "reason": "one-shot-observation-closed"}
    (OUTPUT / "frame-observation-result.json").write_text(json.dumps(observation, indent=2) + "\n")
    report = aggregate(codes, outputs)
    if unexpected:
        report["failures"].append("frame-observation: unexpected request after one-shot closure")
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
