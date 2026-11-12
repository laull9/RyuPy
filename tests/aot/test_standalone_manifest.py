"""schema4组合字段与资源负合同；合成结构不冒充实际Native闭包。"""
import copy
import sys
from pathlib import Path
import unittest
sys.path.insert(0, str(Path(__file__).parent / 'default_source_aot'))
from test_schema4 import sample, validate as validate_empty
from standalone_manifest import validate_standalone_schema4
from manifest_files import ManifestError


def combined():
    data, _, _ = sample()
    record = {'relative_path': '_internal/resources/v1/modules/standard.flf',
              'size': 12, 'sha256': 'b' * 64, 'consumer_module_ids': [1]}
    data['resources'] = [record]
    data['artifacts'].append({k:v for k,v in record.items() if k != 'consumer_module_ids'} |
                             {'role': 'declared_resource'})
    data['scope'].update(resources_included=True, declared_resources_complete=True)
    return data, copy.deepcopy(data['resources'])


def validate(data, expected):
    validate_standalone_schema4(data, 'linux-x64', '10.0.403',
        expected_automatic=('sample',), metadata_report={'checked': True},
        expected_resources=expected)


class StandaloneManifestTests(unittest.TestCase):
    def test_combination_and_explicit_empty_do_not_mutate(self):
        data, expected = combined(); old = copy.deepcopy(data)
        validate(data, expected); self.assertEqual(data, old)
        with self.assertRaises(ManifestError): validate_empty(data)
        empty, _, _ = sample(); validate_empty(empty)
        with self.assertRaises(ManifestError): validate(empty, [])
        empty['scope']['declared_resources_complete'] = True
        validate(empty, [])
        with self.assertRaises(ManifestError): validate_empty(empty)

    def test_resource_corruptions_rejected(self):
        changes = [
            lambda d: d['resources'].clear(),
            lambda d: d['resources'].append(copy.deepcopy(d['resources'][0])),
            lambda d: d['resources'][0].update(consumer_module_ids=[]),
            lambda d: d['resources'][0].update(consumer_module_ids=[True]),
            lambda d: d['resources'][0].update(consumer_module_ids=[99]),
            lambda d: d['resources'][0].update(consumer_module_ids=[1, 1]),
            lambda d: d['resources'][0].update(consumer_module_ids=[0]),
            lambda d: d['resources'][0].update(sha256='c'*64),
            lambda d: d['resources'][0].update(size=13),
            lambda d: d['resources'][0].update(relative_path='../escape'),
            lambda d: d['resources'][0].update(relative_path='_internal/resources/v1/modules/code.py'),
            lambda d: d['artifacts'].pop(),
            lambda d: d['artifacts'][-1].update(role='unclassified_sidecar'),
            lambda d: d['module_locations'][1].update(relative_file='_internal/resources/v1/elsewhere.py'),
            lambda d: d['module_locations'][1].update(module_name='wrong'),
            lambda d: d['module_locations'].pop(),
            lambda d: d['scope'].update(resources_included=1),
            lambda d: d['scope'].update(resources_included=False),
            lambda d: d['scope'].update(declared_resources_complete=False),
            lambda d: d['scope'].update(automatic_module_locations_complete=False),
            lambda d: d['scope'].update(dynamic_dependency_closure_complete=True),
            lambda d: d.update(unknown=True),
        ]
        for index, change in enumerate(changes):
            with self.subTest(index=index):
                data, expected = combined(); change(data)
                with self.assertRaises(ManifestError): validate(data, expected)

    def test_anchor_and_case_collisions_rejected(self):
        for path in ('_internal/resources/v1/modules/sample.py',
                     '_internal/resources/v1/MODULES/standard.flf'):
            data, _ = combined()
            data['resources'][0]['relative_path'] = path
            data['artifacts'][-1]['relative_path'] = path
            with self.assertRaises(ManifestError): validate(data, copy.deepcopy(data['resources']))
        data, _ = combined()
        # 延期源码也有虚拟锚；即便无consumer ID，也不能把资源占到该锚。
        data['default_source_plan']['deferred_sources'] = [{
            'module_name': 'future', 'package': '', 'logical_source': 'modules/standard.flf',
            'source_sha256': 'c' * 64, 'relative_file': data['resources'][0]['relative_path'],
            'is_package': False, 'reason': 'deferred'}]
        with self.assertRaises(ManifestError): validate(data, copy.deepcopy(data['resources']))

    def test_independent_expected_set_catches_consistent_omission(self):
        data, expected = combined()
        data['resources'] = []; data['artifacts'].pop()
        data['scope']['resources_included'] = False
        with self.assertRaises(ManifestError): validate(data, expected)
        data, expected = combined(); expected[0]['sha256'] = 'c' * 64
        with self.assertRaises(ManifestError): validate(data, expected)

if __name__ == '__main__': unittest.main()
