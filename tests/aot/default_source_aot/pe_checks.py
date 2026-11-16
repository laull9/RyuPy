"""重建检查器，非旧文件逐字恢复。MIR给全集，emission只给slot，实际IL必须独立吻合。"""
import hashlib
import json
from collections import Counter
from checking import require

PREFIX = 'RyuPy.Runtime.'
REGISTERS = {'RegisterSourceLocated', 'RegisterCandidateSource',
             'RegisterInstanceSourceLocated', 'RegisterCandidateInstance'}
ROLES = ('module_init', 'module_getter', 'module_setter')
OTHER_ROLES = {'callable_thunk', 'generator_resume', 'exception_factory', 'declared_native_body'}


def signature(blob, types):
    raw = bytes.fromhex(blob); pos = 0
    def number():
        nonlocal pos
        require(pos < len(raw), '截短ABI')
        first = raw[pos]; pos += 1
        if first < 128: return first
        count, value = (1, first & 63) if first < 192 else (3, first & 31)
        require(first < 224 and pos + count <= len(raw), '坏压缩ABI')
        for byte in raw[pos:pos+count]: value = (value << 8) | byte
        pos += count
        return value
    def typ():
        tag = number()
        primitive = {1:'void', 2:'bool', 10:'long', 14:'string', 24:'nint', 28:'object'}
        if tag in primitive: return primitive[tag]
        if tag == 29: return typ()+'[]'
        require(tag in (17,18), '非预期ABI类型')
        coded = number(); table = {0:2, 1:1}.get(coded & 3)
        require(table is not None and coded >> 2, '坏ABI类型token')
        key = hex((table << 24) | (coded >> 2))
        require(key in types, '未知ABI类型')
        if tag == 17: return 'valuetype:'+types[key]
        if types[key] == 'System.Object':
            require(key in types.get('__trusted_object_tokens__', ()), 'Object类型尚未核真实程序集归属')
            return 'object'
        return types[key]
    convention = number(); count = number(); ret = typ(); args = tuple(typ() for _ in range(count))
    require(pos == len(raw), 'ABI尾部字节')
    return convention, ret, args


def validate_cold_abi(decoded, types):
    common = ('string', 'string', 'string', 'long', 'string', 'bool')
    legacy = common + ('bool',) + tuple(PREFIX+x for x in ('PyModuleInit','PyModuleGet','PyModuleSet'))
    instance = common + (PREFIX+'PyModuleExecute',)
    count = 0
    for method in decoded['methods']:
        for ins in (method.get('body') or {}).get('instructions', []):
            target = ins.get('target', {}); owner = target.get('owner'); name = target.get('name')
            if owner == PREFIX+'PyModules' and (name in REGISTERS or name == 'ConfigureOptionalSources'):
                require(ins['opcode'] == '0x28', '冷注册必须static call')
                args = ('string',) if name == 'ConfigureOptionalSources' else instance if 'Instance' in name else legacy
                require(signature(target['signature_hex'], types) == (0, 'void', args), '冷注册实际ABI不符')
                count += 1
            if owner in {PREFIX+x for x in ('PyModuleInit','PyModuleGet','PyModuleSet','PyModuleExecute')}:
                require(name == '.ctor' and ins['opcode'] == '0x73', '委托非真实构造')
                require(signature(target['signature_hex'], types) == (32, 'void', ('object','nint')), '委托ABI不符')
    require(count > 1, '缺冷注册/配置ABI')


def integer(ins):
    op = int(ins['opcode'], 16)
    if 0x15 <= op <= 0x1e: return op - 0x16
    require(op in (0x1f,0x20,0x21), '非整数常量')
    raw = bytes.fromhex(ins['operand_hex'])
    require(len(raw) == {0x1f:1,0x20:4,0x21:8}[op], '常量宽度不符')
    return int.from_bytes(raw, 'little', signed=True)


def cold_registration(decoded, oracle, roots, payload, types, layout):
    candidate = json.loads(payload)
    require(hashlib.sha256(payload.encode()).hexdigest() == oracle['candidate_payload_sha256'], '冷载荷与发射前MIR身份不同')
    require(candidate['schema_version'] == 3, '非v3')
    modules = oracle['modules']; sources = oracle['sources']; functions = oracle['functions']
    require([x['module_id'] for x in modules] == list(range(len(modules))), 'MIR模块ID不连续')
    require([x['module_id'] for x in sources] == list(range(len(modules))), '源码ID不连续')
    require(len({x['name'] for x in modules}) == len(modules), '重复模块名')
    fmap = {x['function_id']:x for x in functions}
    require(len(fmap) == len(functions), '重复FnId')
    mapping = oracle['source_to_emitted']
    require(all(x is None or type(x) is int for x in mapping), '非法source投影')
    require(sorted(x for x in mapping if x is not None) == list(range(len(modules))), 'source投影非双射')
    require(sum(x is None for x in mapping) == len(candidate['deferred']), 'Deferred孔洞不符')
    expected = []
    for module, source in zip(modules, sources):
        require(source['module_name'] == module['name'], 'MIR与来源不等')
        require(module['execution'] in ('LegacySingleton','InstanceGlobals'), '未知模块执行域')
        require(module['init_fn'] in fmap, '未知init FnId')
        expected.extend((module['module_id'], role) for role in
                        (ROLES if module['execution'] == 'LegacySingleton' else ROLES[:1]))
    methods = decoded['methods']; by_token = {m['token']:m for m in methods}
    require(len(by_token) == len(methods), '重复MethodDef')
    def locate(row):
        require(row is not None and type(row['slot']) is int, '无slot')
        choices = [m for m in methods if m['method']['owner'] == row['owner']]
        require(0 <= row['slot'] < len(choices), 'slot越界')
        found = choices[row['slot']]
        require(found['method']['name'] == row['name'] and found.get('body'), 'slot/name/body不符')
        return found
    # 按真实来源工厂块定位分区，不以方法名忽略任何多余root。
    from code_source_checks import source_blocks
    _, source_facts = source_blocks(decoded, types)
    source_positions = {b['root']: b for b in source_facts if b['legacy']}
    all_roles = [r for r in roots if r['role'] in ROLES]
    require(all(r['role'] in set(ROLES) | OTHER_ROLES for r in roots), '未知委托角色')
    selected=[]; observed_sources=[]
    module_ids={m['name']:m['module_id'] for m in modules}
    for row in all_roles:
        position=(locate(row['source'])['token'],row['instruction'])
        if position in source_positions:
            block=source_positions[position]
            require(row['role']=='module_getter' and row['module_id']==module_ids.get(block['module_name']) and
                    locate(row['target'])['token']==block['getter_token'], '来源root报告错归属')
            observed_sources.append(position)
        else:
            selected.append(row)
    require(Counter(observed_sources)==Counter(source_positions.keys()), '来源root报告缺失/重复')
    require(Counter((r['module_id'],r['role']) for r in selected)==Counter(expected), '冷注册root全集不符')
    positions = set(); targets = set(); records = []; getters = {}; hosts = set()
    for module, source in zip(modules, sources):
        mid = module['module_id']; name = module['name']; instance = module['execution'] == 'InstanceGlobals'
        rows = {r['role']:r for r in selected if r['module_id'] == mid}
        first = rows['module_init']; host = locate(first['source']); init = locate(first['target'])
        require(host['method']['name'] == '__init_functions', '注册不在唯一冷初始化器')
        require(init['method']['name'] == fmap[module['init_fn']]['codegen_name'], 'init非发射前FnId名字')
        hosts.add(host['token'])
        ins = host['body']['instructions']; at = first['instruction']; args = 6 if instance else 7
        require(type(at) is int and at >= args+1, '根指令越界')
        start = at-args-1; values = ins[start:start+args]
        loc = candidate['locations'][name]
        require(loc['registration'] in ('active','candidate') and loc['kind'] == 2, '编译源错误归属')
        for index, value in ((0,name),(1,loc['package']),(2,loc['file']),(4,loc['path'])):
            require(values[index]['opcode'] == '0x72' and values[index].get('string') == value, '冷注册来源常量不符')
        require(source['package'] == loc['package'], 'source package不符')
        require(integer(values[3]) == 2 and integer(values[-1]) == int(loc['is_package']), 'kind/package常量不符')
        if not instance: require(integer(values[5]) == 1, 'Legacy file-backed标记不符')
        for index, role in enumerate(ROLES[:1] if instance else ROLES):
            row = rows[role]; target = locate(row['target']); pos = at+3*index
            require(locate(row['source'])['token'] == host['token'] and row['instruction'] == pos, '委托序列不连续')
            expected_abi = ('void', ('object',)) if instance else {
                'module_init': ('void', ()), 'module_getter': ('object', ('string',)),
                'module_setter': ('void', (PREFIX+'PyModule', 'string', 'object'))}[role]
            require(type(target.get('flags')) is int and target['flags'] & 0x10, '委托目标非static MethodDef')
            require(signature(target['method']['signature_hex'], types) == (0, *expected_abi), '委托目标完整ABI不符')
            require(pos+1 < len(ins) and ins[pos-1]['opcode'] == '0x14', '委托缺null接收者')
            require(ins[pos]['opcode'] == '0xfe06' and ins[pos].get('token') == target['token'], '实际ldftn目标不符')
            ctor = ins[pos+1]; delegate = 'PyModuleExecute' if instance else {'module_init':'PyModuleInit','module_getter':'PyModuleGet','module_setter':'PyModuleSet'}[role]
            require(ctor['opcode'] == '0x73' and ctor.get('target',{}).get('owner') == PREFIX+delegate and ctor['target']['name'] == '.ctor', '实际newobj目标不符')
            key = (host['token'],pos); require(key not in positions and target['token'] not in targets, '重复根/复用target')
            positions.add(key); targets.add(target['token'])
            if role == 'module_getter': getters[mid] = target
        end = at + (3 if instance else 9)-1
        register = ('RegisterCandidateInstance' if instance else 'RegisterCandidateSource') if loc['registration']=='candidate' else ('RegisterInstanceSourceLocated' if instance else 'RegisterSourceLocated')
        require(end < len(ins) and ins[end]['opcode']=='0x28' and ins[end].get('target',{}).get('owner')==PREFIX+'PyModules' and ins[end]['target']['name']==register, '实际注册边不符')
        records.append((host['token'],end))
    actual_positions = {(m['token'],i) for m in methods for i,ins in enumerate((m.get('body') or {}).get('instructions',[])) if ins['opcode']=='0xfe06' and ins.get('token') in targets}
    from code_source_checks import verify_sources
    extra = verify_sources(decoded, oracle, types, layout, getters, hosts)
    require(actual_positions == positions | extra, '多余/缺失实际源码根全集')
    actual_regs = []; configure = []
    for method in methods:
        ins = (method.get('body') or {}).get('instructions',[])
        for pos, inst in enumerate(ins):
            target = inst.get('target',{})
            if target.get('owner') != PREFIX+'PyModules': continue
            if target.get('name') in REGISTERS: actual_regs.append((method['token'],pos))
            if target.get('name') == 'ConfigureOptionalSources':
                require(inst['opcode']=='0x28' and pos>0 and ins[pos-1].get('string')==payload and ins[pos-1]['opcode']=='0x72', '配置未消费原载荷')
                configure.append((method['token'],pos))
    require(Counter(actual_regs)==Counter(records) and len(configure)==1, '注册/Configure全集不符')
    require(all(token==configure[0][0] and pos<configure[0][1] for token,pos in records), 'Configure先于注册')
    return {'modules':len(modules),'static_roots':len(positions), 'code_source_roots':len(extra),'registrations':len(records),
            'pe_sha256':decoded['sha256'],'scope':'实际IL静态根；非Native执行/trim存活证明；ABI另门'}
