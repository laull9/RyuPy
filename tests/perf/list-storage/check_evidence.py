#!/usr/bin/env python3
"""校验隔离存储 CI 确实执行合同与双臂筛选；不计算或宣称产品性能收益。"""
from collections import Counter
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


# 当前定向套件的合同族与参数化实例数；测试增加时同步调整，不能以重复记录充数。
CONTRACTS = {
    "SelfExtendFreezesLengthAndPreservesReferences": 6,
    "ExportIsPermanentAndExternalMutationIsVisible": 1,
    "SliceHasIndependentSlotsButSharedElementIdentity": 1,
    "LiveIteratorSeesAppendAndOverwriteButExhaustionSticks": 1,
    "PopDropsSegmentsAndPreservesOrderAcrossRefill": 1,
    "FailedConsumptionCommitsOnlySuccessfulPrefix": 1,
    "CrossStorageExtendRetainsSourceAndReferences": 4,
    "UniqueSliceElementsExposeCrossSegmentOrdering": 8,
    "LiveIteratorTracksDeletionAcrossMaterialization": 1,
    "ClearThenReuseRetainsModeAndLiveCursorPosition": 2,
    "ContiguousSlicesCopyUniqueReferencesAcrossBothBoundaries": 6,
    "CompactSmallSlicePreservesIndependentIdentityAndLaterMutation": 7,
    "CompactSliceLiveCursorObservesExportedWritesAndClear": 1,
}


def check(directory: Path) -> None:
    root = ET.parse(directory / "list-storage.trx").getroot()
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    results = root.findall("t:Results/t:UnitTestResult", ns)
    assert len(results) == sum(CONTRACTS.values()), f"合同数量不符：{len(results)}"
    ids = [item.attrib["testId"] for item in results]
    names = [item.attrib["testName"] for item in results]
    assert all(ids) and len(set(ids)) == len(ids), "testId 为空或重复"
    assert len(set(names)) == len(names), "测试名称重复"
    prefix = "RyuPy.Runtime.Tests.PySegmentedListStorageTests."
    assert all(name.startswith(prefix) for name in names), "包含其他测试类"
    families = Counter(name[len(prefix):].split("(", 1)[0] for name in names)
    assert families == CONTRACTS, f"合同族覆盖不符：{families}"
    summary = root.find("t:ResultSummary", ns)
    assert summary is not None and summary.attrib["outcome"] == "Completed"
    counters = summary.find("t:Counters", ns)
    assert counters is not None, "缺失测试汇总"
    for field in ("total", "executed", "passed"):
        assert int(counters.attrib[field]) == len(results), f"汇总 {field} 不一致"
    for field, value in counters.attrib.items():
        if field not in ("total", "executed", "passed"):
            assert int(value) == 0, f"测试汇总有非通过记录：{field}"
    assert all(item.attrib["outcome"] == "Passed" for item in results), "合同含失败/跳过"
    data = json.loads((directory / "samples.json").read_text())
    assert data["schema"] == "list-storage-prototype-v3"
    assert data["frequency"] > 0
    assert data["framework"].startswith(".NET 10.")
    samples = data["samples"]
    assert len(samples) == 80, "双臂四阶段必须各有十轮原始样本"
    actual = {(s["round"], s["mode"], s["stage"]) for s in samples}
    expected = {(i, mode, stage) for i in range(10)
                for mode in ("contiguous", "segmented")
                for stage in ("append", "slice", "slice_normalized", "extend")}
    assert actual == expected, "样本重复或缺失"
    for sample in samples:
        assert sample["ticks"] > 0
        assert sample["allocated_bytes"] >= 0
    print(f"通过：{len(results)} 个合同、80 个原始筛选样本；非产品 AOT 验收。")


if __name__ == "__main__":
    check(Path(sys.argv[1]))
