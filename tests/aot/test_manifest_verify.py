"""schema/完整搬迁轻测；伪ELF头仅格式单测，绝不执行或冒真实ILC。"""
import copy
import hashlib
import json
from pathlib import Path
import struct
import tempfile
import unittest
from unittest.mock import patch
import manifest_verify as gate
from manifest_verify import ManifestError, MANIFEST_NAME, verify_manifest, relocate_manifest, read_sdk_version


def manifest_fixture(root):
    root.mkdir()
    header = bytearray(64)
    header[:7] = b'\x7fELF\x02\x01\x01'
    struct.pack_into('<HHI', header, 16, 3, 62, 1)
    struct.pack_into('<H', header, 52, 64)
    (root/'program').write_bytes(header)
    (root/'symbols').mkdir()
    (root/'symbols/program.dbg').write_bytes(b'debug symbols')
    method = {'owner':'Generated', 'name':'Main', 'signature':'void ()'}
    digest = '0'*64
    def artifact(name, role):
        data = (root/name).read_bytes()
        return {'relative_path':name, 'role':role, 'size':len(data), 'sha256':hashlib.sha256(data).hexdigest()}
    data = {'schema_version':1, 'kind':'ryupy-native-aot-artifact',
      'compiler':{'package_version':'0.0.1','build_revision':None,'codegen_report_version':1,
        'options':{'experimental_direct_accumulator':False, 'experimental_foreach_accumulator':False,
            'experimental_leaf_inline':False, 'diagnostic_scalar_slp':False, 'experimental_packed_slp':False,
            'instance_modules':[], 'module_environment_policy':'explicit-only'}},
      'target':{'rid':'linux-x64','native_format':'ELF','architecture':'x64'},
      'toolchain':{'sdk_version':'10.0.403','target_framework_major':10},
      'entry':{'module_id':0,'managed_entry':method},
      'sources':[{'module_id':0,'module_name':'__main__','package':'','logical_source':'entry/main.py','compiled_text_sha256':digest}],
      'modules':[{'id':0,'name':'__main__','init':method,'getter':method,'setter':method,'exports':['run'],'environment_reason':'existing-contract'}],
      'emitted_delegate_references':[{'site':{'source_method':method,'instruction':2},'target':method,
        'target_display':'Generated.Main','role':'module_init','module_id':0,'function_id':None,'class_id':None}],
      'build_inputs':[{'relative_path':name,'role':role,'size':1,'sha256':digest} for name,role in
        [('managed-entry.dll','compiled_managed_input'),('RyuPy.Runtime.dll','runtime_managed_input')]],
      'artifacts':[artifact('program','native_executable'),artifact('symbols/program.dbg','unclassified_sidecar')],
      'scope':dict.fromkeys(['dynamic_dependency_closure_complete','resources_included','native_retention_proven','manifest_self_hash_included'],False)}
    (root/MANIFEST_NAME).write_text(json.dumps(data))
    return data


def instance_manifest_fixture(root):
    """普通subject/consumer合成图，非任何业务fixture或实际ILC产物。"""
    data = manifest_fixture(root)
    data['schema_version'] = data['compiler']['codegen_report_version'] = 2
    data['compiler']['options']['instance_modules'] = ['subject']
    data['scope'].update(declared_resources_complete=True)
    data['resources'], data['module_locations'] = [], []
    for number, name in ((1, 'subject'), (2, 'consumer')):
        source = {**data['sources'][0], 'module_id': number, 'module_name': name,
                  'logical_source': 'modules/' + name + '.py'}
        data['sources'].append(source)
        module = {**data['modules'][0], 'id': number, 'name': name}
        if name == 'subject': module.update(getter=None, setter=None, exports=[], environment_reason='explicit-instance')
        data['modules'].append(module)
        data['module_locations'].append(dict(module_id=number, module_name=name,
            kind='executable_relative', relative_file='_internal/resources/v1/' + source['logical_source']))
    (root / MANIFEST_NAME).write_text(json.dumps(data))
    return data


class ManifestVerifyTests(unittest.TestCase):
    def test_report_versions_keep_v1_and_v2_and_reject_unknown(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / 'publish'
            data = manifest_fixture(root)
            for version in (1, 2):
                data['compiler']['codegen_report_version'] = version
                (root / MANIFEST_NAME).write_text(json.dumps(data))
                verify_manifest(root, root / 'program', 'linux-x64', '10.0.403')
            for version in (0, 3, True, '2'):
                data['compiler']['codegen_report_version'] = version
                (root / MANIFEST_NAME).write_text(json.dumps(data))
                with self.assertRaises(ManifestError):
                    verify_manifest(root, root / 'program', 'linux-x64', '10.0.403')

    def test_complete_relocation_keeps_symbols_and_manifest(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)/'publish'
            data = manifest_fixture(root)
            self.assertEqual(verify_manifest(root,root/'program','linux-x64','10.0.403'),data)
            target, relocated = relocate_manifest(root,root/'program',Path(directory)/'moved','linux-x64','10.0.403')
            self.assertEqual(relocated,data)
            self.assertTrue((target.parent/'symbols/program.dbg').is_file())
            self.assertEqual((target.parent/MANIFEST_NAME).read_bytes(),(root/MANIFEST_NAME).read_bytes())

    def test_same_logical_source_can_have_distinct_module_identities(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory)/'publish';data=manifest_fixture(root)
            for number,name in [(1,'pkg'),(2,'pkg.__init__')]:
                data['sources'].append({**data['sources'][0], 'module_id':number,
                    'module_name':name, 'package':'pkg', 'logical_source':'modules/pkg/__init__.py'})
                data['modules'].append({**data['modules'][0], 'id':number, 'name':name})
            (root/MANIFEST_NAME).write_text(json.dumps(data))
            self.assertEqual(len(verify_manifest(root,root/'program','linux-x64','10.0.403')['modules']),3)

    def test_target_sdk_native_and_schema_contract_rejections(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory)/'publish'; original=manifest_fixture(root)
            for rid,sdk,native in [('osx-arm64','10.0.403','program'),('linux-x64','10.0.402','program'),('linux-x64','10.0.403','symbols/program.dbg')]:
                with self.subTest(rid=rid,sdk=sdk,native=native), self.assertRaises(ManifestError):
                    verify_manifest(root,root/native,rid,sdk)
            broken = bytearray((root/'program').read_bytes())
            struct.pack_into('<H', broken, 52, 0)
            (root/'program').write_bytes(broken)
            malformed = copy.deepcopy(original)
            malformed['artifacts'][0]['sha256'] = hashlib.sha256(broken).hexdigest()
            (root/MANIFEST_NAME).write_text(json.dumps(malformed))
            with self.assertRaises(ManifestError):
                verify_manifest(root,root/'program','linux-x64','10.0.403')
            struct.pack_into('<H', broken, 52, 64)
            (root/'program').write_bytes(broken)
            mutations=[lambda d:d.update(private_source_path='/Users/private/project'),
              lambda d:d['sources'][0].update(logical_source='C:/private/main.py'),
              lambda d:d['sources'][0].update(compiled_text_sha256='oops'),
              lambda d:d['modules'][0].update(name='wrong'),
              lambda d:d['scope'].update(native_retention_proven=True),
              lambda d:d['artifacts'][1].update(role='native_executable'),
              lambda d:d['emitted_delegate_references'][0].update(module_id=9)]
            for mutate in mutations:
                data=copy.deepcopy(original);mutate(data);(root/MANIFEST_NAME).write_text(json.dumps(data))
                with self.assertRaises(ManifestError):
                    verify_manifest(root,root/'program','linux-x64','10.0.403')

    def test_json_duplicate_keys_and_sdk_record_are_not_silently_accepted(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory)/'publish';manifest_fixture(root)
            path=root/MANIFEST_NAME
            path.write_text('{"schema_version":1,"schema_version":1}')
            with self.assertRaises(ManifestError):
                verify_manifest(root,root/'program','linux-x64','10.0.403')
            record=Path(directory)/'sdk.version'
            for value in ('10.0.403','10.0.403\n','\ufeff10.0.403\r\n','10.0.100-preview.1\n'):
                # 固定输入字节，不让Windows文本写入把显式CRLF二次翻译。
                record.write_bytes(value.encode('utf-8'))
                self.assertEqual(read_sdk_version(record),value.lstrip('\ufeff').rstrip('\r\n'))
            for value in ('', '10.0.403\nextra', ' 10.0.403', '10.0.403\n\n'):
                record.write_bytes(value.encode('utf-8'))
                with self.assertRaises(ManifestError):read_sdk_version(record)

    def test_none_preserves_legacy_options_but_explicit_tuple_is_strict(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / 'publish'; data = manifest_fixture(root)
            gate.validate_schema(data, 'linux-x64', '10.0.403', expected_instance_modules=())
            data['compiler']['options']['instance_modules'] = ['pkg.mod', 'pkg.mod']
            gate.validate_schema(data, 'linux-x64', '10.0.403')
            gate.validate_schema(data, 'linux-x64', '10.0.403', expected_instance_modules=None)
            for selected in ((), ('pkg.mod',), ('pkg.mod', 'pkg.mod')):
                with self.assertRaises(ManifestError):
                    gate.validate_schema(data, 'linux-x64', '10.0.403', expected_instance_modules=selected)

    def test_explicit_instance_original_and_relocation_use_identical_expectation(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / 'publish'; data = instance_manifest_fixture(root)
            with self.assertRaises(ManifestError): verify_manifest(root, root/'program', 'linux-x64', '10.0.403')
            with self.assertRaises(ManifestError):
                relocate_manifest(root, root/'program', Path(directory)/'default-rejected', 'linux-x64', '10.0.403')
            self.assertFalse((Path(directory)/'default-rejected').exists())
            selected = ('subject',)
            self.assertEqual(verify_manifest(root, root/'program', 'linux-x64', '10.0.403', expected_instance_modules=selected), data)
            with patch.object(gate, 'verify_manifest', wraps=gate.verify_manifest) as verify:
                binary, moved = relocate_manifest(root, root/'program', Path(directory)/'moved', 'linux-x64',
                    '10.0.403', expected_instance_modules=selected)
            self.assertEqual([call.kwargs for call in verify.call_args_list], [{'expected_instance_modules': selected}] * 2)
            self.assertEqual(moved, data)
            self.assertEqual((binary.parent/MANIFEST_NAME).read_bytes(), (root/MANIFEST_NAME).read_bytes())
            with self.assertRaises(ManifestError): verify_manifest(binary.parent, binary, 'linux-x64', '10.0.403')

    def test_explicit_instance_rejects_bad_expectations_shapes_and_identities(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / 'publish'; original = instance_manifest_fixture(root)
            for selected in ([], ['subject'], 'subject', False, ('',), ('bad..name',), (1,), ('subject', 'subject'), (), ('other',)):
                with self.subTest(selected=selected), self.assertRaises(ManifestError):
                    gate.validate_schema(original, 'linux-x64', '10.0.403', expected_instance_modules=selected)
            method = original['modules'][0]['init']
            mutations = [lambda d: d['compiler'].update(codegen_report_version=1),
                lambda d: d['compiler']['options'].update(instance_modules=[]),
                lambda d: d['compiler']['options'].update(instance_modules=['subject', 'subject']),
                lambda d: d['modules'][1].update(init=None),
                lambda d: d['modules'][1].update(getter=method), lambda d: d['modules'][1].update(setter=method),
                lambda d: d['modules'][1].update(getter=method, setter=method),
                lambda d: d['modules'][1].update(exports=['hidden']),
                lambda d: d['modules'][1].update(environment_reason='policy-disabled'),
                lambda d: d['modules'][2].update(getter=None), lambda d: d['modules'][2].update(setter=None),
                lambda d: d['modules'][2].update(environment_reason='explicit-instance'),
                lambda d: d['entry'].update(module_id=1)]
            def schema1(data):
                data.update(schema_version=1); data.pop('resources'); data.pop('module_locations')
                data['scope'].pop('declared_resources_complete')
            mutations.append(schema1)
            for mutate in mutations:
                changed = copy.deepcopy(original); mutate(changed)
                with self.subTest(mutation=mutate), self.assertRaises(ManifestError):
                    gate.validate_schema(changed, 'linux-x64', '10.0.403', expected_instance_modules=('subject',))
            # 即使options与预期一致，也不能以缺失模块或错误顺序借用null权利。
            data = copy.deepcopy(original); data['compiler']['options']['instance_modules'] = ['missing']
            data['modules'][1] = {**data['modules'][0], 'id': 1, 'name': 'subject'}
            with self.assertRaises(ManifestError): gate.validate_schema(data, 'linux-x64', '10.0.403', expected_instance_modules=('missing',))
            data = copy.deepcopy(original); data['compiler']['options']['instance_modules'] = ['consumer', 'subject']
            with self.assertRaises(ManifestError): gate.validate_schema(data, 'linux-x64', '10.0.403', expected_instance_modules=('subject', 'consumer'))

    def test_relocation_rechecks_instance_shape_and_bytes_after_copy(self):
        for mutation in ('half-null', 'artifact'):
            with self.subTest(mutation=mutation), tempfile.TemporaryDirectory() as directory:
                root = Path(directory) / 'publish'; instance_manifest_fixture(root)
                copy_file = gate.shutil.copy2
                def copy_and_change(source, target):
                    result = copy_file(source, target)
                    if Path(source).name == MANIFEST_NAME:
                        if mutation == 'half-null':
                            data = json.loads(Path(target).read_text())
                            data['modules'][1]['setter'] = data['modules'][0]['setter']
                            Path(target).write_text(json.dumps(data))
                        else: (Path(target).parent/'program').write_bytes(b'changed')
                    return result
                with patch.object(gate.shutil, 'copy2', side_effect=copy_and_change), self.assertRaises(ManifestError):
                    relocate_manifest(root, root/'program', Path(directory)/'moved', 'linux-x64',
                        '10.0.403', expected_instance_modules=('subject',))


if __name__=='__main__':
    unittest.main()
