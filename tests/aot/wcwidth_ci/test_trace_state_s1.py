"""S1 采集身份的纯脚本反例；XML/列表为标明的合成数据，不编译或运行 C#、产品。"""
import copy
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET

import trace_state_s1 as gate

NS = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"
N = {"t": NS}


def tag(name):
    return "{" + NS + "}" + name


def synthetic_trx():
    # 元素/属性形状对照冻结 CI003 TRX；这些成功结果仅测试采集器，不是实际执行原件。
    root = ET.Element(tag("TestRun"))
    definitions, results = ET.SubElement(root, tag("TestDefinitions")), ET.SubElement(root, tag("Results"))
    for index, (case, full_method) in enumerate(gate.CASE_METHOD.items()):
        test_id, execution_id = "synthetic-test-" + str(index), "synthetic-execution-" + str(index)
        unit = ET.SubElement(definitions, tag("UnitTest"), id=test_id, name=case)
        ET.SubElement(unit, tag("Execution"), id=execution_id)
        cls, method = full_method.rsplit(".", 1)
        ET.SubElement(unit, tag("TestMethod"), className=cls, name=method)
        ET.SubElement(results, tag("UnitTestResult"), testId=test_id, executionId=execution_id, testName=case, outcome="Passed")
    summary = ET.SubElement(root, tag("ResultSummary"))
    ET.SubElement(summary, tag("Counters"), total=str(len(gate.CASE_METHOD)), executed=str(len(gate.CASE_METHOD)),
                  passed=str(len(gate.CASE_METHOD)), failed="0", notExecuted="0")
    return root


def units(root):
    return root.findall("t:TestDefinitions/t:UnitTest", N)


def results(root):
    return root.findall("t:Results/t:UnitTestResult", N)


class S1CollectionIdentity(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.out = Path(self.temporary.name)
        self.patch = patch.object(gate, "OUT", self.out)
        self.patch.start()
        self.addCleanup(self.patch.stop)

    def discovery(self, names=None):
        path = self.out / "discover.stdout"
        chosen = gate.CASE_METHOD if names is None else names
        path.write_text("Test run for synthetic.dll (.NETCoreApp,Version=v10.0)\nThe following Tests are available:\n"
                        + "".join("    " + name + "\n" for name in chosen))
        return gate.verify_discovery(path)

    def verify(self, root, discovery=None):
        path = self.out / "synthetic.trx"
        ET.ElementTree(root).write(path, encoding="utf-8", xml_declaration=True)
        return gate.verify_trx(path, discovery or self.discovery())

    def reject(self, root):
        with self.assertRaises(RuntimeError):
            self.verify(root)
        self.assertTrue(json.loads((self.out / "trx-validation.json").read_text())["violations"])

    def test_expanded_discovery_is_exact_full_case_set(self):
        self.assertEqual((len(gate.EXPECTED), len(gate.CASE_METHOD)), (30, 37))
        report = self.discovery()
        self.assertEqual(report["mode"], "expanded_cases")
        self.assertEqual(report["observed_count"], 37)
        self.assertEqual(sum(count for method, count in gate.EXPECTED.items()
                             if method.startswith(("RyuPy.Runtime.Tests.PyTraceStateTests.",
                                                   "RyuPy.Runtime.Tests.PyTraceStateEpochTests."))), 21)

    def test_metadata_abi_fact_is_registered_and_cannot_be_omitted_or_misattributed(self):
        cls = "PyTraceStateV1ArtifactContractTests"
        method = "CombinedAbiRejectsInvalidAllowListsAndMetadataFlags"
        name = "RyuPy.Runtime.Tests." + cls + "." + method
        self.assertEqual(gate.PLAN[cls][method], gate.FACT)
        self.assertEqual(gate.EXPECTED[name], 1)
        self.assertEqual(gate.CASE_METHOD[name], name)
        # 两种 discovery 形态都必须保留这个完整身份，不能仅靠总数量通过。
        for collection in (gate.EXPECTED, gate.CASE_METHOD):
            names = list(collection)
            wrong = name.replace(cls, "PyTraceStateTests")
            for changed in ([x for x in names if x != name], names + [name],
                            [wrong if x == name else x for x in names]):
                with self.subTest(changed=changed), self.assertRaises(RuntimeError):
                    self.discovery(changed)
        root = synthetic_trx()
        missing = next(row for row in results(root) if row.get("testName") == name)
        root.find("t:Results", N).remove(missing)
        # 即使同步降低汇总，也不能把漏执行伪装为合法的旧36案例门。
        root.find("t:ResultSummary/t:Counters", N).attrib.update(total="36", executed="36", passed="36")
        self.reject(root)

    def test_method_only_discovery_does_not_claim_case_expansion(self):
        report = self.discovery(gate.EXPECTED)
        self.assertEqual(report["mode"], "methods_only")
        self.assertEqual(report["observed_count"], 30)
        self.assertEqual(len(self.verify(synthetic_trx(), report)), 37)

    def test_discovery_rejects_missing_extra_duplicate_wrong_owner_and_mixed_forms(self):
        names = list(gate.CASE_METHOD)
        theory = next(i for i, name in enumerate(names) if "(" in name)
        variants = [names[1:], names + [names[0]], names + ["RyuPy.Runtime.Tests.Unknown.Case"],
                    [names[0].replace("PyTraceStateTests", "OtherClass")] + names[1:]]
        changed = list(names); changed[theory] = changed[theory].split("(")[0]; variants.append(changed)
        changed = list(names); changed[theory] = changed[theory].replace("False", "Unknown"); variants.append(changed)
        for variant in variants:
            with self.subTest(variant=variant[:2]), self.assertRaises(RuntimeError):
                self.discovery(variant)
        path = self.out / "discover.stdout"; path.write_text("no list header\n")
        with self.assertRaises(RuntimeError):
            gate.verify_discovery(path)

    def test_theory_repeated_method_name_with_distinct_cases_is_valid(self):
        records = self.verify(synthetic_trx())
        name = "RyuPy.Runtime.Tests.PyTraceStateTests.FirstEmptyForwardIsLegalButNewSequenceCannotResubmitTheBoundary"
        theory = [row for row in records if row["method"] == name]
        self.assertEqual(len(theory), 4)
        self.assertEqual(len({row["name"] for row in theory}), 4)
        self.assertEqual(len({row["test_id"] for row in theory}), 4)

    def test_trx_rejects_wrong_class_even_when_short_method_and_count_match(self):
        root = synthetic_trx()
        units(root)[0].find("t:TestMethod", N).set("className", "RyuPy.Runtime.Tests.AnotherClass")
        self.reject(root)

    def test_trx_rejects_definition_case_and_method_mismatch(self):
        for mutation in ("case", "method", "missing_method", "two_methods"):
            root = synthetic_trx(); unit = units(root)[0]; method = unit.find("t:TestMethod", N)
            if mutation == "case": unit.set("name", units(root)[1].get("name"))
            if mutation == "method": method.set("name", "OtherMethod")
            if mutation == "missing_method": unit.remove(method)
            if mutation == "two_methods": unit.append(copy.deepcopy(method))
            with self.subTest(mutation=mutation): self.reject(root)

    def test_trx_rejects_empty_or_duplicate_definition_ids_without_overwrite(self):
        for identity in ("", " ", "duplicate"):
            root = synthetic_trx()
            units(root)[1].set("id", units(root)[0].get("id") if identity == "duplicate" else identity)
            self.reject(root)
        root = synthetic_trx(); root.find("t:TestDefinitions", N).append(copy.deepcopy(units(root)[0])); self.reject(root)

    def test_duplicate_theory_result_cannot_fill_another_case_count(self):
        root = synthetic_trx(); rows = results(root)
        indices = [i for i, row in enumerate(rows) if "ExplicitPermanentlyRevokesBirth" in row.get("testName")]
        self.assertEqual(len(indices), 2)
        # 保持37结果和短/完整方法计数，只有一个Theory案例被重复替代。
        parent = root.find("t:Results", N); parent.remove(rows[indices[1]])
        parent.insert(indices[1], copy.deepcopy(rows[indices[0]]))
        self.reject(root)

    def test_trx_rejects_missing_unknown_or_swapped_definition_mapping(self):
        for mutation in ("missing", "unknown", "swap", "case_name"):
            root = synthetic_trx(); row = results(root)[0]
            if mutation == "missing": root.find("t:Results", N).remove(row)
            if mutation == "unknown": row.set("testId", "unknown-test-id")
            if mutation == "swap": row.set("testId", results(root)[1].get("testId"))
            if mutation == "case_name": row.set("testName", results(root)[1].get("testName"))
            with self.subTest(mutation=mutation): self.reject(root)

    def test_trx_rejects_duplicate_empty_and_mismatched_execution_ids(self):
        for mutation in ("definition_duplicate", "definition_empty", "result_duplicate", "result_empty", "mismatch"):
            root = synthetic_trx(); first = units(root)[0].find("t:Execution", N); second = units(root)[1].find("t:Execution", N)
            if mutation == "definition_duplicate": second.set("id", first.get("id"))
            if mutation == "definition_empty": first.set("id", "")
            if mutation == "result_duplicate": results(root)[1].set("executionId", results(root)[0].get("executionId"))
            if mutation == "result_empty": results(root)[0].set("executionId", "")
            if mutation == "mismatch": results(root)[0].set("executionId", "unmatched-execution")
            with self.subTest(mutation=mutation): self.reject(root)

    def test_trx_rejects_failure_skip_and_counter_contradictions(self):
        for outcome in ("Failed", "NotExecuted", "Unknown"):
            root = synthetic_trx(); results(root)[0].set("outcome", outcome); self.reject(root)
        for key, value in (("total", "35"), ("executed", "35"), ("passed", "35"), ("failed", "1"), ("notExecuted", "1"), ("total", "invalid")):
            root = synthetic_trx(); root.find("t:ResultSummary/t:Counters", N).set(key, value); self.reject(root)
        root = synthetic_trx(); parent = root.find("t:ResultSummary", N); parent.append(copy.deepcopy(parent[0])); self.reject(root)

    def test_trx_must_match_this_run_discovery(self):
        report = self.discovery()
        report["names"][0] = "RyuPy.Runtime.Tests.Other.Case"
        with self.assertRaises(RuntimeError): self.verify(synthetic_trx(), report)


if __name__ == "__main__":
    unittest.main()
