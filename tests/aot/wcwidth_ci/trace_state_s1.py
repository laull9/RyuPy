#!/usr/bin/env python3
"""有限 S1 专用门：真实 Release C# 测试/PE/旧 ABI，保留每步原流和精确 TRX。"""
import collections
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "target/trace-state-s1-evidence"
BASE = ROOT / "target/trace-s1-baseline"
BASE_REVISION = "bd5d7dbe1f09bd3ba649e9b1c1928845784e4fe8"
BASE_TREE = "aff0fc2cf409551b5287c638cefe131a1ddd0e1f"
EXPECTED = {
    "ExclusiveForwardThenInclusiveHandlePrependsOnlyTraversedFramesToLatestHistory": 1,
    "FailedFreshBeginDoesNotConsumeBirthGrantAndLegalRetrySucceeds": 1,
    "ExplicitPermanentlyRevokesBirthEvenWhenCommittedHistoryIsEmpty": 2,
    "FirstEmptyForwardIsLegalButNewSequenceCannotResubmitTheBoundary": 4,
    "ReceiveRejectsInvalidModeCombinationsWithoutPublishing": 1,
    "RootHandlesOnlyRealCallbackFramesAndNeverInfersFreshFromEmptyHistory": 2,
    "ClosingReceiveDiscardsOuterPendingWithoutRestoringHistory": 2,
    "OnlyLatestReceiveReplaysAndNewRequestInvalidatesTheReceipt": 1,
    "InvalidInputsLeaveEveryStateFieldAndGrantUntouched": 1,
    "SceneAndSnapshotsAreDefensiveAndSameNamedActivationsRemainDistinct": 1,
    "TwoOwnersJoinOneEpochWithoutConsumingItsIdentity": 2,
    "IssuingNewEpochLeavesOldFlightReceivableAndOtherOwnerCanRotateFirst": 1,
    "PermitRejectsForgeryWrongOwnerStaleVersionAndSupersededTarget": 1,
    "FailedBeginKeepsAnAlreadyIssuedPermitValid": 1,
    "ProducedAssemblyHasNoOldTypeReferencesToCoreAndNoNewInitializers": 1,
    "ProducedAssemblyPreservesEveryBaselinePublicSignature": 1,
}


def write(name, value):
    (OUT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n")


def run(label, command, env):
    write(label + "-command.json", {"argv": command, "cwd": str(ROOT)})
    with (OUT / (label + ".stdout")).open("wb") as stdout, (OUT / (label + ".stderr")).open("wb") as stderr:
        result = subprocess.run(command, cwd=ROOT, env=env, stdout=stdout, stderr=stderr, check=False)
    write(label + "-exit.json", {"returncode": result.returncode})
    print(f"{label}: exit={result.returncode}", flush=True)
    if result.returncode:
        raise RuntimeError(f"{label}失败；保留原stdout/stderr，不自动重试。")


def verify_trx(path):
    tree = ET.parse(path)
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    definitions = {}
    for unit in tree.findall(".//t:TestDefinitions/t:UnitTest", ns):
        method = unit.find("t:TestMethod", ns)
        if method is None:
            raise RuntimeError("TRX 缺少 TestMethod。")
        definitions[unit.attrib["id"]] = method.attrib["name"]
    results = tree.findall(".//t:Results/t:UnitTestResult", ns)
    records = [{"name": row.attrib["testName"], "method": definitions[row.attrib["testId"]],
                "outcome": row.attrib["outcome"]} for row in results]
    write("executed-tests.json", records)
    counts = collections.Counter(row["method"] for row in records)
    if dict(counts) != EXPECTED or not records or any(row["outcome"] != "Passed" for row in records):
        raise RuntimeError("S1 必须精确执行全部预期案例，非零、零失败、零跳过；见 executed-tests.json。")
    counters = tree.find(".//t:ResultSummary/t:Counters", ns)
    if counters is None or int(counters.attrib["total"]) != len(records) or int(counters.attrib["passed"]) != len(records):
        raise RuntimeError("TRX 汇总计数不符。")
    return records


def main():
    OUT.mkdir(parents=True, exist_ok=False)
    env = os.environ.copy()
    dotnet = env.get("DOTNET", "dotnet")
    # 同一次 SDK，旧/新 obj/bin 都分离，不能污染当前或基线 checkout。
    actual = subprocess.check_output(["git", "-C", str(BASE), "rev-parse", "HEAD"], text=True).strip()
    baseline_tree = subprocess.check_output(["git", "-C", str(BASE), "rev-parse", "HEAD^{tree}"], text=True).strip()
    if actual != BASE_REVISION or baseline_tree != BASE_TREE:
        raise RuntimeError("旧 ABI checkout 提交或源树身份不符。")
    write("identity.json", {"baseline": actual, "baseline_tree": baseline_tree, "candidate": subprocess.check_output(
        ["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(), "dotnet": dotnet})
    run("sdk", [dotnet, "--info"], env)
    baseline_artifacts = OUT / "baseline-artifacts"
    candidate_artifacts = OUT / "candidate-artifacts"
    run("baseline-build", [dotnet, "build", str(BASE / "runtime/RyuPy.Runtime/RyuPy.Runtime.csproj"),
        "-c", "Release", "--artifacts-path", str(baseline_artifacts), "-nologo"], env)
    run("candidate-build", [dotnet, "build", "runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj",
        "-c", "Release", "--artifacts-path", str(candidate_artifacts), "-nologo"], env)
    # SDK 可以采用 release 或 release_net10.0 pivot；只接受本次独立目录中的唯一运行时产物。
    def runtime_dll(artifacts):
        matches = list((artifacts / "bin/RyuPy.Runtime").glob("**/RyuPy.Runtime.dll"))
        if len(matches) != 1:
            raise RuntimeError(f"本次运行时产物不是唯一一份：{matches}")
        return matches[0]
    baseline_dll = runtime_dll(baseline_artifacts)
    candidate_dll = runtime_dll(candidate_artifacts)
    identities = {}
    for name, path in (("baseline", baseline_dll), ("candidate", candidate_dll)):
        if not path.is_file():
            raise RuntimeError(f"缺少本次真实产物：{path}")
        blob = path.read_bytes()
        identities[name] = {"path": str(path), "bytes": len(blob), "sha256": hashlib.sha256(blob).hexdigest()}
    write("assembly-identities.json", identities)
    env.update(RYUPY_TRACE_BASELINE_DLL=str(baseline_dll), RYUPY_TRACE_CANDIDATE_DLL=str(candidate_dll),
               RYUPY_TRACE_EVIDENCE_DIR=str(OUT))
    common = [dotnet, "test", "runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj", "-c", "Release",
              "--artifacts-path", str(candidate_artifacts), "--no-build", "--no-restore", "-nologo",
              "--filter", "FullyQualifiedName~PyTraceState"]
    run("discover", common + ["--list-tests"], env)
    # 非零 test runner 也先保留原流；如有 TRX，仍解析并保存执行明细。
    error = None
    try:
        run("tests", common + ["--logger", "trx;LogFileName=trace-state-s1.trx", "--results-directory", str(OUT / "trx")], env)
    except RuntimeError as exc:
        error = exc
    records = verify_trx(OUT / "trx/trace-state-s1.trx")
    if error is not None:
        raise error
    for required in ("candidate-pe.json", "baseline-abi.json"):
        if not (OUT / required).is_file():
            raise RuntimeError(f"缺少真实PE证据：{required}")
    write("result.json", {"status": "passed", "executed": len(records), "scope": "S1 内核/PE/公开ABI；非traceback修复或旧项目全绿"})


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        if OUT.is_dir():
            write("result.json", {"status": "failed", "error": str(exc)})
        print(str(exc), file=sys.stderr)
        sys.exit(1)
