"""实体key切片准入证据：原15合同、新11合同、完整MIR库（含3源码结构）；不代表产品接线。"""
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET
from check_records_probe_evidence import NS, verify

CLASS = 'RyuPy.Runtime.Tests.PyDictMaterializedIncrementTests.'
CASES = {
    'MissingKeyStoresTheCallerTupleWhileHitKeepsTheOlderTuple',
    'ProductSmallIntegerIdentityMatchesOrdinaryArithmetic',
    'OverflowRejectsBeforeWriteAndOrdinaryFallbackPromotesOnce',
    'RejectedValueCallbackObservesSameKeyAndRunsOnlyInFallback',
    'ThrowingFallbackPreservesOldValueAndDoesNotReplayCallback',
    'ShapeAndComparerRejectionsDoNotHashOrAllocateStoredKeys',
    'IncrementDifferentialPreservesAllValuesKeysAndOrder',
    'UserKeyHashAndEqualityRunOncePerOrdinaryOperationOnly',
    'FallbackCallbackDeleteReinsertKeepsTheNewStoredKey',
    'ActiveIteratorAllowsFusedOverwriteButRejectsInsertAndDeleteReinsert',
    'LargeExternalMutationCannotRenewProofBeyondScanBudget',
}
MIR = {'records_increment_contract_tests::'+n for n in (
    'original_records_retains_named_tuple_dynamic_get_add_and_write',
    'observable_key_and_generic_receiver_still_have_the_same_candidate_spelling',
    'local_observation_does_not_authorize_discarding_the_key')}
IGNORED = {'trace_frame_tests::original_frames_has_pc_partitioned_retry_frame_obligations',
           'trace_frame_tests::renamed_generator_and_factory_alias_keep_frame_proof',
           'trace_frame_tests::repeated_actual_match_block_converges_without_epoch_growth',
           'trace_frame_dataflow::tests::actual_mir_diamond_reaches_fixed_point'}


def check_cs(root):
    rows = root.findall('.//t:UnitTestResult', NS)
    assert len(rows) == 11 and all(r.get('outcome') == 'Passed' for r in rows)
    assert {r.get('testName') for r in rows} == {CLASS+n for n in CASES}
    for field in ('testId', 'executionId'):
        ids = [r.get(field) for r in rows]
        assert all(ids) and len(set(ids)) == 11
    summaries = root.findall('t:ResultSummary', NS)
    assert len(summaries) == 1 and summaries[0].get('outcome') == 'Completed'
    counters = summaries[0].findall('t:Counters', NS)
    assert len(counters) == 1
    for name in ('total', 'executed', 'passed'):
        assert counters[0].get(name) == '11'
    for name in ('failed', 'error', 'timeout', 'aborted', 'inconclusive', 'passedButRunAborted',
                 'notRunnable', 'notExecuted', 'disconnected', 'warning', 'completed', 'inProgress', 'pending'):
        assert counters[0].get(name) == '0'
    return {'passed': 11}


def check_mir(text):
    rows = re.findall(r'^test ([\w:]+) \.\.\. (ok|FAILED|ignored)\b', text, re.M)
    assert len({n for n, _ in rows}) == len(rows)
    outcomes = dict(rows)
    assert {n for n in outcomes if n.startswith('records_increment_contract_tests::')} == MIR
    assert all(outcomes[n] == 'ok' for n in MIR)
    assert {n for n, result in rows if result == 'ignored'} == IGNORED
    assert not any(result == 'FAILED' for _, result in rows)
    summaries = re.findall(r'^test result: ok\. (\d+) passed; (\d+) failed; (\d+) ignored; (\d+) measured; (\d+) filtered out;', text, re.M)
    assert len(summaries) == 1
    passed, failed, ignored, measured, filtered = map(int, summaries[0])
    assert passed == len(rows)-4 and passed > 3 and ignored == 4 and failed == measured == filtered == 0
    return {'passed': passed, 'ignored': sorted(IGNORED), 'new_structure_tests': 3}


if __name__ == '__main__':
    folder = Path(sys.argv[1]); sha = sys.argv[2]
    report = {'commit': sha, 'ok': False, 'checks': {}, 'scope': 'Runtime合同及源码MIR结构；未启用默认融合，无AOT收益结论'}
    checks = {
        'original': lambda: verify(folder, sha),
        'materialized': lambda: check_cs(ET.parse(folder/'materialized.trx').getroot()),
        'mir': lambda: check_mir((folder/'mir.log').read_text()),
        'commands': lambda: json.loads((folder/'rust-exits.json').read_text()) == {'fmt': 0, 'mir': 0},
    }
    for name, check in checks.items():
        try:
            result = check(); assert result is not False
            report['checks'][name] = {'ok': True, 'result': result}
        except Exception as error:
            report['checks'][name] = {'ok': False, 'error': type(error).__name__+': '+str(error)}
    report['ok'] = all(c['ok'] for c in report['checks'].values())
    (folder/'materialized-evidence.json').write_text(json.dumps(report, ensure_ascii=False, indent=2))
    print(json.dumps(report, ensure_ascii=False, indent=2))
    sys.exit(0 if report['ok'] else 1)
