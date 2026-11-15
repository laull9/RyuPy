#!/usr/bin/env python3
"""生成器 Runtime 隔离 CI 的 TRX 门禁：防止零测试、重复记录与合同遗漏。"""
from collections import Counter
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

# 对应当前 PyGeneratorSumTests 的 14 方法、17 个参数化实例。
CONTRACTS = {
    "ExperimentalCapabilityDoesNotAddPublicAbi": 1,
    "LongRunUsesOneResumeAndDiscardsReturnValue": 1,
    "EmptyPreservesStartAndZeroStillMakesResultDirty": 1,
    "OverflowItemIsAddedExactlyOnce": 2,
    "ConsumerStopIterationDoesNotEnterProducerPep479OrCloseIt": 1,
    "PausedGeneratorKeepsResumePositionAndOrdinarySend": 1,
    "ProducerStopIterationStillUsesPep479": 1,
    "CallbackReturningLongKeepsItsBoxWhenNoLaterAdditionOccurs": 1,
    "ProducerReentryIsRejectedButConsumerCallbackCanResume": 1,
    "TraceDomainRejectsBatchWithoutConsuming": 1,
    "MissingCapabilityFallsBackToCompleteOrdinarySum": 1,
    "InitialNonNoneSendDoesNotStartOrCloseBatchCapableGenerator": 1,
    "ConsumerCallbackMayControlPausedGenerator": 3,
    "ProducerErrorRestoresCallerContextAndReleasesCapability": 1,
}
PREFIX = "RyuPy.Runtime.Tests.PyGeneratorSumTests."
TRACE_CONTRACTS = {
    "RyuPy.Runtime.Tests.PyTraceStateArtifactTests.GeneratorSumBridgeRejectsWrongSignatureAndExtraAuthority": 1,
    "RyuPy.Runtime.Tests.PyTraceStateArtifactTests.ActualPeRejectsGeneratorSumAuthorityAndFinallyOrderChanges": 2,
}
# 属性名不是测试身份：精确映射源码中的类与方法，不允许同类其他测试跳过。
ALLOWED_SKIPS = {
    "RyuPy.Runtime.Tests.PyTextDedentInitializationTests.FreshProcessContract",
    "RyuPy.Runtime.Tests.PyTraceStateArtifactTests.ProducedAssemblyPreservesEveryBaselinePublicSignature",
}


def check(path: Path, mode: str) -> None:
    assert mode in ("focused", "full")
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    root = ET.parse(path).getroot()
    results = root.findall("t:Results/t:UnitTestResult", ns)
    assert results, "没有执行测试"
    ids = [r.attrib["testId"] for r in results]
    assert all(ids) and len(set(ids)) == len(ids), "testId 为空或重复"
    definitions = root.findall("t:TestDefinitions/t:UnitTest", ns)
    definition_ids = [d.attrib["id"] for d in definitions]
    assert len(set(definition_ids)) == len(definition_ids), "测试定义 ID 重复"
    identities = {}
    for definition in definitions:
        method = definition.find("t:TestMethod", ns)
        assert method is not None
        identities[definition.attrib["id"]] = (
            method.attrib["className"].split(",", 1)[0] + "." + method.attrib["name"])
    for result in results:
        assert identities[result.attrib["testId"]] == result.attrib["testName"].split("(", 1)[0], "测试源码身份不符"
    skipped = [r for r in results if r.attrib["outcome"] == "NotExecuted"]
    passed = [r for r in results if r.attrib["outcome"] == "Passed"]
    assert len(passed) + len(skipped) == len(results), "测试有失败或未知状态"
    skip_names = [identities[r.attrib["testId"]] for r in skipped]
    assert len(set(skip_names)) == len(skip_names), "白名单跳过重复"
    assert not skipped if mode == "focused" else set(skip_names) <= ALLOWED_SKIPS, "存在未授权跳过"
    summary = root.find("t:ResultSummary", ns)
    assert summary is not None and summary.attrib["outcome"] == "Completed"
    counters = summary.find("t:Counters", ns)
    assert counters is not None, "缺失汇总"
    # 固定 Microsoft.NET.Test.Sdk/VSTest 17.14.1 的 TRX logger：
    # total 包含跳过，executed=passed+failed；summary.notExecuted 固定为0，
    # 不是逐项 NotExecuted 的计数。跳过仍逐项精确白名单核验，且总数必须守恒。
    # 来源：microsoft/vstest v17.14.1 的 TrxLogger.cs 与 ObjectModel/TestRunSummary.cs。
    expected = {"total": len(results), "executed": len(passed),
                "passed": len(passed), "notExecuted": 0}
    for key, count in expected.items():
        assert int(counters.attrib.get(key, "0")) == count, f"汇总 {key} 不一致"
    for key, value in counters.attrib.items():
        if key not in expected:
            assert int(value) == 0, f"汇总有未通过记录：{key}"
    names = [r.attrib["testName"] for r in results]
    focused = [name for name in names if name.startswith(PREFIX)]
    assert len(set(focused)) == len(focused), "定向合同名称重复"
    families = Counter(name[len(PREFIX):].split("(", 1)[0] for name in focused)
    assert families == CONTRACTS, f"定向合同族不完整：{families}"
    if mode == "focused":
        assert len(results) == 17, "定向套件混入其他测试"
    else:
        trace_names = [r.attrib["testName"] for r in passed
                       if identities[r.attrib["testId"]] in TRACE_CONTRACTS]
        assert len(set(trace_names)) == len(trace_names), "PE 合同实例重复"
        trace_families = Counter(name.split("(", 1)[0] for name in trace_names)
        assert trace_families == TRACE_CONTRACTS, "实际 PE 变异与桥签名合同未全部执行"
        # 全套无过滤命令负责完整发现；门禁另防意外只执行定向套件，并记录实际数。
        assert any(not r.attrib["testName"].startswith(PREFIX) for r in passed), \
            "全套没有任何非定向通过测试"
    print(f"{mode}: {len(results)} 个记录，{len(passed)} 通过，{len(skipped)} 个白名单跳过；"
          "包含 14 方法/17 实例生成器合同。")
    for name in skip_names:
        print("未验证专项：" + name)


if __name__ == "__main__":
    check(Path(sys.argv[1]), sys.argv[2])
