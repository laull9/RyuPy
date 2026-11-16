"""固定具名fixture的PE关键链；不读取production IR，不执行生成程序集，不推广为通用证明。"""
from pathlib import Path
import hashlib
import json


def need(ok, message):
    if not ok:
        raise ValueError(message)


def code(method):
    need(method.get('body') is not None, '实际MethodDef没有body: ' + method['method']['name'])
    return method['body']['instructions']


def runtime(ins, owner, name):
    target = ins.get('target', {})
    return ins['opcode'] == '0x28' and target.get('owner') == 'RyuPy.Runtime.' + owner and target.get('name') == name


def number(ins):
    op = int(ins['opcode'], 16)
    if 0x15 <= op <= 0x1e:
        return op - 0x16
    if op in (0x1f, 0x20, 0x21):
        return int.from_bytes(bytes.fromhex(ins['operand_hex']), 'little', signed=True)
    return None


def local(ins, load):
    op = int(ins['opcode'], 16)
    start = 6 if load else 10
    if start <= op <= start + 3:
        return op - start
    if op in ((0x11, 0xfe0c) if load else (0x13, 0xfe0e)):
        return int.from_bytes(bytes.fromhex(ins['operand_hex']), 'little')
    return None


def expression(ins, end, depth=0):
    """仅解本门涉及的load/局部别名/两参marker表达式；未知形状拒绝，不猜栈。"""
    need(0 <= end < len(ins) and depth < 12, 'origin表达式越界/过深')
    i = ins[end]
    op = int(i['opcode'], 16)
    if op == 0x00:
        return expression(ins, end - 1, depth + 1)
    if op == 0x7e:
        return ('field', i['field_token']), end - 1
    if op == 0x14:
        return ('null',), end - 1
    if op == 0x72:
        return ('string', i['string']), end - 1
    value = number(i)
    if value is not None:
        return ('integer', value), end - 1
    if 0x02 <= op <= 0x05:
        return ('argument', op - 2), end - 1
    if op in (0x0e, 0xfe09):
        return ('argument', int.from_bytes(bytes.fromhex(i['operand_hex']), 'little')), end - 1
    index = local(i, True)
    if index is not None:
        stores = [k for k in range(end) if local(ins[k], False) == index]
        need(stores, 'frame local没有先前赋值')
        need(not any(0x2b <= int(x['opcode'], 16) <= 0x45 or int(x['opcode'], 16) in (0xdd, 0xde)
                     for x in ins[stores[-1]+1:end]), 'origin别名跨分支，超出固定前言域')
        resolved, _ = expression(ins, stores[-1] - 1, depth + 1)
        return resolved, end - 1
    if runtime(i, 'PyTraceback', 'CreateModuleExecutionSourceWithCode'):
        context, previous = expression(ins, end - 1, depth + 1)
        source, previous = expression(ins, previous, depth + 1)
        return ('module-marker', source, context), previous
    raise ValueError('不在固定origin表达式域: ' + json.dumps(i, ensure_ascii=False))


def unique(rows, description):
    need(len(rows) == 1, description + ' 数量不是1: ' + str(len(rows)))
    return rows[0]


def locate(decoded, expectation, role):
    """旧PE也可调用的独立词法→signature→thunk→真实core定位，不冒新source链正例。"""
    methods = {m['token']: m for m in decoded['methods']}
    owner_methods = [m for m in methods.values() if m['method']['owner'] == 'RyuPy.Generated.Module']
    init = unique([m for m in owner_methods if m['method']['name'] == '__init_functions'], '冷初始化MethodDef')
    mains = [m for m in owner_methods if m['method']['name'].endswith('___module_main')]
    count = len(mains)
    counts = (expectation['allowed_module_counts_by_role'][role] if 'allowed_module_counts_by_role' in expectation
              else expectation['allowed_module_counts'])
    need(count in counts, '模块数不是固定臂')
    modules = expectation['modules'][:count]
    need(all(not m['optional'] for m in modules[:-1]), 'optional图次序错误')
    definitions = {}
    for index, module in enumerate(modules):
        for item in module['definitions']:
            key = (index, item['qualname'], item['firstline'])
            need(key not in definitions, '词法预期重复')
            definitions[key] = item
    instructions = code(init)
    association_name = 'FunctionDefinitionWithSource' if role == 'new' else 'FunctionDefinition'
    associated, evidence = {}, []
    for k, ins in enumerate(instructions):
        if not runtime(ins, 'PyOps', association_name):
            continue
        length = 4 if role == 'new' else 3
        parts = instructions[k-length:k]
        need(len(parts) == length and parts[0]['opcode'] == '0x7e' and parts[1]['opcode'] == '0x72', '词法关联参数形状')
        field = parts[0]['field_token']
        field_name = parts[0]['field']['name']
        # 来源前缀不单独自证：稍后必须与独立词法表和实际thunk→core单元同时一致。
        prefix = field_name.split('_', 1)[0]
        need(prefix.startswith('m') and prefix[1:].isdigit(), '签名字段缺固定单元前缀')
        index = int(prefix[1:])
        key = (index, parts[1]['string'], number(parts[2]))
        need(key in definitions and field not in associated, '签名词法/行/单元不符或重复: ' + str(key))
        need(k >= length + 2 and runtime(instructions[k-length-2], 'PySignature', 'Create')
             and instructions[k-length-1]['opcode'] == '0x80'
             and instructions[k-length-1]['field_token'] == field, '不是实际Create刚写入的signature')
        thunks = [m for m in owner_methods if m['method']['name'].startswith('__thunk_') and
                  any(x.get('field_token') == field and x['opcode'] == '0x7e' for x in code(m))]
        thunk = unique(thunks, 'signature真实thunk')
        targets = {x['token'] for x in code(thunk) if x['opcode'] == '0x28' and
                   x.get('token') in methods and x['target']['name'].startswith('m')}
        core = methods[unique(list(targets), 'thunk实际core MethodDef')]
        expected = definitions[key]
        need(core['method']['name'] == f'm{index}_' + expected['name'], 'core单元/名字错绑')
        need(core['flags'] & 0x10, 'core不是静态CIL方法')
        lines = [(j, x) for j, x in enumerate(code(core)) if runtime(x, 'PyTraceback', 'SetLine')]
        if not expected['generator']:
            need(lines, '普通core无真实行更新')
            first, _ = expression(code(core), lines[0][0] - 1)
            need(first[0] == 'integer' and first[1] in expected['body_lines'], '首SetLine不属于该词法正文: ' + str(key))
        if expected['mode'] == 'static':
            need(core['method']['signature_hex'] == expected['static_signature_hex'], 'Static数值ABI改变')
        associated[field] = {'key': key, 'definition': expected, 'core': core, 'thunk': thunk,
                             'source_instruction': parts[3] if role == 'new' else None}
        evidence.append({'unit': index, 'qualname': key[1], 'firstline': key[2],
                         'signature_field': field, 'thunk': thunk['token'], 'core': core['token'],
                         'mode': expected['mode'], 'generator_frame_excluded': expected['generator']})
    need(sum(runtime(x, 'PySignature', 'Create') for x in instructions) == len(associated), '额外未关联signature')
    need(len(associated) == len(definitions), '签名关联未覆盖固定全部定义')
    need(len({tuple(a['key']) for a in associated.values()}) == len(definitions), '同词法定义被重复签名替代')
    return modules, methods, init, associated, evidence


def getter_name(getter, fields, index, module_name, main):
    """只取SameName的单一线性命中分支；guard/value分别核，不扫描模糊存在。"""
    gc = code(getter)
    compare = unique([j for j, x in enumerate(gc) if runtime(x, 'PyModules', 'SameName')
                      and j >= 2 and gc[j-1].get('string') == '__name__'], 'getter name比较')
    need(gc[compare-2]['opcode'] == '0x2', 'getter比较不是实际name实参')
    branch = gc[compare+1]
    need(branch['opcode'] in ('0x2c', '0x39'), 'getter不是BranchFalsy')
    raw = bytes.fromhex(branch['operand_hex'])
    need(len(raw) == (1 if branch['opcode'] == '0x2c' else 4), 'getter分支operand长度')
    target = branch['offset'] + 1 + len(raw) + int.from_bytes(raw, 'little', signed=True)
    stop = unique([j for j, x in enumerate(gc) if x['offset'] == target], 'getter下个分支边界')
    need(stop > compare+2, 'getter空/倒向命中分支')
    hit = gc[compare+2:stop]
    own = unique([t for t, f in fields.items() if f['name'] == f'm{index}___mv0___name__'], '活globals name字段')
    need(hit[-1]['opcode'] == '0x2a', 'getter命中没有终止return')
    if len(hit) == 5 and runtime(hit[3], 'PyModules', 'LoadAttribute'):
        need(hit[0]['opcode'] == '0x7e' and hit[0].get('field_token') == own
             and hit[1].get('string') == module_name and hit[2].get('string') == '__name__', 'ValueSentinel value槽/guard错误')
        guard, bound = 'ValueSentinel', None
    elif len(hit) == 6 and runtime(hit[3], 'PyModules', 'CheckBound'):
        need(hit[0]['opcode'] == hit[4]['opcode'] == '0x7e' and hit[4].get('field_token') == own
             and hit[1].get('string') == module_name and hit[2].get('string') == '__name__', 'BoundFlag value槽/guard错误')
        bound = hit[0].get('field_token')
        need(bound in fields and fields[bound]['signature_hex'] == '0602'
             and fields[bound]['name'].startswith(f'm{index}___bound_'), 'BoundFlag不是本单元bool槽')
        mc = code(main)
        push = unique([j for j, x in enumerate(mc) if runtime(x, 'PyTraceback', 'PushFrameWithOrigin')], 'typed-name实际帧')
        marker = unique([j for j, x in enumerate(mc[:push]) if runtime(x, 'PyTraceback', 'CreateModuleExecutionSourceWithCode')], 'typed-name入口marker')
        need(not any(x['opcode'] == '0x80' and x.get('field_token') == own
                     for x in mc[marker+1:push]), 'typed-name不得在压帧前发布')
        store = next((j for j in range(push+1, len(mc)-2)
                      if mc[j]['opcode'] == '0x80' and mc[j].get('field_token') == own), None)
        need(store is not None, 'typed-name压帧后缺实际name发布')
        # build.rs先emit_frame_push再build_stmt；只允许首个name发布前的纯load/store和SetLine。
        pure = {0x00,0x72,0x06,0x07,0x08,0x09,0x0a,0x0b,0x0c,0x0d,0x11,0x13,0xfe0c,0xfe0e}
        need(all(int(x['opcode'],16) in pure or number(x) is not None
                 or runtime(x,'PyTraceback','SetLine') for x in mc[push+1:store]), 'typed-name发布前有分支/其他副作用')
        published, _ = expression(mc, store-1)
        need(published == ('string',module_name), 'typed-name首个发布不是实际模块初值')
        need(number(mc[store+1]) == 1 and mc[store+2]['opcode'] == '0x80'
             and mc[store+2].get('field_token') == bound, 'getter bound槽不是入口value实际配对发布槽')
        guard = 'BoundFlag'
    else:
        raise ValueError('getter命中分支超出既有ValueSentinel/BoundFlag形状')
    return {'getter': getter['token'], 'guard': guard, 'value_field': own, 'bound_field': bound,
            'comparison_offset': gc[compare]['offset'], 'next_branch_offset': target}


def native_locations(decoded, expectation, manifest):
    """仅固定native_consumers审核域；manifest绑定同一PE，不能由缺文件自动进入。"""
    fixed = json.loads(Path(__file__).with_name('fixed_expectations.json').read_text())['cases']['native_consumers']
    need(decoded.get('case_id') == 'native_consumers' and expectation == fixed, 'Native只接受固定native_consumers预期')
    need(isinstance(manifest, dict) and manifest.get('schema_version') == 2
         and manifest.get('kind') == 'ryupy-native-aot-artifact', 'Native manifest类型/schema错误')
    need(manifest.get('target') == {'architecture': 'x64', 'native_format': 'ELF', 'rid': 'linux-x64'}, 'Native目标不符')
    for key, count in [('build_inputs', 2), ('modules', 3), ('sources', 3), ('module_locations', 2)]:
        rows = manifest.get(key)
        need(isinstance(rows, list) and len(rows) == count and all(isinstance(row, dict) for row in rows),
             'Native manifest表缺失/类型/数量错误: ' + key)
    inputs = manifest['build_inputs']
    need({row.get('role') for row in inputs} == {'compiled_managed_input', 'runtime_managed_input'}, 'Native输入角色重复/缺失')
    managed = unique([row for row in inputs if row['role'] == 'compiled_managed_input'], 'Native实际managed输入')
    need(managed.get('relative_path') == 'managed-entry.dll' and managed.get('sha256') == decoded['sha256']
         and isinstance(decoded['sha256'], str) and len(decoded['sha256']) == 64
         and all(c in '0123456789abcdef' for c in decoded['sha256']), 'Native manifest与实际PE SHA不符')
    need(type(managed.get('size')) is int and type(decoded.get('pe_bytes')) is int
         and managed['size'] == decoded['pe_bytes'] > 0, 'Native manifest与实际PE字节数不符')
    packages = ['', 'sp_pkg', 'sp_pkg']
    logical = ['entry/entry.py', 'modules/sp_pkg/__init__.py', 'modules/sp_pkg/subject.py']
    locations = [(0, ''), (2, '_internal/resources/v1/modules/sp_pkg/__init__.py'),
                 (2, '_internal/resources/v1/modules/sp_pkg/subject.py')]
    expected_sources, expected_locations = [], []
    owner = 'RyuPy.Generated.Module'
    for index, module in enumerate(fixed['modules']):
        source = manifest['sources'][index]
        row = manifest['modules'][index]
        need(type(source.get('module_id')) is int and type(row.get('id')) is int, 'Native模块ID必须为整数')
        expected_sources.append({'module_id': index, 'module_name': module['name'],
            'package': packages[index], 'logical_source': logical[index], 'compiled_text_sha256': module['sha256']})
        legacy = module['environment'] == 'LegacySingleton'
        need(row.get('id') == index and row.get('name') == module['name']
             and row.get('environment_reason') == ('aot-not-migrated' if legacy else 'explicit-instance'), 'Native模块映射/环境不符')
        need(row.get('init') == {'owner': owner, 'name': f'm{index}___module_main',
             'signature': 'static void ()' if legacy else 'static void ([System.Runtime]System.Object)'}, 'Native模块入口不符')
        for access, signature in [('getter', 'static [System.Runtime]System.Object (string)'),
                                  ('setter', 'static void ([RyuPy.Runtime]RyuPy.Runtime.PyModule, string, [System.Runtime]System.Object)')]:
            expected = {'owner': owner, 'name': f'__mod_{"get" if access == "getter" else "set"}_{index}',
                        'signature': signature} if legacy else None
            need(access in row and row[access] == expected, 'Native模块accessor/环境不符')
        if index:
            expected_locations.append({'module_id': index, 'module_name': module['name'],
                'kind': 'executable_relative', 'relative_file': locations[index][1]})
    need(manifest['sources'] == expected_sources, 'Native sources映射/固定输入SHA不符')
    need(all(type(row.get('module_id')) is int for row in manifest['module_locations'])
         and manifest['module_locations'] == expected_locations, 'Native module_locations不是精确两处相对位置')
    need(manifest.get('resources') == [] and isinstance(manifest.get('scope'), dict)
         and manifest['scope'].get('resources_included') is False, 'Native空锚点不能携带源码资源')
    return locations, packages


def registration_location(part, module, native_location=None, package=None):
    """从实际注册的连续literal/kind/path实参读取位置；JIT仍核存在的绝对源码及SHA。"""
    positions = [j for j, item in enumerate(part) if item['opcode'] == '0x72'][-4:]
    need(len(positions) == 4, '模块注册缺四个literal')
    start = positions[0]
    need(positions == [start, start+1, start+2, start+4], '模块注册位置参数不是连续literal/kind/path')
    strings = [part[j]['string'] for j in positions]
    kind = number(part[start+3])
    need(strings[0] == module['name'] and strings[2] == module['file'], '模块注册literal不符')
    if native_location is None:
        path = Path(strings[3])
        need(kind == 1 and path.is_absolute() and path.is_file()
             and hashlib.sha256(path.read_bytes()).hexdigest() == module['sha256'], '实际JIT注册Kind1源路径/SHA不符')
    else:
        need((kind, strings[3]) == native_location and strings[1] == package, '实际Native注册kind/path/package不符')
    return {'kind': kind, 'runtime_path': strings[3]}


def check(decoded, expectation, fixture_root, role, *, native_manifest=None):
    modules, methods, init, bindings, evidence = locate(decoded, expectation, role)
    fields = decoded['fields']
    for module in modules:
        path = Path(fixture_root) / module['fixture']
        need(path.is_file() and hashlib.sha256(path.read_bytes()).hexdigest() == module['sha256'], '固定fixture SHA不符')
    need(role == 'new', '旧定位仅为checker控制，不签准新source协议')
    native = native_locations(decoded, expectation, native_manifest) if native_manifest is not None else None
    source_init = unique([m for m in methods.values() if m['method']['owner'] == 'RyuPy.Generated.Module'
                          and m['method']['name'] == '__init_code_sources'], 'source冷入口')
    instructions = code(init)
    need(not any(runtime(x, 'PyOps', 'FunctionDefinition') for x in instructions), '新冷绑定退旧helper')
    need(instructions[0].get('target', {}).get('name') == '__init_literals'
         and instructions[0].get('token') in methods and instructions[1].get('token') == source_init['token']
         and instructions[0]['opcode'] == instructions[1]['opcode'] == '0x28', 'literal→source→signature顺序')
    source_tokens = {}
    for index, module in enumerate(modules):
        name = f'm{index}___code_source'
        token = unique([token for token, row in fields.items() if row['name'] == name], 'own source字段')
        source_tokens[index] = token
    need(sum(row['name'].endswith('___code_source') for row in fields.values()) == len(modules), '额外source字段')
    # source冷入口为固定无分支直线：每条源构造实参+真正getter MethodDef指针+写唯一字段。
    emitted = code(source_init)
    position = 0
    getters = {}
    for index, module in enumerate(modules):
        legacy = module['environment'] == 'LegacySingleton'
        width = 7 if legacy else 5
        part = emitted[position:position+width]
        need(len(part) == width and part[0].get('string') == module['name']
             and part[1].get('string') == module['file'] and part[2]['opcode'] == '0x14', 'source module/file/空context错误')
        if legacy:
            pointer = part[3]
            need(pointer['opcode'] == '0xfe06' and pointer.get('token') in methods, 'getter不是实际MethodDef指针')
            getter = methods[pointer['token']]
            need(getter['method']['name'] == f'__mod_get_{index}', 'source错误getter')
            need(part[4]['opcode'] == '0x73' and part[4].get('target', {}).get('owner') == 'RyuPy.Runtime.PyModuleGet', 'getter委托错误')
            getters[index] = getter['token']
            main = unique([m for m in methods.values() if m['method']['name'] == f'm{index}___module_main'], 'getter对应入口')
            getter_name(getter, fields, index, module['name'], main)
        factory = 'CreateLegacyCodeSourceV1' if legacy else 'CreateCodeSource'
        need(runtime(part[-2], 'PyTraceback', factory) and part[-1]['opcode'] == '0x80'
             and part[-1].get('field_token') == source_tokens[index], 'source调用/存储错绑')
        position += width
    need(len(emitted) == position + 1 and emitted[-1]['opcode'] == '0x2a', 'source冷路有额外求值/调用')
    for method in methods.values():
        if method['token'] != source_init['token'] and method.get('body'):
            need(not any(x['opcode'] == '0x80' and x.get('field_token') in source_tokens.values()
                         for x in code(method)), 'source字段在冷入口之外被重写')
    for field, binding in bindings.items():
        index = binding['key'][0]
        source = binding['source_instruction']
        need(source['opcode'] == '0x7e' and source.get('field_token') == source_tokens[index], 'signature关联错误source')
    # 实际注册调用把模块literal、入口及getter指针与同一固定单元相连，不看unused MemberRef。
    register_evidence = []
    previous = 0
    for k, ins in enumerate(instructions):
        name = ins.get('target', {}).get('name')
        if not (runtime(ins, 'PyModules', 'RegisterSourceLocated') or runtime(ins, 'PyModuleInstances', 'RegisterInstanceSourceLocated')
                or runtime(ins, 'PyModules', 'RegisterInstanceSourceLocated')):
            continue
        part = instructions[previous:k]
        index = len(register_evidence)
        need(index < len(modules), '额外真实模块注册')
        module = modules[index]
        location = registration_location(part, module, native[0][index] if native else None,
                                         native[1][index] if native else None)
        ptrs = [x['token'] for x in part if x['opcode'] == '0xfe06']
        legacy = module['environment'] == 'LegacySingleton'
        ptrs = ptrs[-3:] if legacy else ptrs[-1:]
        need(len(ptrs) == (3 if legacy else 1) and all(t in methods for t in ptrs), '注册缺实际MethodDef')
        main = methods[ptrs[0]]
        need(main['method']['name'] == f'm{index}___module_main', '注册入口错单元')
        if legacy:
            need(name == 'RegisterSourceLocated' and ptrs[1] == getters[index], '注册/source getter不一致')
        else:
            need(name == 'RegisterInstanceSourceLocated', 'Instance退回Legacy注册')
        mc = code(main)
        marker = unique([j for j, x in enumerate(mc) if runtime(x, 'PyTraceback', 'CreateModuleExecutionSourceWithCode')], 'ModuleMain实际marker')
        push = unique([j for j, x in enumerate(mc) if runtime(x, 'PyTraceback', 'PushFrameWithOrigin')], 'ModuleMain实际origin帧')
        origin, last = expression(mc, push-1)
        function, last = expression(mc, last)
        filename, last = expression(mc, last)
        need(marker < push and origin == ('module-marker', ('field', source_tokens[index]), ('null',) if legacy else ('argument', 0))
             and function == ('string', '<module>') and filename == ('string', module['file']), 'ModuleMain帧source/context/literal错误')
        need(not any(runtime(x, 'PyTraceback', 'PushFrame') for x in mc), 'ModuleMain退旧帧')
        register_evidence.append({'unit': index, 'module': module['name'], 'source_field': source_tokens[index],
                                  'main': main['token'], 'getter': getters.get(index), 'marker_offset': mc[marker]['offset'],
                                  'push_offset': mc[push]['offset'], 'location': location})
        previous = k+1
    need(len(register_evidence) == len(modules), '模块注册覆盖不全')
    for field, binding in bindings.items():
        expected, core = binding['definition'], binding['core']
        if expected['generator']:
            continue
        ins = code(core)
        push = unique([j for j, x in enumerate(ins) if runtime(x, 'PyTraceback', 'PushFrameWithOrigin')], '普通实际帧')
        origin, previous = expression(ins, push-1)
        function, previous = expression(ins, previous)
        filename, previous = expression(ins, previous)
        signature = bytes.fromhex(core['method']['signature_hex'])
        need(signature[0] == 0 and signature[1] < 128, '固定core签名超出本门有限域')
        if expected['mode'] == 'dynamic':
            thunk = code(binding['thunk'])
            self_arg = int(modules[binding['key'][0]]['environment'] == 'InstanceGlobals')
            for at, call in enumerate(thunk):
                if call['opcode'] == '0x28' and call.get('token') == core['token']:
                    actual_self, _ = expression(thunk, at-1)
                    need(actual_self == ('argument', self_arg), 'thunk没有传实际PyFunction自身')
        wanted = ('field', field) if expected['mode'] == 'static' else ('argument', signature[1]-1)
        need(origin == wanted and function == ('string', expected['name']) and
             filename == ('string', modules[binding['key'][0]]['file']), 'Static own signature或Dynamic self帧错误')
        need(not any(runtime(x, 'PyTraceback', 'PushFrame') for x in ins), '普通函数退旧帧')
    return {'admitted': True, 'role': role, 'pe_sha256': decoded['sha256'],
            'location_mode': 'native_consumers_aot' if native else 'jit_files',
            'unit_link_mapping_verified': True, 'frame_carriers_verified': True,
            'verification_scope': '固定fixture关键source/getter、词法signature→真实thunk/core、ModuleMain/Dynamic/Static origin操作数；非通用全IR或Runtime协议证明。',
            'modules': register_evidence, 'definitions': evidence,
            'generator_state_machine_proof': '不在本检查器重证；沿用既有测试并与本批实际语义输出联合验收。',
            'product_accepted': False}
