"""严格核对独立host归因身份、配置、完整有序阶段矩阵及独立算术oracle。"""
import json
import math
from pathlib import Path
import re
import xml.etree.ElementTree as ET

CLASS = "RyuPy.Runtime.Tests.PyDictProbeAttributionTests"
SAFETY = "FusedIncrementPreservesStoredKeyAndRejectsOverflowOrNonLong"
STAGES = ("guard", "allocate", "hash-tuple", "hash-pair", "index-tuple", "index-pair-cached",
          "index-pair", "get", "probe-get", "aggregate-tuple", "aggregate-probe", "aggregate-fused")
SCENARIOS = {"SmallTable": (32, False), "LargeTable": (4096, False), "CollisionTable": (32, True)}
CASES = [(SAFETY, "default", "0")] + [(case, tier, reverse) for case in SCENARIOS
         for tier in ("default", "off") for reverse in ("0", "1")]
NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
ROW = re.compile(r"attribution keys=(\d+) collision=(True|False) round=(\d+) stage=(\S+) "
                 r"ms=(\S+) bytes=(\d+) gen0=(\d+) gen1=(\d+) gen2=(\d+) pauseMs=(\S+) sum=(-?\d+)")
CONFIG = re.compile(r"attribution-config keys=(\d+) collision=(True|False) reverse=(True|False) "
                    r"tiering=(default|0) framework=(\.NET 10\.\d+\.\d+) cpu=([1-9]\d*) serverGC=(True|False) token=([0-9a-f]{32})")


def require(condition, message):
    if not condition:
        raise ValueError(message)


def identity(case, tier, reverse):
    return f"{case}-{tier}-reverse{reverse}"


def signed(value, bits):
    return (value + (1 << (bits - 1))) % (1 << bits) - (1 << (bits - 1))


def reference_hash(first):
    def text(value):
        h = 0
        for ch in value:
            h = signed(h * 1000003 + ord(ch), 64)
            h = (1 if h >= 0 else -1) * (abs(h) % ((1 << 61) - 1))
        return h
    h = signed(0x345678 * 1000003, 64) ^ text(first)
    h = signed(h * 1000003, 64) ^ text("buy")
    return signed(h ^ (h >> 32), 32)


def expected_sums(size, collision):
    count = 120000
    weights = [count // size + (i < count % size) for i in range(size)]
    slot_sum = sum(i * n for i, n in enumerate(weights))
    hash_sum = sum(reference_hash("\0" * (i + 1) if collision else f"user{i}") * n for i, n in enumerate(weights))
    return {stage: count if stage == "guard" else hash_sum if stage.startswith("hash-")
            else slot_sum if stage.startswith("index-") else slot_sum + count * 7 if stage in ("get", "probe-get")
            else 0 for stage in STAGES}


def evidence_lines(text):
    return [line.strip() for line in text.splitlines() if line.strip().startswith("attribution")]


def verify_case(folder, case, tier, reverse, token):
    name = identity(case, tier, reverse)
    root = ET.parse(folder / f"{name}.trx").getroot()
    results = root.findall(".//t:UnitTestResult", NS)
    require(len(results) == 1, "必须精确一个测试结果")
    result = results[0]
    require(result.get("testName") == f"{CLASS}.{case}" and result.get("outcome") == "Passed", "测试名称或结果错误")
    require(all(result.get(key) for key in ("testId", "executionId")), "缺测试身份")
    definitions = root.findall(".//t:UnitTest", NS)
    require(len(definitions) == 1 and definitions[0].get("id") == result.get("testId"), "测试定义身份错误")
    method = definitions[0].find("t:TestMethod", NS)
    execution = definitions[0].find("t:Execution", NS)
    require(method is not None and method.get("className") == CLASS and method.get("name") == case, "测试方法错误")
    require(execution is not None and execution.get("id") == result.get("executionId"), "执行身份错误")
    summaries = root.findall("t:ResultSummary", NS)
    require(len(summaries) == 1 and summaries[0].get("outcome") == "Completed", "汇总状态错误")
    counters = summaries[0].findall("t:Counters", NS)
    require(len(counters) == 1, "汇总计数缺失或重复")
    for key in ("total", "executed", "passed"):
        require(counters[0].get(key) == "1", "通过计数错误")
    for key in ("failed", "error", "timeout", "aborted", "inconclusive", "passedButRunAborted", "notRunnable",
                "notExecuted", "disconnected", "warning", "completed", "inProgress", "pending"):
        require(counters[0].get(key) == "0", "非通过计数错误")
    stdout = "\n".join(node.text or "" for node in result.findall(".//t:StdOut", NS))
    lines = evidence_lines(stdout)
    require(lines == evidence_lines((folder / f"{name}.log").read_text()), "TRX与console证据不一致")
    if case == SAFETY:
        require(lines == [f"attribution-safety token={token}"], "安全测试运行token错误")
        return
    size, collision = SCENARIOS[case]
    require(len(lines) == 73, "需要一条配置和精确72条样本")
    config = CONFIG.fullmatch(lines[0])
    require(config is not None, "配置格式错误")
    require(config.group(1, 2, 3, 4) == (str(size), str(collision), str(reverse == "1"), "0" if tier == "off" else "default"), "运行配置不符")
    require(config.group(8) == token, "旧运行token")
    stages = list(STAGES)
    if reverse == "1":
        stages.reverse()
    expected_order = [(round_no, stage) for round_no in range(6)
                      for stage in (stages if round_no % 2 == 0 else stages[::-1])]
    sums = expected_sums(size, collision)
    for text, expected in zip(lines[1:], expected_order):
        row = ROW.fullmatch(text)
        require(row is not None, "样本格式错误")
        require(row.group(1, 2) == (str(size), str(collision)), "样本场景不符")
        require((int(row[3]), row[4]) == expected, "阶段/轮次缺失重复或乱序")
        require(all(math.isfinite(float(row[i])) and float(row[i]) >= 0 for i in (5, 10)), "耗时非finite或为负")
        require(int(row[11]) == sums[row[4]], "阶段独立oracle错误")


def verify_all(folder, sha):
    summary = json.loads((folder / "summary.json").read_text())
    manifest = json.loads((folder / "manifest.json").read_text())
    require(manifest.get("commit") == sha and manifest.get("sdk") == "10.0.403", "提交/SDK错误")
    token = manifest.get("token", "")
    require(re.fullmatch(r"[0-9a-f]{32}", token) is not None, "运行token缺失")
    require(summary.get("commit") == sha and summary.get("token") == token and summary.get("sdk") == "10.0.403", "汇总身份错误")
    wanted = [{"case": identity(*case), "exit": 0, "passed": True} for case in CASES]
    require(summary.get("runs") == wanted, "汇总缺失重复或未通过")
    require(all(type(run.get("exit")) is int and type(run.get("passed")) is bool for run in summary["runs"]), "汇总类型错误")
    for case in CASES:
        verify_case(folder, *case, token)
