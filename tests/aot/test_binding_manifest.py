"""恢复版纯工具测试：合成文件不冒真实C/ILC产物。"""
import copy
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
from types import SimpleNamespace
from binding_manifest import validate_schema, verify, relocate
from manifest_files import ManifestError, plain_stat


def digest(raw): return {'size':len(raw),'sha256':hashlib.sha256(raw).hexdigest()}
def item(name,role,raw): return {'relative_path':name,'role':role,**digest(raw)}
def method(name,signature='static void ()'): return {'owner':'Generated.Module','name':name,'signature':signature}


def sample(root):
    files = {'app.dll':b'managed bytes','RyuPy.Runtime.dll':b'runtime bytes',
        'app.runtimeconfig.json':b'{}','_internal/native/v1/b0/library.so':b'own shared bytes'}
    roles = ['managed_entry','runtime_managed','runtimeconfig','native_shared']
    artifacts = [item(n,r,v) for (n,v),r in zip(files.items(),roles)]
    for name,raw in files.items():
        path = root/name; path.parent.mkdir(parents=True,exist_ok=True); path.write_bytes(raw)
    body = method('Body','static object (object[])')
    b = {'binding_id':'b0','module_id':1,'module':'declared_demo','export':'add',
        'signature':'cdecl_i32_i32_i64','library':'ryupy_cabi_probe','symbol':'ryupy_demo_add_i32',
        'declaration_sha256':'a'*64,'linkage':'jit_shared','relative_file':'_internal/native/v1/b0/library.so',
        'initializer_id':4,'export_id':3,'body':body,'native':method('Native','static long (int, int)'),
        'pinvoke':{'calling_convention':'Cdecl','character_set':'NotSpecified','no_mangle':True,
            'supports_last_error':False,'preserve_sig':True,'is_static':True,'has_body':False}}
    modules = [{'id':i,'name':name,'init':method('Init'+str(i)),'getter':method('Get'+str(i)),
        'setter':method('Set'+str(i)),'exports':exports,'origin':origin,'environment_reason':'existing-contract'}
        for i,name,exports,origin in [(0,'__main__',[],{'kind':'compiled_source','source_module_id':0}),
        (1,'declared_demo',['add'],{'kind':'declared_native_binding','binding_id':'b0','declaration_sha256':'a'*64})]]
    data = {'schema_version':3,'kind':'ryupy-managed-binding-artifact',
        'compiler':{'package_version':'0.1.0','build_revision':None,'codegen_report_version':1,
            'options':{'experimental_direct_accumulator':False, 'experimental_foreach_accumulator':False,
            'experimental_leaf_inline':False, 'diagnostic_scalar_slp':False, 'experimental_packed_slp':False,
            'instance_modules':[], 'module_environment_policy':'explicit-only'}},
        'target':{'rid':'linux-x64','native_format':'ELF','architecture':'x64'},
        'toolchain':{'sdk_version':None,'target_framework_major':10},
        'entry':{'module_id':0,'managed_entry':method('Main','static int (string[])')},
        'sources':[{'module_id':0,'module_name':'__main__','package':'','logical_source':'entry/app.py','compiled_text_sha256':'b'*64}],
        'modules':modules,'native_bindings':[b], 'emitted_delegate_references':[
            {'site':{'source_method':method('InitFunctions'),'instruction':7},'target':body,'target_display':'Body',
             'role':'declared_native_body','module_id':1,'function_id':None,'class_id':None}],
        'build_inputs':[item('managed-entry.dll','compiled_managed_input',files['app.dll']),
            item('RyuPy.Runtime.dll','runtime_managed_input',files['RyuPy.Runtime.dll']),
            item('selected-native-input','native_shared',files['_internal/native/v1/b0/library.so'])],
        'artifacts':artifacts,'scope':dict.fromkeys(('dynamic_dependency_closure_complete','resources_included','native_retention_proven','manifest_self_hash_included'),False)}
    expected = {k:b[k] for k in ('module','export','library','symbol','declaration_sha256')}
    expected.update(native_input=digest(files['_internal/native/v1/b0/library.so']),sources={'__main__':'b'*64})
    return data,expected


class BindingManifestTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.root = Path(self.temp.name)/'bundle'; self.root.mkdir()
        self.data,self.expected = sample(self.root); self.write()
    def tearDown(self): self.temp.cleanup()
    def write(self): (self.root/'ryupy-artifact.json').write_text(json.dumps(self.data),encoding='utf-8')
    def verify(self): return verify(self.root,self.root/'app.dll','linux-x64','jit_shared',None,self.expected)

    def test_report_versions_keep_v1_and_v2_and_reject_unknown(self):
        for version in (1, 2):
            self.data['compiler']['codegen_report_version'] = version
            self.write()
            self.verify()
        for version in (0, 3, True, '2'):
            self.data['compiler']['codegen_report_version'] = version
            self.write()
            with self.assertRaises(ManifestError):
                self.verify()

    def test_valid_exact_bundle_relocates_all_files(self):
        self.verify()
        entry,data = relocate(self.root,self.root/'app.dll',Path(self.temp.name)/'moved','linux-x64','jit_shared',None,self.expected)
        self.assertEqual(entry.read_bytes(),b'managed bytes'); self.assertEqual(data,self.data)
        self.assertTrue((entry.parent/'_internal/native/v1/b0/library.so').is_file())

    def test_tamper_missing_extra_are_not_valid_deployments(self):
        lib = self.root/'_internal/native/v1/b0/library.so'; original = lib.read_bytes()
        lib.write_bytes(b'tamper')
        with self.assertRaises(ManifestError): self.verify()
        lib.unlink()
        with self.assertRaises(ManifestError): self.verify()
        lib.write_bytes(original); (self.root/'extra').write_bytes(b'x')
        with self.assertRaises(ManifestError): self.verify()

    def test_generated_origin_and_delegate_reference_are_required(self):
        for mutate in [lambda d:d['modules'][1].update(origin={'kind':'compiled_source','source_module_id':1}),
                       lambda d:d['sources'].append(copy.deepcopy(d['sources'][0])),
                       lambda d:d['modules'][0]['origin'].update(source_module_id=False),
                       lambda d:d.update(emitted_delegate_references=[]),
                       lambda d:d['emitted_delegate_references'][0].update(module_id=0)]:
            data = copy.deepcopy(self.data); mutate(data)
            with self.assertRaises(ManifestError): validate_schema(data,'linux-x64','jit_shared',None)

    def test_exact_signatures_flags_and_bool_types(self):
        for key,value in [('calling_convention','Stdcall'),('calling_convention','cdecl'),('no_mangle',1),('has_body',True),('character_set','Unicode')]:
            data = copy.deepcopy(self.data); data['native_bindings'][0]['pinvoke'][key] = value
            with self.assertRaises(ManifestError): validate_schema(data,'linux-x64','jit_shared',None)
        self.data['native_bindings'][0]['native']['signature'] = 'static int (int, int)'; self.write()
        with self.assertRaises(ManifestError): self.verify()

    def test_expected_is_independent_of_manifest_claim(self):
        for key in ('module','symbol','declaration_sha256'):
            expected = copy.deepcopy(self.expected); expected[key] = 'other'
            with self.assertRaises(ManifestError): verify(self.root,self.root/'app.dll','linux-x64','jit_shared',None,expected)
        self.expected['native_input']['sha256'] = 'c'*64
        with self.assertRaises(ManifestError): self.verify()
        self.expected['native_input'] = digest(b'own shared bytes'); self.expected['sources'] = {'__main__':'d'*64}
        with self.assertRaises(ManifestError): self.verify()

    def test_sdk_scope_and_paths_do_not_silently_widen(self):
        for mutate in [lambda d:d['scope'].update(resources_included=True),
                       lambda d:d['toolchain'].update(sdk_version='10.0.403'),
                       lambda d:d['artifacts'][0].update(relative_path='../app.dll'),
                       lambda d:d['native_bindings'][0].update(relative_file='/private/lib.so')]:
            data = copy.deepcopy(self.data); mutate(data)
            with self.assertRaises(ManifestError): validate_schema(data,'linux-x64','jit_shared',None)

    def test_manifest_growth_after_metadata_check_is_bounded(self):
        manifest = self.root/'ryupy-artifact.json'
        manifest.write_bytes(b' '*(4*1024*1024+1))
        def before_growth(path, **kwargs):
            return SimpleNamespace(st_size=2) if Path(path)==manifest else plain_stat(path,**kwargs)
        with patch('binding_manifest.plain_stat',side_effect=before_growth), \
             patch.object(Path,'read_bytes',side_effect=AssertionError('禁止无界读取清单')):
            with self.assertRaisesRegex(ManifestError,'读取中清单增长'): self.verify()

    def test_aot_shape_requires_sdk_and_excludes_shared_role(self):
        data = copy.deepcopy(self.data); data['kind'] = 'ryupy-native-aot-artifact'
        data['toolchain']['sdk_version'] = '10.0.403'; data['native_bindings'][0].update(linkage='aot_static',relative_file=None)
        data['build_inputs'][2]['role'] = 'native_static_archive'
        data['artifacts'] = [item('app','native_executable',b'not executed')]
        validate_schema(data,'linux-x64','aot_static','10.0.403')
        data['artifacts'].append(item('library.so','native_shared',b'not executed'))
        with self.assertRaises(ManifestError): validate_schema(data,'linux-x64','aot_static','10.0.403')


if __name__ == '__main__': unittest.main()
