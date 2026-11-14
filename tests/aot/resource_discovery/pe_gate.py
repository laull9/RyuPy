"""两固定pkg fixture的真实PE冷链；复用原解码结果，不把名字出现当来源证明。"""


def need(value, message):
    if not value:
        raise ValueError(message)


def only(rows, message):
    need(len(rows) == 1, message)
    return rows[0]


def code(method):
    need(method.get('body') is not None, '实际MethodDef无body')
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


def out_binding(methods, init, core, fixture):
    """只核两份固定无参out：实际Create/词法/首行/thunk闭链，不泛化数据流。"""
    need(fixture in ('original', 'resource'), '未知固定fixture')
    firstline, body_line = (28, 29) if fixture == 'original' else (15, 16)
    at = only([i for i, ins in enumerate(init) if i >= 4
               and runtime(ins, 'PyOps', 'FunctionDefinitionWithSource')
               and init[i - 3].get('string') == 'out'], 'out必须唯一真实词法关联')
    need(at >= 13 and init[at - 3]['opcode'] == '0x72'
         and number(init[at - 2]) == firstline, 'out词法首行改变')
    create = init[at - 13:at - 4]
    need(create[0]['opcode'] == '0x72' and create[0].get('string') == 'out'
         and all(number(create[i]) == 0 and create[i + 1]['opcode'] == '0x8d' for i in (1, 3, 5))
         and runtime(create[7], 'PySignature', 'Create') and create[8]['opcode'] == '0x80',
         'out不是实际无参Create立即写入signature')
    signature = create[8]['field_token']
    need(init[at - 4]['opcode'] == '0x7e' and init[at - 4].get('field_token') == signature
         and sum(x['opcode'] == '0x80' and x.get('field_token') == signature for x in init) == 1,
         'out词法关联未使用自身唯一Create字段')
    need(init[at - 1]['opcode'] == '0x7e', 'out来源不是实际字段')
    thunks = [m for m in methods.values() if m['method']['owner'] == 'RyuPy.Generated.Module'
              and m['method']['name'].startswith('__thunk_')
              and any(x['opcode'] == '0x7e' and x.get('field_token') == signature for x in code(m))]
    thunk = only(thunks, 'out signature必须唯一连接实际thunk')
    targets = {x['token'] for x in code(thunk) if x['opcode'] == '0x28' and x.get('token') in methods}
    need(targets == {core['token']}, 'out实际thunk未指向自身core')
    body = code(core)
    frame = only([i for i, ins in enumerate(body) if runtime(ins, 'PyTraceback', 'PushFrameWithOrigin')],
                 'Static必须唯一真实压帧')
    need(frame >= 3 and body[frame - 3].get('string') == '__init__.py'
         and body[frame - 2].get('string') == 'out' and body[frame - 1]['opcode'] == '0x7e'
         and body[frame - 1].get('field_token') == signature, 'out压帧未使用自身定义signature')
    lines = [i for i, ins in enumerate(body) if runtime(ins, 'PyTraceback', 'SetLine')]
    need(lines and lines[0] > 0 and number(body[lines[0] - 1]) == body_line, 'out实际首行不符')
    return signature, init[at - 1]['field_token'], thunk['token'], firstline, body_line


def bind_native_inputs(manifest, expected):
    """把固定发布manifest精确绑定到已审PE与本轮冻结Runtime的SHA及大小。"""
    rows = manifest['build_inputs']
    need(len(rows) == 2 and len({r['role'] for r in rows}) == 2, 'Native输入角色缺失/重复')
    actual = {row['role']: {'sha256': row['sha256'], 'size': row['size']} for row in rows}
    need(set(actual) == {'compiled_managed_input', 'runtime_managed_input'}
         and all(type(row['size']) is int and row['size'] > 0 for row in actual.values())
         and actual == expected, 'Native build_inputs与所审PE/冻结Runtime的SHA或大小不符')
    return actual


def check(decoded, fixture):
    methods = {method['token']: method for method in decoded['methods']}
    def method(name):
        return only([m for m in methods.values() if m['method']['owner'] == 'RyuPy.Generated.Module'
                     and m['method']['name'] == name], 'MethodDef不唯一: ' + name)
    core = method('m1_out')
    need(core['flags'] & 0x10 and core['method']['signature_hex'] == '000001', 'out不是真Static void()')
    init = code(method('__init_functions'))
    signature, source, thunk, firstline, body_line = out_binding(methods, init, core, fixture)
    sources = code(method('__init_code_sources'))
    need(len(sources) == 22 and sources[-1]['opcode'] == '0x2a', '固定3包冷创建形状改变')
    fields = []
    for unit, name in enumerate(('__main__', 'pkg', 'pkg.data')):
        part = sources[unit * 7:(unit + 1) * 7]
        need(part[0].get('string') == name and part[1].get('string') == ('entry.py' if unit == 0 else '__init__.py'), 'source词法单元改变')
        need(part[2]['opcode'] == '0x14' and part[3]['opcode'] == '0xfe06'
             and part[3].get('token') == method('__mod_get_' + str(unit))['token'], 'source实际getter错绑')
        need(part[4]['opcode'] == '0x73' and part[4].get('target', {}).get('owner') == 'RyuPy.Runtime.PyModuleGet', 'getter委托错绑')
        need(runtime(part[5], 'PyTraceback', 'CreateLegacyCodeSourceV1') and part[6]['opcode'] == '0x80', '缺V1冷能力或实际字段写入')
        fields.append(part[6]['field_token'])
    need(len(set(fields)) == 3 and source == fields[1], 'Static own signature不属于pkg source')
    main, data = method('m1___module_main'), method('m2___module_main')
    need(any(i['opcode'] == '0x28' and i.get('token') == core['token'] for i in code(main)), '包入口未直接call真实Static core')
    registrations = [i for i in init if runtime(i, 'PyModules', 'RegisterSourceLocated')]
    need(len(registrations) == 3, '必须真实登记3个Legacy源码模块')
    # 注册入口指针逐一真实连接ModuleMain；data不得被编译器提前call。
    starts = 0
    for unit, register in enumerate(registrations):
        end = init.index(register, starts)
        part = init[starts:end]
        target = method('m' + str(unit) + '___module_main')['token']
        need(sum(i['opcode'] == '0xfe06' and i.get('token') == target for i in part) == 1, 'registration未绑定自身ModuleMain')
        starts = end + 1
    edges = [i for m in methods.values() if m.get('body') for i in code(m) if i.get('token') == data['token']]
    need(len(edges) == 1 and edges[0]['opcode'] == '0xfe06', 'data必须只作为注册委托，不得预调用')
    return dict(pe_sha256=decoded['sha256'], static_signature=signature, source_field=source,
                core_token=core['token'], thunk_token=thunk, definition_firstline=firstline,
                body_firstline=body_line, fixture=fixture, registered_modules=3, lexical_v1_verified=True,
                data_only_registered=True, scope='固定两fixture的实际冷来源/Static调用链，非通用形式化证明')
