"""分域源映射与精确负例的纯文本/AST自测；不执行CP、CLI或生成程序集。"""
from copy import deepcopy
import json
from pathlib import Path
import subprocess
import unittest

import warning_split as split_gate

ROOT = Path(__file__).resolve().parents[1]
CONTRACT = json.loads((ROOT / 'contract.json').read_text())
SPLIT = json.loads((ROOT / 'warning-split.json').read_text())
SUBJECT = Path('/synthetic/fixtures/warning_names/subject.py')


def result(code=1, out=b'', err=b''):
    return subprocess.CompletedProcess(['synthetic-not-executed'], code, out, err)


class WarningSplitTests(unittest.TestCase):
    def arm(self, index):
        return deepcopy(CONTRACT['boundary_cases'][0]['arms'][index])

    def legacy_stderr(self):
        expected = SPLIT['boundaries']['legacy-rejected']
        return (expected['last_stage_prefix'] + '123\n' + expected['diagnostic'].replace('{SOURCE}', str(SUBJECT))).encode()

    def test_actual_frozen_sources_and_split_mapping(self):
        self.assertEqual(split_gate.load(ROOT, CONTRACT), SPLIT)
        for name, expected in SPLIT['expected_cp'].items():
            split_gate.check_cp(SPLIT, name, result(0, expected['stdout'].encode()))
        split_gate.check_cp(SPLIT, 'warning_names', result(0, SPLIT['original_cp_stdout'].encode()))

    def test_original_is_never_a_positive_or_dropped_boundary(self):
        bad = deepcopy(CONTRACT)
        bad['cases'].append(bad['boundary_cases'][0])
        with self.assertRaises(RuntimeError): split_gate.load(ROOT, bad)
        for arms in [[], [self.arm(0)], [self.arm(1)]]:
            bad = deepcopy(CONTRACT); bad['boundary_cases'][0]['arms'] = arms
            with self.assertRaises(RuntimeError): split_gate.load(ROOT, bad)

    def test_wrong_execution_modes_are_rejected(self):
        for name in ['warning_names_legacy', 'warning_name_missing']:
            bad = deepcopy(CONTRACT)
            case = next(c for c in bad['cases'] if c['id'] == name)
            case['instance_modules'] = ['__main__']
            with self.assertRaises(RuntimeError): split_gate.load(ROOT, bad)
        bad = deepcopy(CONTRACT); bad['boundary_cases'][0]['arms'][1]['instance_modules'] = []
        with self.assertRaises(RuntimeError): split_gate.load(ROOT, bad)

    def test_exact_build_boundary_with_only_known_path_and_timing_projection(self):
        value = split_gate.rejection(SPLIT, self.arm(0), result(err=self.legacy_stderr()), SUBJECT)
        self.assertEqual(value['phase'], 'build')
        self.assertFalse(value['combined_input_supported'])
        self.assertFalse(value['positive_compatibility'])

    def test_compile_wrong_diagnostic_source_stage_extra_output_are_rejected(self):
        stderr = self.legacy_stderr()
        for wrong in [stderr.replace(b'E0100', b'E0199'),
                      stderr.replace('__name__'.encode(), '__doc__'.encode()),
                      stderr.replace(b'subject.py:1:1', b'subject.py:40:1'),
                      stderr.replace(b'module="subject"', b'module="__main__"'),
                      stderr.replace(b'event=begin', b'event=end'),
                      stderr + b'other error\n', b'noise\n' + stderr,
                      stderr.replace(b'elapsed_us=123', b'elapsed_us=unknown')]:
            with self.subTest(stderr=wrong), self.assertRaises(RuntimeError):
                split_gate.rejection(SPLIT, self.arm(0), result(err=wrong), SUBJECT)

    def test_exact_instance_boundary(self):
        expected = SPLIT['boundaries']['instance-rejected']['stderr'].encode()
        value = split_gate.rejection(SPLIT, self.arm(1), result(err=expected), SUBJECT)
        self.assertEqual(value['phase'], 'jit')
        for wrong in [b'', expected + b'\n', expected.replace(b'NotImplementedError', b'TypeError'),
                      b'NotImplementedError: unrelated\n', b'Unhandled exception\n' + expected]:
            with self.subTest(stderr=wrong), self.assertRaises(RuntimeError):
                split_gate.rejection(SPLIT, self.arm(1), result(err=wrong), SUBJECT)

    def test_wrong_exit_or_nonempty_stdout_cannot_pass_either_boundary(self):
        for i, stderr in [(0, self.legacy_stderr()), (1, SPLIT['boundaries']['instance-rejected']['stderr'].encode())]:
            for code in [0, 2, 124, 137, -9, -11, None]:
                with self.subTest(arm=i, code=code), self.assertRaises(RuntimeError):
                    split_gate.rejection(SPLIT, self.arm(i), result(code, err=stderr), SUBJECT)
            with self.assertRaises(RuntimeError):
                split_gate.rejection(SPLIT, self.arm(i), result(out=b'WN done\n', err=stderr), SUBJECT)

    def test_changed_cp_observation_is_not_new_oracle(self):
        for name, expected in SPLIT['expected_cp'].items():
            for actual in [result(1, expected['stdout'].encode()), result(0, b'WN done\n'),
                           result(0, expected['stdout'].encode(), b'warning\n')]:
                with self.subTest(name=name), self.assertRaises(RuntimeError):
                    split_gate.check_cp(SPLIT, name, actual)

    def test_fixed_pe_table_is_bound_to_new_source_ast_and_modes(self):
        import ast
        import hashlib
        table = json.loads((ROOT / 'ci/fixed_expectations.json').read_text())
        self.assertEqual(table['warning_split_manifest_sha256'], split_gate.sha(ROOT / 'warning-split.json'))
        for name in SPLIT['expected_cp']:
            case = next(c for c in CONTRACT['cases'] if c['id'] == name)
            for module in table['cases'][name]['modules']:
                path = ROOT / module['fixture']; tree = ast.parse(path.read_text())
                self.assertEqual(module['sha256'], hashlib.sha256(path.read_bytes()).hexdigest())
                self.assertEqual(module['environment'], 'InstanceGlobals' if module['name'] in case['instance_modules'] else 'LegacySingleton')
                definitions = []
                def walk(nodes, prefix=''):
                    for node in nodes:
                        if isinstance(node, ast.ClassDef): walk(node.body, prefix + node.name + '.')
                        if isinstance(node, ast.FunctionDef):
                            definitions.append((prefix + node.name, node.lineno, node.body[0].lineno,
                                [n.lineno for n in node.body if not isinstance(n, (ast.Global, ast.Nonlocal))]))
                walk(tree.body)
                self.assertEqual(definitions, [(d['qualname'], d['firstline'], d['body_firstline'], d['body_lines']) for d in module['definitions']])

    def test_future_budget_separates_positive_boundary_host_and_native(self):
        self.assertEqual(len(CONTRACT['cases']), 11)
        self.assertEqual(sum(len(c['arms']) for c in CONTRACT['cases']), 13)
        self.assertEqual(len(split_gate.cases(CONTRACT)) + 1, CONTRACT['budget']['cp_target'])
        arms = [a for c in split_gate.cases(CONTRACT) for a in c['arms']]
        self.assertEqual(len(arms) + 1, CONTRACT['budget']['new_pe_build'])
        self.assertEqual(sum(a.get('reject_phase') != 'build' for a in arms) + 1, CONTRACT['budget']['new_jit'])


if __name__ == '__main__':
    unittest.main()
