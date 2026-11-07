"""schema/完整搬迁轻测；伪ELF头仅格式单测，绝不执行或冒真实ILC。"""
import copy
import hashlib
import json
from pathlib import Path
import struct
import tempfile
import unittest
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


if __name__=='__main__':
    unittest.main()
