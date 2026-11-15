#!/usr/bin/env python3
"""尺寸诊断完整性检查；不使用速度阈值掩盖负结果。"""
import json
from pathlib import Path
import sys

SIZES = (0, 1, 64, 500, 1024, 1025, 4096)
ORDERS = (("contiguous", "fixed", "compact"), ("fixed", "compact", "contiguous"),
          ("compact", "contiguous", "fixed"), ("compact", "fixed", "contiguous"),
          ("fixed", "contiguous", "compact"), ("contiguous", "compact", "fixed"))


def check(data):
    assert data["schema"] == "list-slice-size-study-v1"
    assert data["frequency"] > 0 and data["framework"].startswith(".NET 10.")
    samples = data["samples"]
    expected = {(r, m, n, start) for r in range(12) for m in ORDERS[0]
                for n in SIZES for start in (0, 1000)}
    assert len(samples) == len(expected) == 504
    actual = {(s["round"], s["mode"], s["size"], s["start"]) for s in samples}
    assert actual == expected, "样本缺失或重复"
    for s in samples:
        assert s["order_index"] == ORDERS[s["round"] % 6].index(s["mode"])
        assert s["repeats"] == 2000
        assert type(s["ticks"]) is int and s["ticks"] > 0
        assert type(s["allocated_bytes"]) is int and s["allocated_bytes"] >= 0
    print("通过：504个三臂尺寸原始样本；仅存储实验，不是产品性能验收。")


if __name__ == "__main__":
    check(json.loads(Path(sys.argv[1]).read_text()))
