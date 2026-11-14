"""只用合成结果/TRX/图测试判定边界；不启动CP、CLI、dotnet或目标进程。"""
import json
import re
from pathlib import Path
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET

import gate


class SourceImportGateTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)

    def test_original_refusal_rejects_crash_success_and_wrong_error(self):
        case = json.loads((gate.HERE / 'fixtures.json').read_text())['cases'][0]
        stdout = case['expected_stdout'].encode()
        stderr = (case['expected_terminal_error'] + '\n').encode()
        gate.verify_result(subprocess.CompletedProcess([], 1, stdout, stderr), case)
        for code, out, err in [(-6, stdout, stderr), (134, stdout, stderr), (0, stdout, stderr),
                               (1, stdout + b'P:files-returned\n', stderr),
                               (1, stdout, b'NotImplementedError: unsupported\n')]:
            with self.assertRaises(RuntimeError):
                gate.verify_result(subprocess.CompletedProcess([], code, out, err), case)

    def test_positive_compares_all_three_streams(self):
        case = {'name': 'positive', 'expected_exit': 0, 'expected_stdout': 'ok\n'}
        gate.verify_result(subprocess.CompletedProcess([], 0, b'ok\n', b''), case)
        for code, out, err in [(1, b'ok\n', b''), (0, b'skipped\n', b''), (0, b'ok\n', b'warning')]:
            with self.assertRaises(RuntimeError):
                gate.verify_result(subprocess.CompletedProcess([], code, out, err), case)

    def test_caller_expands_only_expected_source_root(self):
        case = json.loads((gate.HERE / 'fixtures.json').read_text())['cases'][-1]
        expected = case['expected_stdout'].replace('{SOURCE_ROOT}', str(gate.ROOT / case['stdout_source_root'])).encode()
        gate.verify_result(subprocess.CompletedProcess([], 0, expected, b''), case)
        with self.assertRaises(RuntimeError):
            gate.verify_result(subprocess.CompletedProcess([], 0, case['expected_stdout'].encode(), b''), case)

    def test_graph_requires_real_explicit_child_and_mode(self):
        path = self.root / 'graph.json'
        data = {'compiled_feature': True, 'session_policy': 'ordinary-dynamic-module-pilot',
                'modules': [{'module': name, 'compiled': True, 'execution': 'LegacySingleton'}
                            for name in ('__main__', 'pkg', 'pkg.data')]}
        path.write_text(json.dumps(data)); gate.verify_graph(path, ['__main__', 'pkg', 'pkg.data'])
        with self.assertRaises(RuntimeError): gate.verify_graph(path, ['__main__', 'pkg'])
        data['modules'][-1]['execution'] = 'InstanceGlobals'; path.write_text(json.dumps(data))
        with self.assertRaises(RuntimeError): gate.verify_graph(path, ['__main__', 'pkg', 'pkg.data'])

    def trx(self, *, remove=False, remove_old=False, extra=None, duplicate=None, empty=False, outcome='Passed'):
        namespace = '{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}'
        root = ET.Element(namespace + 'TestRun')
        results = ET.SubElement(root, namespace + 'Results')
        names = [f'RyuPy.Runtime.Tests.PySourceImportPathTests.{name}(row: {i})'
                 for name, count in gate.NEW_TESTS.items() for i in range(count)]
        if remove: names.pop()
        names += [f'RyuPy.Runtime.Tests.{name}(row: {i})'
                  for name, count in gate.OLD_TESTS.items() for i in range(count)]
        if remove_old: names.pop()
        if extra == 'new': names.append('RyuPy.Runtime.Tests.PySourceImportUnexpectedTests.Unexpected')
        if extra == 'old': names.append('RyuPy.Runtime.Tests.PyModuleCacheAliasTests.Unexpected')
        if duplicate == 'new': names.append(names[0])
        if duplicate == 'old': names.append(names[-1])
        if empty: names.clear()
        for name in names:
            ET.SubElement(results, namespace + 'UnitTestResult', testName=name, outcome=outcome)
        path = self.root / 'tests.trx'; ET.ElementTree(root).write(path); return path

    def test_trx_requires_new44_old_methods_and_zero_skips(self):
        self.assertEqual(gate.verify_tests(self.trx())['new'], 44)
        self.assertEqual(gate.verify_tests(self.trx())['old'], 84)
        self.assertEqual(gate.verify_tests(self.trx())['total'], 128)
        for kwargs in ({'remove': True}, {'remove_old': True}, {'extra': 'new'}, {'extra': 'old'},
                       {'duplicate': 'new'}, {'duplicate': 'old'}, {'empty': True}):
            with self.assertRaises(RuntimeError): gate.verify_tests(self.trx(**kwargs))
        for outcome in ('Failed', 'NotExecuted'):
            with self.assertRaises(RuntimeError): gate.verify_tests(self.trx(outcome=outcome))

    def test_old_inventory_includes_every_partial_source_file(self):
        # 按类声明跨全部源文件收集，不能再把类名当成唯一文件名；当前仅Fact/InlineData域。
        inventory = {}
        found_classes = set()
        source = gate.ROOT / 'runtime/RyuPy.Runtime.Tests'
        declarations = r'public\s+(?:partial\s+)?class\s+(' + '|'.join(gate.OLD_CLASSES) + r')\b'
        method = r'((?:\s*\[(?:Fact|Theory|InlineData)[^\n]*\]\s*)+)public void (\w+)'
        for path in source.rglob('*.cs'):
            text = path.read_text(encoding='utf-8-sig')
            owners = set(re.findall(declarations, text))
            if not owners: continue
            self.assertEqual(len(owners), 1, str(path))
            owner = next(iter(owners)); found_classes.add(owner)
            self.assertNotRegex(text, r'\[(?:MemberData|ClassData)\b', str(path))
            methods = re.findall(method, text)
            # 新增不在本简单扫描域的测试声明时显式失败，不静默漏掉async/数据源等方法。
            self.assertEqual(len(methods), len(re.findall(r'\[(?:Fact|Theory)(?:\([^\]\n]*\))?\]', text)), str(path))
            for attrs, name in methods:
                key = owner + '.' + name
                self.assertNotIn(key, inventory)
                inventory[key] = attrs.count('[InlineData(') or 1
        self.assertEqual(found_classes, set(gate.OLD_CLASSES))
        self.assertEqual(len(inventory), 61)
        self.assertEqual(sum(inventory.values()), 84)
        self.assertEqual(inventory, gate.OLD_TESTS)


if __name__ == '__main__':
    unittest.main()
