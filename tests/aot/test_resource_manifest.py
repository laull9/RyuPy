"""资源清单纯文件/结构轻测，不冒真正ILC或源码加载证明。"""
import copy
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
from manifest_verify import (MANIFEST_NAME, ManifestError, validate_schema, verify_manifest,
                             relocate_manifest, dependency_binaries)
from resource_manifest import check_targets, portable
from test_manifest_verify import manifest_fixture


def resource_fixture(root):
    data = manifest_fixture(root)
    data['schema_version'] = 2
    data['scope'].update(resources_included=True, declared_resources_complete=True)
    data['module_locations'] = [{'module_id':0, 'module_name':'__main__',
        'kind':'executable_relative', 'relative_file':'_internal/resources/v1/entry/main.py'}]
    relative = '_internal/resources/v1/entry/data.txt'
    path = root/relative
    path.parent.mkdir(parents=True)
    raw = '真实随包数据\n'.encode()
    path.write_bytes(raw)
    resource = {'relative_path':relative, 'size':len(raw), 'sha256':hashlib.sha256(raw).hexdigest()}
    data['resources'] = [{**resource, 'consumer_module_ids':[0]}]
    data['artifacts'].append({**resource, 'role':'declared_resource'})
    save(root, data)
    return data


def save(root, data):
    (root/MANIFEST_NAME).write_text(json.dumps(data), encoding='utf-8')


class ResourceManifestTests(unittest.TestCase):
    def test_full_relocation_preserves_data_symbols_and_no_source_anchor(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)/'publish'; data = resource_fixture(root)
            binary, moved = relocate_manifest(root, root/'program', Path(directory)/'moved', 'linux-x64','10.0.403')
            self.assertEqual(moved, data)
            self.assertEqual((binary.parent/data['resources'][0]['relative_path']).read_bytes(), '真实随包数据\n'.encode())
            self.assertFalse((binary.parent/data['module_locations'][0]['relative_file']).exists())
            self.assertTrue((binary.parent/'symbols/program.dbg').is_file())
            self.assertEqual(dependency_binaries(binary.parent, data), [binary])

    def test_unicode_root_native_with_ascii_resources_relocates(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)/'publish'; data = resource_fixture(root)
            (root/'program').rename(root/'中文入口')
            native = next(x for x in data['artifacts'] if x['role']=='native_executable')
            native['relative_path'] = '中文入口'; save(root,data)
            binary,moved = relocate_manifest(root,root/'中文入口',Path(directory)/'moved','linux-x64','10.0.403')
            self.assertEqual(binary.name,'中文入口')
            self.assertEqual(moved,data)
            self.assertTrue((binary.parent/data['resources'][0]['relative_path']).is_file())

    def test_native_unicode_exception_does_not_expand_other_names(self):
        native = {'relative_path':'中文入口','role':'native_executable'}
        for path in ('中文资源.txt','symbols/中文.dbg','_internal/resources/v1/中文.txt'):
            with self.subTest(path=path), self.assertRaises(ManifestError):
                check_targets([native,{'relative_path':path,'role':'unclassified_sidecar'}],[])
        for path in ('../escape','dir/入口','入口\\x','入口:bad','入口.', '入口 ',
                     'CON .txt','com¹.exe','LPT²','_INTERNAL','RYUPY-artifact.json',
                     '.ryupy-artifact.json.tmp','入口\x85'):
            with self.subTest(native=path), self.assertRaises(ManifestError):
                check_targets([{'relative_path':path,'role':'native_executable'}],[])
        with self.assertRaises(ManifestError):
            check_targets([native],['_internal/resources/v1/中文.py'])
        with self.assertRaises(ManifestError):
            check_targets([native,dict(native)],[])
        with self.assertRaises(ManifestError):
            check_targets([{'relative_path':'Program','role':'native_executable'},
                           {'relative_path':'program','role':'unclassified_sidecar'}],[])
        # Unicode字符不作casefold；明确只折叠ASCII，避免暗造跨平台Unicode等价承诺。
        from resource_manifest import path_key
        self.assertEqual(path_key('AİK'), 'aİK')

    def test_missing_extra_and_same_size_tampering_are_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)/'publish'; data = resource_fixture(root)
            path = root/data['resources'][0]['relative_path']; raw = path.read_bytes()
            for replacement in (None, b'x'*len(raw)):
                if replacement is None:
                    path.unlink()
                else:
                    path.write_bytes(replacement)
                with self.assertRaises(ManifestError):
                    verify_manifest(root,root/'program','linux-x64','10.0.403')
            path.write_bytes(raw)
            (path.parent/'extra').write_bytes(b'not selected')
            with self.assertRaises(ManifestError):
                verify_manifest(root,root/'program','linux-x64','10.0.403')

    def test_empty_selection_and_shared_logical_anchor_are_valid(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)/'publish'; data = resource_fixture(root)
            alias = {**data['sources'][0], 'module_id':1, 'module_name':'alias'}
            data['sources'].append(alias)
            data['modules'].append({**data['modules'][0], 'id':1, 'name':'alias'})
            data['module_locations'].append({**data['module_locations'][0], 'module_id':1,'module_name':'alias'})
            data['resources'][0]['consumer_module_ids'].append(1)
            validate_schema(data, 'linux-x64','10.0.403')
            data['resources'] = []
            data['artifacts'] = data['artifacts'][:2]
            data['scope']['resources_included'] = False
            validate_schema(data, 'linux-x64','10.0.403')
            data['module_locations'] = []
            validate_schema(data, 'linux-x64','10.0.403')

    def test_schema_identity_consumer_hash_scope_and_budget_fail_closed(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)/'publish'; original = resource_fixture(root)
            changes = [
                lambda d: d['resources'][0].update(consumer_module_ids=[]),
                lambda d: d['resources'][0].update(consumer_module_ids=[False]),
                lambda d: d['resources'][0].update(consumer_module_ids=[1]),
                lambda d: d['resources'][0].update(consumer_module_ids=[0,0]),
                lambda d: d['resources'][0].update(sha256='0'*64),
                lambda d: d['resources'][0].update(size=16*1024*1024+1),
                lambda d: d['resources'].append(copy.deepcopy(d['resources'][0])),
                lambda d: d['artifacts'].pop(),
                lambda d: d['module_locations'][0].update(module_name='wrong'),
                lambda d: d['module_locations'][0].update(kind='source_absolute'),
                lambda d: d['module_locations'][0].update(relative_file='C:/private/main.py'),
                lambda d: d['module_locations'][0].update(relative_file='_internal/resources/v1/other.py'),
                lambda d: d['scope'].update(resources_included=False),
                lambda d: d['scope'].update(declared_resources_complete=False),
                lambda d: d['scope'].update(dynamic_dependency_closure_complete=True),
            ]
            for change in changes:
                data = copy.deepcopy(original); change(data)
                with self.subTest(change=change), self.assertRaises(ManifestError):
                    validate_schema(data, 'linux-x64','10.0.403')

    def test_all_target_portability_anchor_and_ancestor_collisions(self):
        for anchor in ('modules/CON.py','modules/a./main.py','modules/a:b/main.py','modules/a?/main.py','modules/é.py','modules/\u0345\u0301.py'):
            with self.subTest(anchor=anchor), self.assertRaises(ManifestError):
                check_targets([], [anchor])
        pairs = [('a','a/b'), ('a/b','a'), ('a','A'), ('é/file','e\u0301/file'),
                 ('same.py','same.py'), ('dir/x','DIR/y')]
        for anchor, resource in pairs:
            with self.subTest(pair=(anchor,resource)), self.assertRaises(ManifestError):
                check_targets([{'relative_path':resource}], [anchor])
        check_targets([{'relative_path':'dir/data'}], ['dir/a.py','dir/a.py'])

    def test_portable_limits_and_extra_windows_device_names_match_driver(self):
        for value in ('CONIN$', 'conout$.txt', 'CLOCK$', 'CON .txt', 'com1 .bin',
                      'LPT9 .dat', 'x/' + 'a'*256, '/'.join(['a'*255]*4) + '/x'):
            with self.subTest(value=value), self.assertRaises(ManifestError):
                portable(value)
        self.assertEqual(portable('a'*255), 'a'*255)
        maximum = '/'.join(['a'*255]*4) + 'a'
        self.assertEqual(len(maximum), 1024)
        # 1024总长边界需每段仍<=255；这里末段256故拒。
        with self.assertRaises(ManifestError):
            portable(maximum)
        valid = '/'.join(['a'*255]*3 + ['b'*254, 'c'])
        self.assertEqual(len(valid), 1024)
        portable(valid)
        portable('COM0.txt')
        portable('CLOCKwise.txt')

    def test_resource_library_suffix_is_not_an_implicit_native_dependency(self):
        data = {'artifacts':[{'relative_path':'program','role':'native_executable'},
            {'relative_path':'data.dll','role':'declared_resource'},
            {'relative_path':'real.dll','role':'unclassified_sidecar'}]}
        self.assertEqual(dependency_binaries(Path('root'),data), [Path('root/program'),Path('root/real.dll')])


if __name__ == '__main__':
    unittest.main()
