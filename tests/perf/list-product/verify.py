#!/usr/bin/env python3
"""产品切片门：完整字节双流差分、真实AOT入口、26项配对结果与定向TRX计数。"""
import argparse
from collections import Counter
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tests"))
from benchlib import measure, suite


def semantic(output, python, old, new):
    output.mkdir(parents=True, exist_ok=True)
    source = ROOT / "tests/e2e/phase2/m2_list_slice_owned_result.py"
    bench = suite.Benchmark(name=source.stem, path=source)
    env = os.environ.copy()
    env.pop("RYUPY_RUNTIME_DIR", None)
    os.environ.pop("RYUPY_RUNTIME_DIR", None)

    def run(label, command, cwd=ROOT, environment=env):
        result = subprocess.run(command, cwd=cwd, env=environment, capture_output=True)
        (output / (label + ".stdout")).write_bytes(result.stdout)
        (output / (label + ".stderr")).write_bytes(result.stderr)
        (output / (label + ".exit")).write_text(str(result.returncode))
        assert result.returncode == 0, label
        return result.stdout, result.stderr

    expected = run("CP", [str(python), "-B", str(source)])
    completed = []
    workspace = tempfile.TemporaryDirectory(prefix="ryupy-slice-semantic-")
    work = Path(workspace.name)
    for label, cli in (("base", old), ("candidate", new)):
        for backend in ("jit", "aot"):
            arm = label + "-" + backend
            exe, _, error, status = measure.build_case(ROOT, cli, bench, work / arm, backend=backend)
            (output / (arm + "-build.log")).write_text(error)
            assert exe is not None, (arm, status, error)
            command = [str(exe)] if backend == "aot" else ["dotnet", str(exe)]
            assert run(arm, command) == expected, arm
            if backend == "aot":
                moved = work / (arm + " 中文 空格搬迁")
                shutil.copytree(exe.parent, moved)
                exe.parent.rename(exe.parent.with_name(exe.parent.name + ".hidden"))
                assert run(arm + "-moved", [str(moved / exe.name)], moved,
                           env | {"PATH": "/nonexistent"}) == expected
            completed.append(arm)
            (output / "completed.json").write_text(json.dumps(completed))


def trx(path):
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    root = ET.parse(path).getroot()
    results = root.findall("t:Results/t:UnitTestResult", ns)
    assert len(results) == 20
    assert len({r.attrib["testId"] for r in results}) == 20
    assert len({r.attrib["testName"] for r in results}) == 20
    assert all(r.attrib["outcome"] == "Passed" for r in results)
    assert all(r.attrib["testName"].startswith("RyuPy.Runtime.Tests.PyListSliceOwnershipTests.") for r in results)
    expected_families = {
        "SlicesHaveIndependentSlotsAndRetainElementReferences": 5,
        "PublicEnumerableConstructorStillCopiesExternalMutableList": 1,
        "SpecialBackingMaterializesOnlyForNonemptySlice": 2,
        "SliceAndSourceIteratorsRemainIndependentAcrossMutations": 1,
        "ExtremeStepsKeepTheSingleSelectedReference": 4,
        "EmptySourceRemainsIndependentAndDoesNotMaterialize": 2,
        "PreviouslyCachedNumericBoxSurvivesSliceMaterialization": 1,
        "InvalidSliceBoundsDoNotMaterializeSpecialBacking": 4,
    }
    prefix = "RyuPy.Runtime.Tests.PyListSliceOwnershipTests."
    assert Counter(r.attrib["testName"][len(prefix):].split("(", 1)[0] for r in results) == expected_families
    summary = root.find("t:ResultSummary", ns)
    assert summary.attrib["outcome"] == "Completed"
    counters = root.find("t:ResultSummary/t:Counters", ns)
    for key in ("total", "executed", "passed"): assert int(counters.attrib[key]) == 20
    for key, value in counters.attrib.items():
        if key not in ("total", "executed", "passed"): assert int(value) == 0


def performance(path):
    data = json.loads(path.read_text())
    expected = {b.name for b in suite.discover(ROOT / "tests/benchmarks")}
    assert len(expected) == 26 and {"list_ops", "matmul"} <= expected
    assert len(data["rows"]) == 26
    assert {r["name"] for r in data["rows"]} == expected
    assert data["runs"] == 5 and data["warmup"] == 1
    for row in data["rows"]:
        assert set(row["samples"]) == {"CP", "A", "B"}
        for samples in row["samples"].values():
            assert len(samples) == 5 and all(s["ms"] > 0 for s in samples)
        for arm in ("A", "B"):
            assert {"main_ms", "alloc_bytes"} <= row["stats"][arm].keys()
    print("26项完整双流差分、配对原始样本及两臂搬迁均完成；不是暖态性能结论")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=("semantic", "trx", "performance"))
    parser.add_argument("output", type=Path)
    parser.add_argument("--python", type=Path)
    parser.add_argument("--old", type=Path)
    parser.add_argument("--new", type=Path)
    args = parser.parse_args()
    if args.mode == "semantic": semantic(args.output, args.python, args.old, args.new)
    elif args.mode == "trx": trx(args.output)
    else: performance(args.output)
