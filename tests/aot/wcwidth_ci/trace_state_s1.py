#!/usr/bin/env python3
"""有限 S1/V1 合同门：保留原内核和历史 ABI，新增精确桥接/生命周期；不代替产品验收。"""
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
# 完整方法身份与固定 InlineData 展开名来自实际 xUnit discovery/TRX 格式。
# 同一 Theory 的短方法名可重复，但每个参数案例的 display name/definition id 必须唯一。
FACT = ("",)
PLAN = {
    "PyTraceStateTests": {
        "ExclusiveForwardThenInclusiveHandlePrependsOnlyTraversedFramesToLatestHistory": FACT,
        "FailedFreshBeginDoesNotConsumeBirthGrantAndLegalRetrySucceeds": FACT,
        "ExplicitPermanentlyRevokesBirthEvenWhenCommittedHistoryIsEmpty": ("(callback: False)", "(callback: True)"),
        "FirstEmptyForwardIsLegalButNewSequenceCannotResubmitTheBoundary": (
            "(root: False, suppress: False)", "(root: True, suppress: False)",
            "(root: False, suppress: True)", "(root: True, suppress: True)"),
        "ReceiveRejectsInvalidModeCombinationsWithoutPublishing": FACT,
        "RootHandlesOnlyRealCallbackFramesAndNeverInfersFreshFromEmptyHistory": ("(callback: False)", "(callback: True)"),
        "ClosingReceiveDiscardsOuterPendingWithoutRestoringHistory": ("(kindValue: 2)", "(kindValue: 1)"),
        "OnlyLatestReceiveReplaysAndNewRequestInvalidatesTheReceipt": FACT,
        "InvalidInputsLeaveEveryStateFieldAndGrantUntouched": FACT,
        "SceneAndSnapshotsAreDefensiveAndSameNamedActivationsRemainDistinct": FACT,
    },
    "PyTraceStateEpochTests": {
        "TwoOwnersJoinOneEpochWithoutConsumingItsIdentity": ("(reverse: False)", "(reverse: True)"),
        "IssuingNewEpochLeavesOldFlightReceivableAndOtherOwnerCanRotateFirst": FACT,
        "PermitRejectsForgeryWrongOwnerStaleVersionAndSupersededTarget": FACT,
        "FailedBeginKeepsAnAlreadyIssuedPermitValid": FACT,
    },
    "PyTraceStateArtifactTests": {
        "ProducedAssemblyHasOnlyApprovedV1BridgesAndNoCoreInitializers": FACT,
        "ProducedAssemblyPreservesEveryBaselinePublicSignature": FACT,
        "ActualPeRejectsReceiveInjectedIntoFramesProjection": FACT,
        "ActualPeRejectsAllMissChangedToConsume": FACT,
        "ActualPeRejectsDefaultBuiltinFreshReturnInsteadOfThrow": FACT,
    },
    "PyTraceStateV1ArtifactContractTests": {
        "PublicIncrementContainsOnlyTheTwelveApprovedRecords": FACT,
        "AbiRejectsMissingChangedExtraAndDuplicateRecords": FACT,
        "CombinedAbiRejectsInvalidAllowListsAndMetadataFlags": FACT,
        "BridgeRulesRejectUnknownSourcesTargetsSignaturesAndOpcodes": FACT,
        "CarrierTemplatesRejectExtraFieldsAndUnlistedMechanicalMembers": FACT,
        "TraceTypeMatchingDoesNotTreatNamePrefixesAsIdentity": FACT,
    },
    "PyTraceStateV1LifecycleTests": {
        "DefaultRuntimeNeverBindsTraceWithoutEnter": FACT,
        "FramesAreDetachedReverseProjectionOfLatestOwnerHistory": FACT,
        "FramesSetterRejectsActiveOrPreviouslyBoundHistory": FACT,
        "EnterIsOnlyDomainStartAndRejectsInvalidRoots": FACT,
        "ExitRejectsReachableUnfinishedStateAndFinishExitClearsScope": FACT,
        "RealGeneratorRetryKeepsActivationAndNextResumeAllocatesNew": FACT,
    },
}
# 原31方法/38案例逐名保留，不滚动旧S1/ABI保护；新两Fact独立列账再合并发现集合。
LEGACY_PLAN = PLAN
TREEBUILDER_PLAN = {
    "PyTraceStateArtifactTests": {
        "ActualPeTreeBuilderProducersRejectMissingWrongReusedEscapedAndJumpedFresh": FACT,
        "TreeBuilderAbiIncrementRejectsMissingDuplicateChangedAndInventedPermissions": FACT,
    },
}
ORDINARY_MI_PLAN = {
    "PyTraceStateArtifactTests": {
        "ProducedMiAssemblyHasExactColdSignatureAndTotal": FACT,
    },
}
PLAN = {cls: dict(methods) for cls, methods in LEGACY_PLAN.items()}
for cls, methods in TREEBUILDER_PLAN.items():
    if set(PLAN.get(cls, {})) & set(methods):
        raise RuntimeError("新TreeBuilder Fact与旧S1身份重复。")
    PLAN.setdefault(cls, {}).update(methods)
for cls, methods in ORDINARY_MI_PLAN.items():
    if set(PLAN.get(cls, {})) & set(methods):
        raise RuntimeError("新MI Fact与旧S1身份重复。")
    PLAN.setdefault(cls, {}).update(methods)
GROUP_EXPECTED = {
    group: {"RyuPy.Runtime.Tests." + cls + "." + method: len(cases)
            for cls, methods in plan.items() for method, cases in methods.items()}
    for group, plan in (("legacy", LEGACY_PLAN), ("treebuilder", TREEBUILDER_PLAN), ("ordinary_mi", ORDINARY_MI_PLAN))
}
EXPECTED = {"RyuPy.Runtime.Tests." + cls + "." + method: len(cases)
            for cls, methods in PLAN.items() for method, cases in methods.items()}
CASE_METHOD = {"RyuPy.Runtime.Tests." + cls + "." + method + case: "RyuPy.Runtime.Tests." + cls + "." + method
               for cls, methods in PLAN.items() for method, cases in methods.items() for case in cases}



def execution_groups(records):
    """原38、TreeBuilder2与MI1分别报告真实TRX结果；失败/缺失仍由全身份门严格拒绝。"""
    report = {}
    for group, expected in GROUP_EXPECTED.items():
        observed = [row for row in records if row["method"] in expected]
        report[group] = {"expected_methods": len(expected), "expected_cases": sum(expected.values()),
                         "executed_cases": len(observed),
                         "passed_cases": sum(row["outcome"] == "Passed" for row in observed),
                         "non_passed_cases": sum(row["outcome"] != "Passed" for row in observed),
                         "cases": [row["name"] for row in observed]}
    return report


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


def verify_discovery(path):
    """实际列表若只给方法，只宣告方法集合；展开案例始终由后续 TRX 独立核验。"""
    lines = Path(path).read_text().splitlines()
    headers = [i for i, line in enumerate(lines) if line.strip() == "The following Tests are available:"]
    violations, names = [], []
    if len(headers) != 1:
        violations.append("discovery 必须有唯一实际测试列表头。")
    else:
        for line in lines[headers[0] + 1:]:
            if not line.strip():
                continue
            if not line.startswith("    "):
                violations.append("discovery 列表出现未知行：" + line)
            names.append(line.strip())
    if len(set(names)) != len(names):
        violations.append("discovery 出现重复测试身份。")
    if set(names) == set(CASE_METHOD) and len(names) == len(CASE_METHOD):
        mode = "expanded_cases"
    elif set(names) == set(EXPECTED) and len(names) == len(EXPECTED):
        mode = "methods_only"
    else:
        mode = "invalid"
        violations.append("discovery 不是精确的完整方法集合或完整展开案例集合。")
    report = {"mode": mode, "names": names, "observed_count": len(names),
              "expected_methods": len(EXPECTED), "expected_cases": len(CASE_METHOD), "violations": violations}
    write("discovered-tests.json", report)
    if violations:
        raise RuntimeError("S1 discovery 身份不符；见 discovered-tests.json。")
    return report


def verify_trx(path, discovery):
    tree = ET.parse(path)
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    definitions, definition_cases, definition_executions = {}, set(), set()
    violations, records = [], []
    units = tree.findall(".//t:TestDefinitions/t:UnitTest", ns)
    for unit in units:
        methods = unit.findall("t:TestMethod", ns)
        executions = unit.findall("t:Execution", ns)
        if len(methods) != 1 or len(executions) != 1:
            violations.append("UnitTest 必须恰有一个 TestMethod 和 Execution。")
            continue
        method = methods[0]
        test_id, name = unit.get("id", ""), unit.get("name", "")
        execution_id = executions[0].get("id", "")
        cls, short_name = method.get("className", ""), method.get("name", "")
        full_name = cls + "." + short_name
        if not test_id.strip() or test_id in definitions:
            violations.append("空或重复 UnitTest id：" + test_id)
            continue  # 不能用后一个定义覆盖前一个。
        if not execution_id.strip() or execution_id in definition_executions:
            violations.append("空或重复 definition Execution id：" + execution_id)
        if not name or name in definition_cases:
            violations.append("空或重复 UnitTest 案例名：" + name)
        if not cls or not short_name or full_name not in EXPECTED or CASE_METHOD.get(name) != full_name:
            violations.append("UnitTest 完整方法/案例映射不符：" + full_name + " / " + name)
        definitions[test_id] = {"method": full_name, "name": name, "execution_id": execution_id}
        definition_cases.add(name)
        definition_executions.add(execution_id)
    if definition_cases != set(CASE_METHOD) or len(units) != len(CASE_METHOD):
        violations.append("TRX definition 必须精确覆盖全部展开案例。")
    seen_tests, seen_executions, seen_cases = set(), set(), set()
    for row in tree.findall(".//t:Results/t:UnitTestResult", ns):
        test_id, execution_id, name = row.get("testId", ""), row.get("executionId", ""), row.get("testName", "")
        definition = definitions.get(test_id)
        if not test_id.strip() or test_id in seen_tests:
            violations.append("空或重复 result testId：" + test_id)
        if not execution_id.strip() or execution_id in seen_executions:
            violations.append("空或重复 result executionId：" + execution_id)
        if not name or name in seen_cases:
            violations.append("空或重复 result 案例名：" + name)
        if definition is None or name != definition["name"] or execution_id != definition["execution_id"]:
            violations.append("result 与 definition 的 id/name/execution 映射不符：" + test_id)
        records.append({"test_id": test_id, "execution_id": execution_id, "name": name,
                        "method": definition["method"] if definition else None, "outcome": row.get("outcome", "")})
        seen_tests.add(test_id); seen_executions.add(execution_id); seen_cases.add(name)
    if seen_tests != set(definitions) or seen_cases != set(CASE_METHOD):
        violations.append("TRX result 必须与 definition 一对一并覆盖全部展开案例。")
    counts = collections.Counter(row["method"] for row in records)
    if dict(counts) != EXPECTED or not records or any(row["outcome"] != "Passed" for row in records):
        violations.append("S1 必须精确执行全部完整方法的预期案例，零失败/跳过。")
    discovered = set(discovery.get("names", []))
    if discovery.get("violations") or (discovery.get("mode") == "expanded_cases" and discovered != seen_cases) or (
            discovery.get("mode") == "methods_only" and discovered != set(counts)) or discovery.get("mode") not in ("expanded_cases", "methods_only"):
        violations.append("TRX 与本次已验证 discovery 的完整身份不一致。")
    counter_rows = tree.findall(".//t:ResultSummary/t:Counters", ns)
    try:
        counters = counter_rows[0].attrib if len(counter_rows) == 1 else {}
        if any(int(counters.get(key, "-1")) != len(records) for key in ("total", "executed", "passed")) or any(
                int(value) != 0 for key, value in counters.items() if key not in ("total", "executed", "passed")):
            violations.append("TRX 汇总必须与全部实际执行且通过的记录一致。")
    except ValueError:
        violations.append("TRX 汇总含非法计数。")
    write("executed-tests.json", records)
    write("trx-validation.json", {"definitions": list(definitions.values()), "method_counts": dict(counts),
          "expected_methods": len(EXPECTED), "expected_cases": len(CASE_METHOD),
          "groups": execution_groups(records), "violations": violations})
    if violations:
        raise RuntimeError("S1 TRX 完整身份/执行结果不符；见 trx-validation.json。")
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
    discovery = verify_discovery(OUT / "discover.stdout")
    # 非零 test runner 也先保留原流；如有 TRX，仍解析并保存执行明细。
    error = None
    try:
        run("tests", common + ["--logger", "trx;LogFileName=trace-state-s1.trx", "--results-directory", str(OUT / "trx")], env)
    except RuntimeError as exc:
        error = exc
    records = verify_trx(OUT / "trx/trace-state-s1.trx", discovery)
    if error is not None:
        raise error
    for required in ("candidate-pe.json", "baseline-abi.json"):
        if not (OUT / required).is_file():
            raise RuntimeError(f"缺少真实PE证据：{required}")
    write("result.json", {"status": "passed", "executed": len(records), "groups": execution_groups(records), "scope": "S1/V1合同：原内核、精确ABI/桥接、有限生命周期；非真实产品JIT/AOT或旧项目全绿"})


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        if OUT.is_dir():
            write("result.json", {"status": "failed", "error": str(exc)})
        print(str(exc), file=sys.stderr)
        sys.exit(1)
