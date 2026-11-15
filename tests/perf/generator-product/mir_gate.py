"""隔离运行格式与MIR测试；显式核对结构原型25例及既有四个忽略项，不宣称产品融合。"""
import json
import os
from pathlib import Path
import re
import subprocess
import sys

EXPECTED = {
    'generator_sum_plan::' + name for name in (
        'scalar_yields_keep_explicit_edges_and_residents',
        'unknown_calls_and_dynamic_operators_are_not_pure',
        'original_workload_is_not_claimed_without_range_and_dynamic_proofs',
        'original_three_sources_record_distinct_blocking_instructions',
    )
} | {
    'generator_sum_range_plan::tests::' + name for name in (
        'original_genexpr_has_a_single_cursor_candidate_not_a_product_proof',
        'dead_none_tail_does_not_define_reachable_values',
        'connecting_dead_tail_through_resume_restores_duplicate_rejection',
        'reachable_invalid_edge_or_unresolved_terminator_is_rejected',
        'callbacks_closure_and_multiple_cursors_remain_rejected',
        'single_static_definition_does_not_authorize_read_before_definition',
        'only_matching_trace_depth_may_be_popped',
        'numeric_ids_must_match_one_metadata_record',
        'alternate_path_definition_is_not_a_dominating_fact',
        'shared_resume_does_not_get_a_second_yield_edge',
        'loop_first_read_cannot_use_a_definition_after_suspend',
        'required_resident_set_includes_original_cursor',
        'second_frame_entry_cannot_masquerade_as_matching_trace_depth',
        'separate_builds_can_collide_so_numeric_ids_are_not_a_product_certificate',
    )
} | {
    'generator_sum_prepared::' + name for name in (
        'owned_prepare_checks_actual_lowering_and_keeps_other_generators',
        'mapping_does_not_depend_on_field_names_or_layout_order',
        'real_field_reads_writes_and_pc_mutations_are_rejected',
        'factory_input_and_function_origin_are_checked_from_actual_operands',
        'inserted_dispatch_goto_must_not_hide_a_side_effect',
        'merging_two_field_read_targets_and_rewriting_the_operation_is_rejected',
        'extra_uncovered_dispatch_block_is_not_silently_ignored',
    )
}
IGNORED = {
    'trace_frame_tests::original_frames_has_pc_partitioned_retry_frame_obligations',
    'trace_frame_tests::renamed_generator_and_factory_alias_keep_frame_proof',
    'trace_frame_tests::repeated_actual_match_block_converges_without_epoch_growth',
    'trace_frame_dataflow::tests::actual_mir_diamond_reaches_fixed_point',
}


def validate(text, focused, feature=False):
    expected = EXPECTED | (FEATURE if feature else set())
    rows = re.findall(r'^test ([\w:]+) \.\.\. (ok|FAILED|ignored)\b', text, re.M)
    assert rows and len({n for n, _ in rows}) == len(rows), '测试为空或名称重复'
    outcomes = dict(rows)
    assert {n for n in outcomes if n.startswith('generator_sum_')} == expected, '缺少或多出结构原型测试'
    assert all(outcomes[n] == 'ok' for n in expected), '原型测试失败或被忽略'
    allowed_ignored = set() if focused else IGNORED
    assert {n for n, result in rows if result == 'ignored'} == allowed_ignored, '忽略集合变化'
    assert not any(result == 'FAILED' for _, result in rows), '测试失败'
    summaries = re.findall(r'^test result: ok\. (\d+) passed; (\d+) failed; (\d+) ignored; (\d+) measured; (\d+) filtered out;', text, re.M)
    assert len(summaries) == 1, '缺少或重复汇总'
    passed, failed, ignored, measured, filtered = map(int, summaries[0])
    assert passed == len(rows) - len(allowed_ignored) and failed == measured == 0
    assert ignored == len(allowed_ignored)
    if focused:
        assert len(rows) == len(expected)
    else:
        assert filtered == 0 and passed > len(expected), '并非完整MIR库测试'
    return {'passed': passed, 'ignored': sorted(allowed_ignored), 'filtered': filtered}

FEATURE = {'generator_sum_emit::tests::' + name for name in (
    'compiled_batch_has_guard_and_same_frame_fallback',
    'unknown_callback_closure_and_filter_do_not_get_batch_entry',
    'ordinary_resume_is_not_replaced_or_given_accumulator')}

if __name__ == '__main__':
    print(validate(Path(sys.argv[1]).read_text(), sys.argv[2] == 'focused', sys.argv[3] == 'on'))
