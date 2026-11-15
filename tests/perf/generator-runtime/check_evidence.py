#!/usr/bin/env python3
"""生成器 Runtime 隔离 CI 的 TRX 门禁：防止零测试、重复记录与合同遗漏。"""
from collections import Counter
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

# 对应当前 PyGeneratorSumTests 的 13 方法、16 个参数化实例。
CONTRACTS = {
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


def check(path: Path, mode: str) -> None:
    assert mode in ("focused", "full")
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    root = ET.parse(path).getroot()
    results = root.findall("t:Results/t:UnitTestResult", ns)
    assert results, "没有执行测试"
    ids = [r.attrib["testId"] for r in results]
    assert all(ids) and len(set(ids)) == len(ids), "testId 为空或重复"
    assert all(r.attrib["outcome"] == "Passed" for r in results), "测试失败或跳过"
    summary = root.find("t:ResultSummary", ns)
    assert summary is not None and summary.attrib["outcome"] == "Completed"
    counters = summary.find("t:Counters", ns)
    assert counters is not None, "缺失汇总"
    for key in ("total", "executed", "passed"):
        assert int(counters.attrib[key]) == len(results), f"汇总 {key} 不一致"
    for key, value in counters.attrib.items():
        if key not in ("total", "executed", "passed"):
            assert int(value) == 0, f"汇总有未通过记录：{key}"
    names = [r.attrib["testName"] for r in results]
    focused = [name for name in names if name.startswith(PREFIX)]
    assert len(set(focused)) == len(focused), "定向合同名称重复"
    families = Counter(name[len(PREFIX):].split("(", 1)[0] for name in focused)
    assert families == CONTRACTS, f"定向合同族不完整：{families}"
    if mode == "focused":
        assert len(results) == 16, "定向套件混入其他测试"
    else:
        # 全套无过滤命令负责完整发现；门禁另防意外只执行定向套件，并记录实际数。
        assert len(results) > 16 and len(focused) < len(results), "全套退化为定向测试"
    print(f"{mode}: {len(results)} 个测试全部通过；包含 13 方法/16 实例生成器合同。")


if __name__ == "__main__":
    check(Path(sys.argv[1]), sys.argv[2])
