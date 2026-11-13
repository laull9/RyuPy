"""schema1/2/3共享当前生产者选项合同；合成结构不冒真实publish。"""
import copy
from pathlib import Path
import tempfile
import unittest
from manifest_files import ManifestError
from manifest_verify import validate_schema
from binding_manifest import validate_schema as validate_binding
from test_manifest_verify import manifest_fixture
from test_resource_manifest import resource_fixture
from test_binding_manifest import sample


class CurrentProducerOptionsTests(unittest.TestCase):
    def cases(self, folder):
        root = Path(folder)
        yield manifest_fixture(root / 'v1'), lambda d: validate_schema(d, 'linux-x64', '10.0.403')
        yield resource_fixture(root / 'v2'), lambda d: validate_schema(d, 'linux-x64', '10.0.403')
        (root / 'v3').mkdir()
        data, _ = sample(root / 'v3')
        yield data, lambda d: validate_binding(d, 'linux-x64', 'jit_shared', None)

    def test_current_fields_and_both_policies_and_duplicate_selection(self):
        with tempfile.TemporaryDirectory() as folder:
            for data, validate in self.cases(folder):
                validate(data)
                data['compiler']['options']['module_environment_policy'] = 'ordinary-dynamic-module-pilot'
                # 生产者保存Session Vec，重复项不是本validator可擅自禁止的额外合同。
                data['compiler']['options']['instance_modules'] = ['pkg.mod', 'pkg.mod']
                validate(data)

    def test_catalog_only_current_default_is_accepted_in_all_schemas(self):
        # 普通CLI默认CatalogOnly；不能只用旧explicit-only合成清单掩盖真实发布失败。
        with tempfile.TemporaryDirectory() as folder:
            for data, validate in self.cases(folder):
                with self.subTest(schema=data["schema_version"]):
                    data["compiler"]["options"]["module_environment_policy"] = "catalog-only"
                    validate(data)

    def test_exact_keys_and_types_reject_all_schemas(self):
        with tempfile.TemporaryDirectory() as folder:
            for data, validate in self.cases(folder):
                options = data['compiler']['options']
                mutations = [lambda o:o.update(unknown=False),
                             lambda o:o.update(instance_modules='pkg'),
                             lambda o:o.update(instance_modules=[1]),
                             lambda o:o.update(instance_modules=['']),
                             lambda o:o.update(module_environment_policy='unknown'),
                             lambda o:o.update(module_environment_policy=False)]
                for key in options:
                    mutations.append(lambda o, key=key:o.pop(key))
                for key in ('experimental_direct_accumulator', 'experimental_foreach_accumulator',
                            'experimental_leaf_inline', 'diagnostic_scalar_slp', 'experimental_packed_slp'):
                    for wrong in (0, 1, None, 'false', []):
                        mutations.append(lambda o, key=key, wrong=wrong:o.update({key:wrong}))
                for mutation in mutations:
                    changed = copy.deepcopy(data); mutation(changed['compiler']['options'])
                    with self.assertRaises(ManifestError): validate(changed)
                old = copy.deepcopy(data)
                for key in ('experimental_foreach_accumulator', 'instance_modules', 'module_environment_policy'):
                    del old['compiler']['options'][key]
                with self.assertRaises(ManifestError): validate(old)

    def test_module_reason_required_and_unknown_module_keys_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            for data, validate in self.cases(folder):
                for mutate in (lambda m:m.pop('environment_reason'),
                               lambda m:m.update(environment_reason=''),
                               lambda m:m.update(environment_reason=False),
                               lambda m:m.update(environment_reason=[]),
                               lambda m:m.update(unknown='field')):
                    changed = copy.deepcopy(data); mutate(changed['modules'][0])
                    with self.assertRaises(ManifestError): validate(changed)


if __name__ == '__main__':
    unittest.main()
