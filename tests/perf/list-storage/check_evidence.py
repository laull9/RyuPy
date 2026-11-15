#!/usr/bin/env python3
"""校验隔离存储 CI 确实执行合同与双臂筛选；不计算或宣称产品性能收益。"""
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def check(directory: Path) -> None:
    root = ET.parse(directory / "list-storage.trx").getroot()
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    results = root.findall("t:Results/t:UnitTestResult", ns)
    assert len(results) >= 26, f"合同未充分执行：{len(results)}"
    assert all(item.attrib["outcome"] == "Passed" for item in results), "合同含失败/跳过"
    assert all("PySegmentedListStorageTests" in item.attrib["testName"] for item in results)
    data = json.loads((directory / "samples.json").read_text())
    assert data["schema"] == "list-storage-prototype-v2"
    assert data["frequency"] > 0
    assert data["framework"].startswith(".NET 10.")
    samples = data["samples"]
    assert len(samples) == 60, "双臂三阶段必须各有十轮原始样本"
    actual = {(s["round"], s["mode"], s["stage"]) for s in samples}
    expected = {(i, mode, stage) for i in range(10)
                for mode in ("contiguous", "segmented")
                for stage in ("append", "slice", "extend")}
    assert actual == expected, "样本重复或缺失"
    for sample in samples:
        assert sample["ticks"] > 0
        assert sample["allocated_bytes"] >= 0
    print(f"通过：{len(results)} 个合同、60 个原始筛选样本；非产品 AOT 验收。")


if __name__ == "__main__":
    check(Path(sys.argv[1]))
