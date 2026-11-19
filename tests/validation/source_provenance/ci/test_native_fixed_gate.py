"""Native限定位置门的合成正负控；不是合法PE，不执行fixture或编译器，不代表产品通过。"""
from copy import deepcopy
import contextlib
import hashlib
import io
import json
from pathlib import Path
import runpy
import tempfile
import unittest
from unittest.mock import patch

import test_fixed_gate as F
from bootstrap import pe_review
from native import verify_pe_review


def manifest_model():
    """独立列出测试输入，不从被检查的IL推算预期。"""
    modules, sources = [], []
    names = ['__main__', 'sp_pkg', 'sp_pkg.subject']
    logical = ['entry/entry.py', 'modules/sp_pkg/__init__.py', 'modules/sp_pkg/subject.py']
    owner = 'RyuPy.Generated.Module'
    for index, name in enumerate(names):
        legacy = index < 2
        modules.append({'id': index, 'name': name,
            'environment_reason': 'aot-not-migrated' if legacy else 'explicit-instance',
            'init': {'owner': owner, 'name': f'm{index}___module_main',
                     'signature': 'static void ()' if legacy else 'static void ([System.Runtime]System.Object)'},
            'getter': {'owner': owner, 'name': f'__mod_get_{index}',
                       'signature': 'static [System.Runtime]System.Object (string)'} if legacy else None,
            'setter': {'owner': owner, 'name': f'__mod_set_{index}',
                       'signature': 'static void ([RyuPy.Runtime]RyuPy.Runtime.PyModule, string, [System.Runtime]System.Object)'} if legacy else None})
        sources.append({'module_id': index, 'module_name': name, 'package': 'sp_pkg' if index else '',
            'logical_source': logical[index], 'compiled_text_sha256': F.TABLE['native_consumers']['modules'][index]['sha256']})
    return {'schema_version': 2, 'kind': 'ryupy-native-aot-artifact',
        'target': {'architecture': 'x64', 'native_format': 'ELF', 'rid': 'linux-x64'},
        'build_inputs': [{'role': 'compiled_managed_input', 'relative_path': 'managed-entry.dll',
                          'sha256': hashlib.sha256(F.MODEL_PE_BYTES).hexdigest(), 'size': len(F.MODEL_PE_BYTES)},
                         {'role': 'runtime_managed_input', 'relative_path': 'RyuPy.Runtime.dll', 'sha256': 'b' * 64}],
        'modules': modules, 'sources': sources,
        'module_locations': [{'module_id': index, 'module_name': names[index], 'kind': 'executable_relative',
            'relative_file': '_internal/resources/v1/' + logical[index]} for index in (1, 2)],
        'resources': [], 'scope': {'resources_included': False}}


class NativeFixedGateTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temporary = tempfile.TemporaryDirectory(prefix='ryupy-native-pe-model-')
        cls.previous_root, F.ROOT = F.ROOT, Path(cls.temporary.name)
        # 仅拷贝已固定的源码字节供SHA检查，绝不执行或补写产品输出。
        for module in F.TABLE['native_consumers']['modules']:
            path = F.ROOT / module['fixture']; path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes((F.HERE.parent / module['fixture']).read_bytes())

    @classmethod
    def tearDownClass(cls):
        F.ROOT = cls.previous_root
        cls.temporary.cleanup()

    def setUp(self):
        self.data, self.expectation, self.sources, self.signatures = F.model(native=True)
        self.manifest = manifest_model()

    def accepts(self):
        return F.G['check'](self.data, self.expectation, F.ROOT, 'new', native_manifest=self.manifest)

    def method(self, name):
        return next(m for m in self.data['methods'] if m['method']['name'] == name)['body']['instructions']

    def registration(self, unit):
        ins = self.method('__init_functions')
        calls = [i for i, item in enumerate(ins) if item.get('target', {}).get('name') in
                 ('RegisterSourceLocated', 'RegisterInstanceSourceLocated')]
        end = calls[unit]
        positions = [i for i in range(end) if ins[i]['opcode'] == '0x72'][-4:]
        return ins, positions[0], end

    def test_synthetic_native_shape_not_product(self):
        result = self.accepts()
        self.assertTrue(result['admitted'])
        self.assertFalse(result['product_accepted'])
        self.assertEqual(result['location_mode'], 'native_consumers_aot')
        self.assertEqual([m['location']['kind'] for m in result['modules']], [0, 2, 2])

    def test_requires_explicit_native_mode(self):
        with self.assertRaisesRegex(ValueError, 'JIT注册Kind1'):
            F.G['check'](self.data, self.expectation, F.ROOT, 'new')

    def test_rejects_wrong_case_role_or_expectation(self):
        self.data['case_id'] = 'td_dynamic'
        with self.assertRaises(ValueError): self.accepts()
        self.data['case_id'] = 'native_consumers'
        self.expectation['modules'][0]['optional'] = True
        with self.assertRaises(ValueError): self.accepts()
        with self.assertRaises(ValueError):
            F.G['check'](self.data, F.TABLE['native_consumers'], F.ROOT, 'old', native_manifest=self.manifest)

    def test_rejects_malformed_manifest_tables(self):
        baseline = deepcopy(self.manifest)
        for table in ('build_inputs', 'modules', 'sources', 'module_locations'):
            for malformed in (None, {}, [], baseline[table][:-1], baseline[table] + [baseline[table][0]],
                              [baseline[table][0]] * len(baseline[table]), ['not-a-row'] * len(baseline[table])):
                with self.subTest(table=table, malformed=malformed):
                    self.manifest = deepcopy(baseline); self.manifest[table] = malformed
                    with self.assertRaises(ValueError): self.accepts()

    def test_rejects_manifest_identity_kind_path_and_hash(self):
        baseline = deepcopy(self.manifest)
        mutations = [
            (('schema_version',), 1), (('kind',), 'other'), (('target', 'rid'), 'linux-arm64'),
            (('build_inputs', 0, 'sha256'), 'c' * 64), (('build_inputs', 0, 'relative_path'), 'other.dll'),
            (('build_inputs', 0, 'size'), 0), (('build_inputs', 0, 'size'), 18432),
            (('build_inputs', 0, 'size'), True),
            (('sources', 0, 'compiled_text_sha256'), 'c' * 64), (('sources', 1, 'module_id'), 2),
            (('sources', 1, 'module_id'), True), (('sources', 2, 'module_name'), 'sp_pkg'),
            (('sources', 2, 'logical_source'), 'modules/sp_pkg/../subject.py'),
            (('sources', 2, 'package'), ''), (('modules', 1, 'id'), 2), (('modules', 1, 'name'), '__main__'),
            (('modules', 2, 'environment_reason'), 'aot-not-migrated'),
            (('modules', 2, 'getter'), baseline['modules'][1]['getter']), (('modules', 0, 'setter'), None),
            (('module_locations', 0, 'module_id'), 0), (('module_locations', 0, 'module_name'), '__main__'),
            (('module_locations', 0, 'kind'), 'absolute'), (('module_locations', 1, 'relative_file'), ''),
            (('module_locations', 1, 'relative_file'), '/tmp/subject.py'),
            (('module_locations', 1, 'relative_file'), '_internal/resources/v1/modules/sp_pkg/../subject.py'),
            (('resources',), [{'path': 'source.py'}]), (('scope', 'resources_included'), True),
        ]
        for path, value in mutations:
            with self.subTest(path=path):
                self.manifest = deepcopy(baseline); target = self.manifest
                for key in path[:-1]: target = target[key]
                target[path[-1]] = value
                with self.assertRaises(ValueError): self.accepts()

    def test_rejects_actual_registration_kind_path_and_package(self):
        baseline = deepcopy(self.data)
        for unit, offset, value in [(0, 3, 1), (0, 4, 'entry.py'), (1, 3, 0), (2, 3, 1),
                                    (1, 4, ''), (2, 4, '/tmp/subject.py'),
                                    (2, 4, '_internal/resources/v1/modules/sp_pkg/../subject.py'), (1, 1, '')]:
            with self.subTest(unit=unit, offset=offset, value=value):
                self.data = deepcopy(baseline); ins, start, _ = self.registration(unit)
                if offset == 3: ins[start+offset]['operand_hex'] = value.to_bytes(8, 'little', signed=True).hex()
                else: ins[start+offset]['string'] = value
                with self.assertRaises(ValueError): self.accepts()

    def test_rejects_missing_or_extra_actual_registration(self):
        baseline = deepcopy(self.data)
        for extra in (False, True):
            self.data = deepcopy(baseline); ins, start, end = self.registration(2)
            if extra: ins[-1:-1] = deepcopy(ins[start:end+1])
            else: del ins[start:end+1]
            with self.assertRaises(ValueError): self.accepts()

    def test_native_still_checks_source_signature_getter_and_frame(self):
        baseline = deepcopy(self.data)
        for method, index, key, value in [
            ('__init_code_sources', 6, 'field_token', self.sources[1]),
            ('__mod_get_0', 4, 'field_token', self.sources[1]),
            ('m2___module_main', 1, 'opcode', '0x14'), ('m2_emit', 2, 'opcode', '0x2')]:
            self.data = deepcopy(baseline); self.method(method)[index][key] = value
            with self.assertRaises(ValueError): self.accepts()
        self.data = deepcopy(baseline); ins = self.method('__init_functions')
        at = next(i for i, item in enumerate(ins) if item.get('target', {}).get('name') == 'FunctionDefinitionWithSource')
        ins[at-1]['field_token'] = self.sources[0]
        with self.assertRaises(ValueError): self.accepts()

    def test_native_still_checks_actual_fixture_sha(self):
        path = F.ROOT / self.expectation['modules'][0]['fixture']; original = path.read_bytes()
        try:
            path.write_bytes(original + b'\n# changed\n')
            with self.assertRaisesRegex(ValueError, '固定fixture SHA'): self.accepts()
        finally:
            path.write_bytes(original)

    def wrapper(self, manifest_text=None, role='new', case='native_consumers'):
        """只替换ECMA解码为明确模型；其余read_pe→fixed_gate为实际检查器代码。"""
        path = F.ROOT / 'model-manifest.json'
        path.write_text(json.dumps(self.manifest) if manifest_text is None else manifest_text)
        pe = F.ROOT / 'synthetic-not-a-pe.dll'; pe.write_bytes(F.MODEL_PE_BYTES)
        actual_run = runpy.run_path
        def decode(target, **kwargs):
            if Path(target).name == 'read_pe_decoder.py':
                print(json.dumps(self.data)); raise SystemExit(0)
            return actual_run(target, **kwargs)
        output = io.StringIO()
        argv = [str(F.HERE/'read_pe.py'), str(pe), role, case, '--native-manifest', str(path)]
        with patch('sys.argv', argv), patch('runpy.run_path', side_effect=decode), contextlib.redirect_stdout(output):
            try: actual_run(str(F.HERE/'read_pe.py'), run_name='__main__')
            except SystemExit as error: status = error.code
        return status, json.loads(output.getvalue()), hashlib.sha256(path.read_bytes()).hexdigest()

    def test_native_evidence_transfer_to_final_execution_guard(self):
        status, parsed, digest = self.wrapper()
        self.assertEqual(status, 0)
        review = pe_review(parsed, 'new', 'native_consumers', self.data['sha256'],
                           location_mode='native_consumers_aot', expected_manifest_sha256=digest)
        self.assertEqual(review['native_manifest_sha256'], digest)
        published = {'managed_sha256': self.data['sha256'], 'manifest_sha256': digest}
        verify_pe_review(review, published)
        for key in ('location_mode', 'native_manifest_sha256', 'case_id', 'role', 'pe_sha256',
                    'admitted', 'unit_link_mapping_verified', 'frame_carriers_verified'):
            bad = dict(review); del bad[key]
            with self.subTest(key=key), self.assertRaises(ValueError): verify_pe_review(bad, published)
        for key, value in [('location_mode', 'jit_files'), ('native_manifest_sha256', 'f'*64), ('role', 'old')]:
            bad = dict(review); bad[key] = value
            with self.assertRaises(ValueError): verify_pe_review(bad, published)
        with self.assertRaises(ValueError):
            verify_pe_review(review, {**published, 'manifest_sha256': 'e'*64})
        for key in ('location_mode', 'native_manifest_sha256'):
            bad = dict(parsed); del bad[key]
            with self.assertRaises(RuntimeError):
                pe_review(bad, 'new', 'native_consumers', self.data['sha256'],
                          location_mode='native_consumers_aot', expected_manifest_sha256=digest)

    def test_wrapper_rejects_malformed_or_duplicate_json(self):
        for value in ('{', 'null', '[]', '{"schema_version":2,"schema_version":2}'):
            with self.subTest(value=value):
                status, parsed, _ = self.wrapper(value)
                self.assertEqual(status, 2)
                self.assertFalse(parsed['admitted'])

    def test_wrapper_rejects_native_flag_on_old_or_other_jit_case(self):
        for role, case in [('old', 'native_consumers'), ('new', 'td_dynamic'), ('old', 'td_dynamic')]:
            with self.subTest(role=role, case=case), self.assertRaisesRegex(ValueError, '只能显式'):
                self.wrapper(role=role, case=case)


if __name__ == '__main__':
    unittest.main()
