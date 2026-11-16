"""纯合成元数据负例；不是合法PE、不是编译器产品证明，不启动任何目标。"""
from copy import deepcopy
import hashlib
import json
from pathlib import Path
import runpy
import tempfile
import unittest

HERE = Path(__file__).resolve().parent
ROOT = None  # setUpClass临时模型输入；import不读改argv，也不依赖真实fixture根。
G = runpy.run_path(str(HERE / 'fixed_gate.py'))
TABLE = json.loads((HERE / 'fixed_expectations.json').read_text())['cases']
EXPECT = TABLE['td_dynamic']
MODEL_PE_BYTES = b'synthetic-not-a-valid-pe'  # 仅wrapper mock输入，绝不能交真实解码器。


def model(static=False, bound=False, native=False):
    expectation = deepcopy(TABLE['native_consumers'] if native else EXPECT)
    for module in expectation['modules'] if not native else []:
        module['sha256'] = hashlib.sha256((ROOT/module['fixture']).read_bytes()).hexdigest()
    if static:
        definition = expectation['modules'][1]['definitions'][0]
        definition['mode'], definition['static_signature_hex'] = 'static', '000001'
    data = {'sha256': hashlib.sha256(MODEL_PE_BYTES).hexdigest() if native else 'synthetic-not-a-product',
            'pe_bytes': len(MODEL_PE_BYTES) if native else 0,
            'case_id': 'native_consumers' if native else 'td_dynamic', 'fields': {}, 'methods': []}
    fields, methods = data['fields'], data['methods']
    def field(name):
        token = hex(0x04000000 + len(fields) + 1)
        fields[token] = {'name': name}
        return token
    def ins(op, **kwargs):
        return {'opcode': hex(op), 'operand_hex': '', 'offset': 0, **kwargs}
    def string(value): return ins(0x72, string=value)
    def load(token): return ins(0x7e, field_token=token, field=fields[token])
    def store(token): return ins(0x80, field_token=token, field=fields[token])
    def integer(value): return ins(0x21, operand_hex=value.to_bytes(8, 'little', signed=True).hex())
    def call(owner, name): return ins(0x28, target={'owner': 'RyuPy.Runtime.' + owner, 'name': name})
    def method(name, body, signature='000001'):
        token = hex(0x06000000 + len(methods) + 1)
        row = {'token': token, 'method': {'owner': 'RyuPy.Generated.Module', 'name': name, 'signature_hex': signature},
               'flags': 0x10, 'body': {'instructions': body}}
        methods.append(row)
        return row
    def ptr(m): return ins(0xfe06, token=m['token'], target=m['method'])
    def direct(m): return ins(0x28, token=m['token'], target=m['method'])
    ret = ins(0x2a)
    literal = method('__init_literals', [ret])
    source_init = method('__init_code_sources', [])
    init = method('__init_functions', [direct(literal), direct(source_init)])
    sources = [field(f'm{unit}___code_source') for unit in range(len(expectation['modules']))]
    accessors = {}
    for unit, module in enumerate(expectation['modules']):
        if module['environment'] != 'LegacySingleton':
            continue
        name_field = field(f'm{unit}___mv0___name__')
        bound_field = field(f'm{unit}___bound_0')
        fields[bound_field]['signature_hex'] = '0602'
        getter = method(f'__mod_get_{unit}', [])
        hit = ([load(bound_field), string(module['name']), string('__name__'), call('PyModules','CheckBound'),load(name_field)]
               if bound else [load(name_field),string(module['name']),string('__name__'),call('PyModules','LoadAttribute')])
        branch = ins(0x39, operand_hex='00000000')
        gc = [ins(2),string('__name__'),call('PyModules','SameName'),branch] + hit + [deepcopy(ret),ins(0x14),deepcopy(ret)]
        offset = 0
        for item in gc:
            item['offset'] = offset
            offset += 5 if item['opcode'] in ('0x28','0x72','0x7e','0x39') else 1
        branch['operand_hex'] = (gc[-2]['offset']-branch['offset']-5).to_bytes(4,'little',signed=True).hex()
        getter['body']['instructions'] = gc
        setter = method(f'__mod_set_{unit}', [ret])
        accessors[unit] = getter, setter, name_field, bound_field
    mains, signature_fields = [], []
    for unit, module in enumerate(expectation['modules']):
        legacy = module['environment'] == 'LegacySingleton'
        if legacy:
            getter, setter, name_field, bound_field = accessors[unit]
        source_init['body']['instructions'] += [string(module['name']), string(module['file']), ins(0x14)]
        if legacy:
            source_init['body']['instructions'] += [ptr(getter), ins(0x73, target={'owner':'RyuPy.Runtime.PyModuleGet','name':'.ctor'})]
        source_init['body']['instructions'] += [call('PyTraceback','CreateLegacyCodeSourceV1' if legacy else 'CreateCodeSource'),store(sources[unit])]
        main = method(f'm{unit}___module_main', [load(sources[unit]), ins(0x14 if legacy else 2),
            call('PyTraceback','CreateModuleExecutionSourceWithCode'), ins(0xa), string(module['file']),
            string('<module>'), ins(6), call('PyTraceback','PushFrameWithOrigin'), ret])
        if legacy and bound:
            main['body']['instructions'][-1:-1] = [string('__main__'),store(name_field),ins(0x17),store(bound_field)]
        mains.append(main)
        for definition in module['definitions']:
            sig = field(f'm{unit}___sig_{len(signature_fields)}');signature_fields.append(sig)
            init['body']['instructions'] += [call('PySignature','Create'), store(sig), load(sig),
                string(definition['qualname']), integer(definition['firstline']), load(sources[unit]),
                call('PyOps','FunctionDefinitionWithSource')]
            fixed = definition['mode'] == 'static'
            core = method(f'm{unit}_'+definition['name'], [string(module['file']),string(definition['name']),
                load(sig) if fixed else ins(3), call('PyTraceback','PushFrameWithOrigin'),
                integer(definition['body_lines'][0]),call('PyTraceback','SetLine'),ret],
                '000001' if fixed else '00021c1c1c')
            method('__thunk_'+str(len(signature_fields)), [load(sig)]+([] if fixed else [ins(2+int(not legacy))])+[direct(core),ret])
    source_init['body']['instructions'].append(ret)
    for unit, module in enumerate(expectation['modules']):
        legacy = module['environment'] == 'LegacySingleton'
        kind = (0 if unit == 0 else 2) if native else 1
        path = ('' if unit == 0 else '_internal/resources/v1/modules/sp_pkg/' + module['file']) if native else str(ROOT/module['fixture'])
        package = 'sp_pkg' if native and unit else ''
        init['body']['instructions'] += [string(module['name']),string(package),string(module['file']),
            integer(kind),string(path),ptr(mains[unit])]
        if legacy:
            getter, setter, _, _ = accessors[unit]
            init['body']['instructions'] += [ptr(getter),ptr(setter)]
        init['body']['instructions'].append(call('PyModules','RegisterSourceLocated' if legacy else 'RegisterInstanceSourceLocated'))
    init['body']['instructions'].append(ret)
    return data, expectation, sources, signature_fields


class FixedGateNegativeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        global ROOT
        cls.temporary = tempfile.TemporaryDirectory(prefix='ryupy-pe-model-')
        ROOT = Path(cls.temporary.name)
        for module in EXPECT['modules']:
            path = ROOT/module['fixture'];path.parent.mkdir(parents=True,exist_ok=True)
            path.write_text('# synthetic-only, never executed\n')
    @classmethod
    def tearDownClass(cls):
        cls.temporary.cleanup()

    def accepts(self, data, expectation):
        return G['check'](data, expectation, ROOT, 'new')
    def source(self, data):
        return next(m for m in data['methods'] if m['method']['name']=='__init_code_sources')['body']['instructions']
    def init(self, data):
        return next(m for m in data['methods'] if m['method']['name']=='__init_functions')['body']['instructions']
    def test_synthetic_positive_shape_not_product(self):
        d,e,_,_=model();self.assertTrue(self.accepts(d,e)['admitted'])
    def test_warning_origin_old_two_or_three_new_only_three(self):
        # 只测试旧/new图定位边界；词法定义另由完整模型门检查。
        expectation = deepcopy(TABLE['warning_origin'])
        for module in expectation['modules']:
            module['definitions'] = []
        for count, role, accepted in [(2, 'old', True), (3, 'old', True),
                                       (2, 'new', False), (3, 'new', True)]:
            methods = [{'token': 'init', 'method': {'owner': 'RyuPy.Generated.Module',
                        'name': '__init_functions'}, 'body': {'instructions': []}}]
            methods += [{'token': str(i), 'method': {'owner': 'RyuPy.Generated.Module',
                         'name': f'm{i}___module_main'}} for i in range(count)]
            with self.subTest(count=count, role=role):
                if accepted:
                    self.assertEqual(len(G['locate']({'methods': methods}, expectation, role)[0]), count)
                else:
                    with self.assertRaises(ValueError):
                        G['locate']({'methods': methods}, expectation, role)
    def test_jit_rejects_wrong_kind_relative_missing_or_changed_source(self):
        for mutation in ('kind0', 'kind2', 'relative', 'missing', 'hash', 'missing-kind'):
            with self.subTest(mutation=mutation):
                d,e,_,_=model();ins=self.init(d)
                at=next(i for i,x in enumerate(ins) if x.get('target',{}).get('name')=='RegisterSourceLocated')
                path=next(i for i in range(at-1,-1,-1) if ins[i]['opcode']=='0x72')
                if mutation.startswith('kind'):
                    ins[path-1]['operand_hex']=int(mutation[-1]).to_bytes(8,'little',signed=True).hex()
                elif mutation=='missing-kind': del ins[path-1]
                elif mutation in ('relative','missing'): ins[path]['string']='entry.py' if mutation=='relative' else str(ROOT/'absent.py')
                else:
                    wrong=ROOT/'different.py';wrong.write_text('# different\n');ins[path]['string']=str(wrong)
                with self.assertRaises(ValueError):self.accepts(d,e)
    def test_accepts_typed_bound_shape_not_product(self):
        d,e,_,_=model(bound=True);self.assertTrue(self.accepts(d,e)['admitted'])
    def test_rejects_name_published_before_frame(self):
        d,e,_,_=model(bound=True);m=next(m for m in d['methods'] if m['method']['name']=='m0___module_main')
        ins=m['body']['instructions'];publish=ins[-5:-1];del ins[-5:-1];ins[4:4]=publish
        with self.assertRaises(ValueError):self.accepts(d,e)
    def test_rejects_wrong_getter_value_field(self):
        for bound in (False,True):
            d,e,s,_=model(bound=bound);g=next(m for m in d['methods'] if m['method']['name']=='__mod_get_0')
            g['body']['instructions'][8 if bound else 4]['field_token']=s[1]
            with self.assertRaises(ValueError):self.accepts(d,e)
    def test_rejects_wrong_bound_field(self):
        d,e,_,_=model(bound=True);wrong='0x4ffffff';d['fields'][wrong]={'name':'m0___bound_other','signature_hex':'0602'}
        g=next(m for m in d['methods'] if m['method']['name']=='__mod_get_0');g['body']['instructions'][4]['field_token']=wrong
        with self.assertRaises(ValueError):self.accepts(d,e)
    def test_rejects_wrong_branch_boundary(self):
        d,e,_,_=model();g=next(m for m in d['methods'] if m['method']['name']=='__mod_get_0')
        g['body']['instructions'][3]['operand_hex']='01000000'
        with self.assertRaises(ValueError):self.accepts(d,e)
    def test_rejects_missing_or_wrong_lexical_capability(self):
        for unit, wrong in [(0, 'CreateCodeSource'), (1, 'CreateLegacyCodeSourceV1')]:
            d,e,_,_=model()
            calls=[x for x in self.source(d) if x.get('target',{}).get('owner') == 'RyuPy.Runtime.PyTraceback']
            calls[unit]['target']['name']=wrong
            with self.assertRaises(ValueError):self.accepts(d,e)
    def test_rejects_swapped_module_literal(self):
        d,e,_,_=model();self.source(d)[0]['string']='subject'
        with self.assertRaises(ValueError):self.accepts(d,e)
    def test_rejects_swapped_source_field(self):
        d,e,s,_=model();self.source(d)[6]['field_token']=s[1]
        with self.assertRaises(ValueError):self.accepts(d,e)
    def test_rejects_wrong_getter_methoddef(self):
        d,e,_,_=model();pointer=self.source(d)[3];wrong=next(m for m in d['methods'] if m['method']['name']=='__mod_set_0');pointer.update(token=wrong['token'],target=wrong['method'])
        with self.assertRaises(ValueError):self.accepts(d,e)
    def test_rejects_signature_wrong_source(self):
        d,e,s,_=model();i=next(k for k,x in enumerate(self.init(d)) if x.get('target',{}).get('name')=='FunctionDefinitionWithSource');self.init(d)[i-1]['field_token']=s[1]
        with self.assertRaises(ValueError):self.accepts(d,e)
    def test_rejects_dynamic_wrong_origin(self):
        d,e,_,_=model();m=next(m for m in d['methods'] if m['method']['name']=='m1_make');m['body']['instructions'][2]['opcode']='0x2'
        with self.assertRaises(ValueError):self.accepts(d,e)
    def test_rejects_module_wrong_context(self):
        d,e,_,_=model();m=next(m for m in d['methods'] if m['method']['name']=='m1___module_main');m['body']['instructions'][1]['opcode']='0x14'
        with self.assertRaises(ValueError):self.accepts(d,e)
    def test_rejects_static_other_signature(self):
        d,e,_,s=model(static=True);self.assertTrue(self.accepts(d,e)['admitted']);m=next(m for m in d['methods'] if m['method']['name']=='m1_make');m['body']['instructions'][2]['field_token']=s[0]
        with self.assertRaises(ValueError):self.accepts(d,e)
    def test_rejects_same_name_core_swap(self):
        d,e,_,_=model();thunks=[m for m in d['methods'] if m['method']['name'].startswith('__thunk_')];one,two=thunks[0]['body']['instructions'][-2],thunks[2]['body']['instructions'][-2];one['token'],two['token']=two['token'],one['token'];one['target'],two['target']=two['target'],one['target']
        with self.assertRaises(ValueError):self.accepts(d,e)


if __name__ == '__main__':
    unittest.main()
