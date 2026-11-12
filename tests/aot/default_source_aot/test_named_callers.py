"""所有AOT具名Rust门的静态调用合同；拒短名/漏exact，避免CI逐个首错。"""
import ast
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]
EXPECTED = {
    'default_aot::tests::default_aot_real_graph_emit',
    'instance_class_aot_tests::instance_class_aot_real_source_and_pe_gate',
    'instance_pe_tests::instance_class_actual_pe_gate',
}


def audit_call(node):
    name = ast.literal_eval(node.args[2])
    argv = ast.literal_eval(node.args[1])
    if name not in EXPECTED or '::' not in name:
        raise ValueError('未知或不完整测试名')
    split = argv.index('--')
    if argv[:split].count(name) != 1 or argv[split + 1:].count('--exact') != 1:
        raise ValueError('测试过滤器与精确身份不一致')
    return name


class NamedCallerTests(unittest.TestCase):
    def test_all_aot_named_callers_are_exact(self):
        names = []
        for path in (ROOT / 'tests/aot').rglob('*.py'):
            tree = ast.parse(path.read_text())
            for node in ast.walk(tree):
                if isinstance(node, ast.Call) and isinstance(node.func, ast.Attribute) and node.func.attr == 'named_test':
                    names.append(audit_call(node))
        self.assertEqual(len(names), 5)
        self.assertEqual(set(names), EXPECTED)

    def test_short_missing_duplicate_and_different_filters_rejected(self):
        name = 'instance_pe_tests::instance_class_actual_pe_gate'
        cases = [(['cargo', name, '--'], name),
                 (['cargo', 'instance_class_actual_pe_gate', '--', '--exact'], name),
                 (['cargo', name, name, '--', '--exact'], name),
                 (['cargo', name, '--', '--exact', '--exact'], name),
                 (['cargo', name, '--', '--exact'], 'instance_class_actual_pe_gate')]
        for argv, expected in cases:
            node = ast.parse(f'r.named_test("gate", {argv!r}, {expected!r})').body[0].value
            with self.assertRaises(ValueError):
                audit_call(node)


if __name__ == '__main__':
    unittest.main()
