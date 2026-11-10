"""固定PE/Native输入门的纯合成反例；不是真PE或产品运行证明。"""
from copy import deepcopy
import unittest
from pe_gate import check, bind_native_inputs


def integer(value):
    return {'opcode': '0x21', 'operand_hex': value.to_bytes(8, 'little', signed=True).hex()}


def model(fixture='resource'):
    methods = []
    def method(name, body, signature='000001'):
        item = {'token': hex(0x06000001 + len(methods)), 'method': {
            'owner': 'RyuPy.Generated.Module', 'name': name, 'signature_hex': signature},
            'flags': 0x10, 'body': {'instructions': body}}
        methods.append(item); return item
    def runtime(owner, name):
        return {'opcode': '0x28', 'target': {'owner': 'RyuPy.Runtime.' + owner, 'name': name}}
    def field(token, store=False):
        return {'opcode': '0x80' if store else '0x7e', 'field_token': token}
    def pointer(target): return {'opcode': '0xfe06', 'token': target['token']}
    def text(value): return {'opcode': '0x72', 'string': value}
    ret = {'opcode': '0x2a'}
    firstline, body_line = (28, 29) if fixture == 'original' else (15, 16)
    core = method('m1_out', [text('__init__.py'), text('out'), field('signature'),
                  runtime('PyTraceback', 'PushFrameWithOrigin'), integer(body_line), runtime('PyTraceback', 'SetLine'), ret])
    direct = {'opcode': '0x28', 'token': core['token']}
    method('__thunk_out', [field('signature'), deepcopy(direct), ret])
    mains = [method('m' + str(i) + '___module_main', [deepcopy(direct), ret] if i == 1 else [ret]) for i in range(3)]
    sources = method('__init_code_sources', [])
    for i, name in enumerate(('__main__', 'pkg', 'pkg.data')):
        getter = method('__mod_get_' + str(i), [ret])
        sources['body']['instructions'] += [text(name), text('entry.py' if i == 0 else '__init__.py'),
            {'opcode': '0x14'}, pointer(getter), {'opcode': '0x73', 'target': {'owner': 'RyuPy.Runtime.PyModuleGet'}},
            runtime('PyTraceback', 'CreateLegacyCodeSourceV1'), field('source' + str(i), True)]
    sources['body']['instructions'].append(ret)
    signature = [text('out')]
    for _ in range(3): signature += [integer(0), {'opcode': '0x8d'}]
    signature += [runtime('PySignature', 'Create'), field('signature', True), field('signature'),
                  text('out'), integer(firstline), field('source1'), runtime('PyOps', 'FunctionDefinitionWithSource')]
    init = method('__init_functions', signature)
    for i, main in enumerate(mains):
        registration = runtime('PyModules', 'RegisterSourceLocated'); registration['offset'] = i
        init['body']['instructions'] += [pointer(main), registration]
    return {'sha256': 'synthetic-not-pe', 'methods': methods}


def named(data, name):
    return next(method for method in data['methods'] if method['method']['name'] == name)


class RejectionTests(unittest.TestCase):
    def test_exact_synthetic_models_are_not_product_proof(self):
        for fixture in ('original', 'resource'):
            self.assertTrue(check(model(fixture), fixture)['lexical_v1_verified'])

    def test_rejects_abi_old_factory_other_getter_and_source(self):
        for change in ('abi', 'factory', 'getter', 'source', 'direct', 'data-call', 'registration'):
            data = deepcopy(model())
            sources = named(data, '__init_code_sources')['body']['instructions']
            if change == 'abi': named(data, 'm1_out')['method']['signature_hex'] = '0001011c'
            if change == 'factory': sources[12]['target']['name'] = 'CreateCodeSource'
            if change == 'getter': sources[10]['token'] = named(data, '__mod_get_0')['token']
            if change == 'source': named(data, '__init_functions')['body']['instructions'][12]['field_token'] = 'source0'
            if change == 'direct': named(data, 'm1___module_main')['body']['instructions'][0]['opcode'] = '0xfe06'
            if change == 'data-call': named(data, 'm0___module_main')['body']['instructions'].append(
                {'opcode': '0x28', 'token': named(data, 'm2___module_main')['token']})
            if change == 'registration': named(data, '__init_functions')['body']['instructions'].pop()
            with self.subTest(change=change), self.assertRaises(ValueError):
                check(data, 'resource')

    def test_rejects_other_signature_even_when_it_has_same_package_source(self):
        data = model()
        init = named(data, '__init_functions')['body']['instructions']
        other = deepcopy(init[:14])
        other[0]['string'] = other[10]['string'] = 'other'
        other[8]['field_token'] = other[9]['field_token'] = 'other-signature'
        other[11] = integer(22)
        # 保合法out原链，另建同pkg source1关联；只有out帧偷换成另一个函数。
        init += other
        named(data, 'm1_out')['body']['instructions'][2]['field_token'] = 'other-signature'
        with self.assertRaisesRegex(ValueError, '自身定义signature'):
            check(data, 'resource')

    def test_rejects_wrong_create_definition_line_and_thunk(self):
        for change in ('create-name', 'create-call', 'store', 'qualname', 'definition-line', 'body-line', 'thunk', 'fixture'):
            data = model(); init = named(data, '__init_functions')['body']['instructions']
            if change == 'create-name': init[0]['string'] = 'other'
            if change == 'create-call': init[7]['target']['name'] = 'Other'
            if change == 'store': init[8]['field_token'] = 'other-signature'
            if change == 'qualname': init[10]['string'] = 'other'
            if change == 'definition-line': init[11] = integer(22)
            if change == 'body-line': named(data, 'm1_out')['body']['instructions'][4] = integer(22)
            if change == 'thunk': named(data, '__thunk_out')['body']['instructions'][1]['token'] = named(data, 'm0___module_main')['token']
            with self.subTest(change=change), self.assertRaises(ValueError):
                check(data, 'original' if change == 'fixture' else 'resource')

    def test_native_inputs_require_exact_pe_and_runtime_hash_and_size(self):
        expected = {'compiled_managed_input': {'sha256': 'a' * 64, 'size': 123},
                    'runtime_managed_input': {'sha256': 'b' * 64, 'size': 456}}
        manifest = {'build_inputs': [dict(role=role, **value) for role, value in expected.items()]}
        self.assertEqual(bind_native_inputs(manifest, expected), expected)
        for role in expected:
            for field, wrong in [('sha256', 'c' * 64), ('size', 999)]:
                changed = deepcopy(manifest)
                next(row for row in changed['build_inputs'] if row['role'] == role)[field] = wrong
                with self.subTest(role=role, field=field), self.assertRaises(ValueError):
                    bind_native_inputs(changed, expected)

    def test_empty_or_unrelated_method_cannot_claim_static_capability(self):
        with self.assertRaises(ValueError):
            check({'methods': [], 'sha256': 'synthetic'}, 'resource')


if __name__ == '__main__':
    unittest.main()
