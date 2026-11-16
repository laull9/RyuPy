"""复用冻结ECMA读取器读取实际PE全部字段、方法、调用；不加载程序集、不运行产品。"""
import hashlib
import json
from pathlib import Path
import runpy
import sys

BASE = Path(__file__).resolve().with_name('pe_metadata.py')
BASE_SHA = 'a43bf4bb9e89ceb169b89a67c8817e8cf2312429cb251a0f36f1b9da8f469bc3'
assert len(sys.argv) == 4, '参数为实际PE路径、old/new角色、case-id'
role, case_id = sys.argv[2:]
assert role in ("old", "new")
pe_path = Path(sys.argv[1]).resolve(strict=True)
assert hashlib.sha256(BASE.read_bytes()).hexdigest() == BASE_SHA
# 已核底座只读取sys.argv[1]，不含程序集加载、进程创建或任何写文件。
saved_argv = sys.argv[:]
sys.argv = [str(BASE), str(pe_path)]
try:
    meta = runpy.run_path(str(BASE))
finally:
    sys.argv = saved_argv
b, tables, string = meta['b'], meta['tables'], meta['string']
nested = {r[0]: r[1] for r in tables[41]}
method_owner = {}
for rid, row in enumerate(tables[2], 1):
    end = tables[2][rid][5] if rid < len(tables[2]) else len(tables[6]) + 1
    for method_rid in range(row[5], end):
        method_owner[method_rid] = rid


def type_name(table, rid):
    assert table in (1, 2, 27) and 1 <= rid <= len(tables[table])
    row = tables[table][rid - 1]
    if table == 27:
        return 'TypeSpec:' + meta['signature'](row[0])
    name, namespace = string(row[1]), string(row[2])
    if table == 2 and rid in nested:
        return type_name(2, nested[rid]) + '+' + name
    if table == 1 and row[0] & 3 == 3:
        return type_name(1, row[0] >> 2) + '+' + name
    return (namespace + '.' if namespace else '') + name


def method_ref(token):
    table, rid = token >> 24, token & 0xffffff
    assert table in (6, 10, 43) and 1 <= rid <= len(tables[table])
    row = tables[table][rid - 1]
    if table == 43:
        parent = row[0]
        result = method_ref(((10 if parent & 1 else 6) << 24) | (parent >> 1))
        return {**result, 'method_spec': meta['signature'](row[1])}
    if table == 6:
        owner = type_name(2, method_owner[rid])
        name, sig = string(row[3]), meta['signature'](row[4])
    else:
        parent, tag = row[0] >> 3, row[0] & 7
        if tag in (0, 1, 4):
            owner = type_name({0: 2, 1: 1, 4: 27}[tag], parent)
        elif tag == 3:
            owner = method_ref(0x06000000 | parent)['owner']
        else:
            assert tag == 2
            owner = 'ModuleRef:' + string(tables[26][parent - 1][0])
        name, sig = string(row[1]), meta['signature'](row[2])
    return {'owner': owner, 'name': name, 'signature_hex': sig}


def user_string(token):
    assert token >> 24 == 0x70
    start, size = meta['streams']['#US']
    offset = token & 0xffffff
    assert offset < size
    n, prefix = meta['compressed'](start + offset)
    assert n >= 1 and offset + prefix + n <= size
    return b[start + offset + prefix:start + offset + prefix + n - 1].decode('utf-16-le')


# ECMA-335有界操作数宽度；未知opcode立即失败，不能逐byte搜索call token。
zero = set(range(0x02, 0x0e)) | set(range(0x14, 0x1f)) | set(range(0x46, 0x6f))
zero |= set(range(0x82, 0x8c)) | set(range(0x90, 0xa3)) | set(range(0xb3, 0xbb))
zero |= set(range(0xd1, 0xdd)) | {0x00, 0x01, 0x25, 0x26, 0x2a, 0x76, 0x7a, 0x8e, 0xc3, 0xdf, 0xe0}
short = set(range(0x0e, 0x14)) | {0x1f}
short_branch = set(range(0x2b, 0x38)) | {0xde}
long_branch = set(range(0x38, 0x45)) | {0xdd}
token_ops = {0x27, 0x28, 0x29, 0x6f, 0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x79,
             0x7b, 0x7c, 0x7d, 0x7e, 0x7f, 0x80, 0x81, 0x8c, 0x8d, 0x8f, 0xa3, 0xa4, 0xa5, 0xc2, 0xc6, 0xd0}
ext_zero = {0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x0f, 0x11, 0x13, 0x14, 0x17, 0x18, 0x1a, 0x1d, 0x1e}
ext_token = {0x06, 0x07, 0x15, 0x16, 0x1c}
call_ops = {0x27, 0x28, 0x6f, 0x73, 0xfe06, 0xfe07}


def body(rid):
    row = tables[6][rid - 1]
    if not row[0]:
        return None
    start = meta['off'](row[0])
    tag = b[start] & 3
    if tag == 2:
        head, size = 1, b[start] >> 2
    else:
        assert tag == 3
        flags = int.from_bytes(b[start:start + 2], 'little')
        head = (flags >> 12) * 4
        assert head >= 12
        size = int.from_bytes(b[start + 4:start + 8], 'little')
    il = b[start + head:start + head + size]
    assert len(il) == size
    p, ins, branches = 0, [], []
    while p < size:
        at, op = p, il[p]
        p += 1
        if op == 0xfe:
            assert p < size
            x = il[p]
            p += 1
            op = 0xfe00 | x
            width = 0 if x in ext_zero else 4 if x in ext_token else 2 if x in range(0x09, 0x0f) else 1 if x in (0x12, 0x19) else None
        else:
            width = 0 if op in zero else 1 if op in short | short_branch else 4 if op in token_ops | long_branch | {0x20, 0x22} else 8 if op in (0x21, 0x23) else None
        if op == 0x45:
            assert p + 4 <= size
            count = int.from_bytes(il[p:p + 4], 'little')
            width = 4 + 4 * count
        assert width is not None, ('unknown opcode', hex(op), at)
        assert p + width <= size
        operand = il[p:p + width]
        p += width
        item = {'offset': at, 'opcode': hex(op), 'operand_hex': operand.hex()}
        if op in call_ops:
            token = int.from_bytes(operand, 'little')
            item.update(token=hex(token), target=method_ref(token))
        if op == 0x72:
            item['string'] = user_string(int.from_bytes(operand, 'little'))
        if op in short_branch | long_branch:
            branches.append(p + int.from_bytes(operand, 'little', signed=True))
        if op == 0x45:
            branches.extend(p + int.from_bytes(operand[i:i + 4], 'little', signed=True) for i in range(4, width, 4))
        ins.append(item)
    boundaries = {i['offset'] for i in ins}
    assert all(target in boundaries for target in branches), ('bad branch boundary', rid)
    return {'rva': row[0], 'header_bytes': head, 'il_bytes': size, 'il_sha256': hashlib.sha256(il).hexdigest(), 'instructions': ins}


# 读取真实FieldDef；不凭字段名推断owner，只把实际token边交给独立unit映射审核。
fields = {hex(0x04000000 | rid): {'name': string(row[1]), 'flags': row[0],
          'signature_hex': meta['signature'](row[2])}
          for rid, row in enumerate(tables[4], 1)}
methods = []
for rid, row in enumerate(tables[6], 1):
    content = body(rid)
    if content:
        for ins in content['instructions']:
            if ins['opcode'] in ('0x7b', '0x7c', '0x7d', '0x7e', '0x7f', '0x80'):
                token = hex(int.from_bytes(bytes.fromhex(ins['operand_hex']), 'little'))
                assert token in fields, ('未解析外部字段，停止不得猜测', token)
                ins['field_token'] = token
                ins['field'] = fields[token]
    methods.append({'token': hex(0x06000000 | rid), 'method': method_ref(0x06000000 | rid),
                    'flags': row[2], 'body': content})
checks = {}
if role == 'new':
    source_init = [m for m in methods if m['method']['name'] == '__init_code_sources']
    checks['exactly_one_source_initializer'] = len(source_init) == 1
    calls = [i['target']['name'] for m in methods if m['body'] for i in m['body']['instructions'] if 'target' in i]
    checks['actual_legacy_source_v1_call'] = 'CreateLegacyCodeSourceV1' in calls
    # 各Legacy/Instance调用角色由fixed_gate依实际module/getter/field逐项核验。
    # 原J2完整三源没有def/lambda/class，ModuleMain无signature；不得为凑符号改原输入。
    signature_calls = calls.count('FunctionDefinitionWithSource')
    checks['actual_signature_source_association'] = signature_calls == 0 if case_id == 'functional_typeddict' else signature_calls > 0
    checks['actual_module_marker_call'] = 'CreateModuleExecutionSourceWithCode' in calls
if case_id in ('static_td', 'mixed_frames'):
    name = 'm0_out' if case_id == 'static_td' else 'm0_static_issue'
    found = [m for m in methods if m['method']['name'] == name]
    assert len(found) == 1, ('Static core定位不唯一', name)
    static = found[0]
    checks['static_numeric_abi_void_noargs'] = bool(static['flags'] & 0x10) and static['method']['signature_hex'] == '000001'
    calls = [i['target']['name'] for i in static['body']['instructions'] if 'target' in i]
    checks['static_push_role'] = calls.count('PushFrameWithOrigin' if role == 'new' else 'PushFrame') == 1
    checks['static_call_builtin'] = 'CallBuiltin' in calls
    checks['thunk_calls_actual_static_core'] = any(
        m['method']['name'].startswith('__thunk_') and m['body'] and any(
            i.get('token') == static['token'] and i['opcode'] == '0x28' for i in m['body']['instructions'])
        for m in methods)
    if role == 'new':
        checks['static_has_existing_signature_field_load'] = any(
            i['opcode'] == '0x7e' and 'field' in i for i in static['body']['instructions'])
result = {'path': str(pe_path), 'sha256': hashlib.sha256(b).hexdigest(),
          'parser_sha256': BASE_SHA, 'case_id': case_id, 'role': role, 'fields': fields,
          'methods': methods, 'finite_checks': checks, 'finite_checks_passed': all(checks.values()),
          'product_accepted': False,
          'mandatory_independent_review': '完整unit→link映射、own signature/source/getter/marker/self、热ABI及cold次序由PE-GATE.zh.md逐项审；字段存在不是证明'}
print(json.dumps(result, ensure_ascii=False, indent=2))
sys.exit(0 if all(checks.values()) else 2)
