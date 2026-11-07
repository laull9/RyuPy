#!/usr/bin/env python3
"""真实PE方法级审计。只放宽指令短/长编码和已证明的PIC删除后重编号。"""
import argparse
import bisect
import copy
import json
import pathlib
import subprocess

ROOT = pathlib.Path(__file__).resolve().parent
DLL = ROOT / 'bin/Debug/net10.0/PeAudit.dll'


def require(ok, message):
    if not ok:
        raise AssertionError(message)


def decode(path):
    return json.loads(subprocess.check_output(['dotnet', str(DLL), str(path)], text=True))


def op(ins):
    name, arg = ins['Op'], ins['Arg']
    if name.startswith('ldc.i4.'):
        tail = name[7:]
        return ('ldc.i4', -1 if tail == 'm1' else arg if tail == 's' else int(tail))
    for base in ('ldloc', 'stloc', 'ldarg', 'starg'):
        if name.startswith(base + '.'):
            tail = name[len(base) + 1:]
            return (base, arg if tail == 's' else int(tail))
    if name.endswith('.s') and (name.startswith('b') or name.startswith('leave')):
        name = name[:-2]
    return name, arg


def call_is(ins, cls, method):
    return ins['Op'] == 'call' and f'RyuPy.Runtime.{cls}::{method}[' in str(ins['Arg'])


def arguments(method):
    """顺序求值栈只跟踪来源；每个调用的实参来源必须可识别，不猜整数位置。"""
    stack, calls = [], {}
    for index, ins in enumerate(method['IL']):
        name = ins['Op']
        if name == 'dup':
            require(stack, 'dup栈空')
            stack.append(stack[-1])
            continue
        if ins['CallArgs'] is not None:
            n = ins['CallArgs']
        elif ins['Pop'] == 'Varpop':
            n = 1 if name == 'ret' and not method['Signature'].endswith('->Void') else 0
        else:
            n = 0 if ins['Pop'] == 'Pop0' else len(ins['Pop'].split('_'))
        # EH入口可隐式带异常对象；与PIC来源无关的未知值保留为-1。
        if len(stack) < n:
            stack = [-1] * (n - len(stack)) + stack
        args = stack[-n:] if n else []
        if n:
            del stack[-n:]
        if ins['CallArgs'] is not None:
            calls[index] = args
            push = int(ins['CallReturns'])
        else:
            require(ins['Push'] != 'Varpush', '未支持可变push')
            push = 0 if ins['Push'] == 'Push0' else len(ins['Push'].split('_'))
        origin = min([index] + [a for a in args if a >= 0])
        stack.extend([origin] * push)
        if name in ('br', 'br.s', 'leave', 'leave.s', 'ret', 'throw', 'endfinally'):
            stack = []
    return calls


def pic_sites(pe):
    result = []
    for mi, method in enumerate(pe['Methods']):
        for ci, origins in arguments(method).items():
            ins = method['IL'][ci]
            if 'RyuPy.Runtime.PyPic::' not in str(ins['Arg']):
                continue
            require(origins and origins[0] >= 0, 'PIC首参来源未知')
            site_index = origins[0]
            kind, value = op(method['IL'][site_index])
            require(kind == 'ldc.i4', 'PIC首参不是直接整数')
            result.append((mi, site_index, value, ci))
    ids = [s[2] for s in result]
    require(ids == list(range(len(ids))), f'全PE PIC编号非唯一连续递增：{ids}')
    return result


def branches(il):
    result = set()
    for ins in il:
        name, arg = op(ins)
        if name == 'switch':
            result.update(arg)
        elif name.startswith('b') and name not in ('box', 'break') or name == 'leave':
            result.add(arg)
    return result


def canonical(method, entries):
    """将目标字节偏移解析为恢复后的指令序号；删除段入口映射到下一条保留指令。"""
    original = method['IL']
    offsets = [i['Offset'] for i in original]
    labels = {}
    for new_index, (old_index, ins) in enumerate(entries):
        labels.setdefault(old_index, new_index)
    # 被删除段可能恰好为exit入口。除第一条外的删除段中间不得有分支目标。
    for old_index in range(len(original), -1, -1):
        if old_index == len(original):
            labels[old_index] = len(entries)
        elif old_index not in labels:
            labels[old_index] = labels[old_index + 1]
    def target(offset):
        pos = bisect.bisect_left(offsets, offset)
        require(pos == len(offsets) and offset == len(bytes.fromhex(method['Bytes'])) or pos < len(offsets) and offsets[pos] == offset, '分支不指向指令边界')
        return labels[pos]
    out = []
    for _, ins in entries:
        name, arg = op(ins)
        if name == 'switch':
            arg = [target(v) for v in arg]
        elif name.startswith('b') and name not in ('box', 'break') or name == 'leave':
            arg = target(arg)
        out.append((name, arg))
    eh = []
    for region in method['EH'] or []:
        e = dict(region)
        for start, length in [('TryOffset', 'TryLength'), ('HandlerOffset', 'HandlerLength')]:
            end = target(e[start] + e[length])
            e[start] = target(e[start])
            e[length] = end - e[start]
        if e['Kind'] == 'Filter':
            e['FilterOffset'] = target(e['FilterOffset'])
        eh.append(e)
    return out, eh


def verify_cfg(il, create, add, value):
    """从PE分支和协议调用独立重建P/H/B/X，拒绝在别的控制流位置插入三段。"""
    indices = {x['Offset']: i for i, x in enumerate(il)}
    require(op(il[create + 2])[0] == 'br', 'P末无goto')
    header = indices[op(il[create + 2])[1]]
    require(header == create + 3, 'H不是P后首块')
    require(call_is(il[header + 1], 'PyOps', 'IterNextOk'), 'H不是Next')
    iterator = op(il[header])
    ok = op(il[header + 2])
    require(iterator[0] == 'ldloc' and ok[0] == 'stloc' and op(il[header + 3]) == ('ldloc', ok[1]), 'H实参或条件不对')
    require(op(il[header + 4])[0] == 'brtrue' and op(il[header + 5])[0] == 'br', 'H不是单条件两出口')
    body = indices[op(il[header + 4])[1]]
    exit_ = indices[op(il[header + 5])[1]]
    require(body == header + 6 and exit_ == value - 1, 'B/X入口不对')
    require(op(il[body]) == iterator and call_is(il[body + 1], 'PyOps', 'IterValue'), 'B首不是同迭代器Current')
    require(op(il[add + 1]) == ('br', il[header]['Offset']) and add + 2 == exit_, 'B唯一回边不正确')
    for name in ('IterStart', 'IterNextOk', 'IterValue'):
        require(sum(call_is(x, 'PyOps', name) for x in il) == 1, '迭代协议调用数改变')
    start = next(i for i, x in enumerate(il) if call_is(x, 'PyOps', 'IterStart'))
    require(start < create - 1 and op(il[start + 1]) == ('stloc', iterator[1]), 'P未建立同一迭代器')
    require(sum(x['Op'] == 'ret' for x in il) == 1, '不是唯一返回')
    branch_indices = [i for i, x in enumerate(il) if op(x)[0].startswith('b') and op(x)[0] not in ('box', 'break') or op(x)[0] in ('switch', 'leave')]
    require(len(branch_indices) == 5 and branch_indices[:4] == [create + 2, header + 4, header + 5, add + 1], 'P/H/B有额外分支')
    require(all(x['Op'] not in ('throw', 'rethrow', 'leave', 'leave.s', 'switch') for x in il), '命中路径有额外退出')


def patterns(off, on):
    """严格识别三处holder改写，恢复原临时量及精确rhs；不忽略其它调用。"""
    a, b = off['IL'], on['IL']
    hits = [[i for i, x in enumerate(b) if call_is(x, 'PyNumericAccumulator', name)] for name in ('Create', 'Add', 'Value')]
    if not any(hits):
        return list(enumerate(b)), None
    # 旧range holder应在两侧原样存在；交给完整比较，而非当作新增命中。
    old = [[i for i, x in enumerate(a) if call_is(x, 'PyNumericAccumulator', name)] for name in ('Create', 'Add', 'Value')]
    if old == hits:
        return list(enumerate(b)), None
    require(all(len(h) == 1 for h in hits) and not any(old), '非唯一新增holder三联')
    create, add, value = [h[0] for h in hits]
    for index, signature in [(create, 'Create[0;0;1](Object)->Object'), (add, 'Add[0;0;2](Object,Object)->Void'), (value, 'Value[0;0;1](Object)->Object')]:
        require(b[index]['Arg'] == '[RyuPy.Runtime]RyuPy.Runtime.PyNumericAccumulator::' + signature, 'holder ABI不是原固定签名')
    total = op(b[create - 1]); holder = op(b[create + 1])
    require(total[0] == 'ldloc' and holder[0] == 'stloc', 'Create不是局部到新holder')
    total, holder = total[1], holder[1]
    require(on['Locals'] == off['Locals'] + ['[System.Runtime]System.Object'] and holder == len(off['Locals']), 'locals不是只尾增一个Object')
    require(op(b[value - 1]) == ('ldloc', holder) and op(b[value + 1]) == ('stloc', total), 'Value不是holder回写total')
    require(not off['EH'] and not on['EH'], '命中函数有EH')
    require(create < add < value, '三段顺序错误')
    verify_cfg(b, create, add, value)
    require(op(b[create + 2])[0] == 'br', 'Create未在P尾紧靠goto')
    require(b[value - 1]['Offset'] in branches(b), 'Value不在出口分支入口')
    origins = arguments(on)[add]
    require(len(origins) == 2 and op(b[origins[0]]) == ('ldloc', holder), 'Add第一参数不正确')
    add_start, rhs_start = origins
    require(rhs_start == add_start + 1, 'Add参数交错')
    rhs = [op(x) for x in b[rhs_start:add]]
    matches = []
    for ci, src in arguments(off).items():
        if a[ci]['Op'] != 'call' or a[ci]['Arg'] != '[RyuPy.Runtime]RyuPy.Runtime.PyPic::Binary[0;0;4](Int32,Int32,Object,Object)->Object' or len(src) != 4:
            continue
        site, code, left, right = src
        if op(a[code]) != ('ldc.i4', 257) or op(a[left]) != ('ldloc', total):
            continue
        if not (code == site + 1 and left == code + 1 and right == left + 1):
            continue
        if [op(x) for x in a[right:ci]] != rhs:
            continue
        tail = [op(x) for x in a[ci + 1:ci + 4]]
        if len(tail) == 3 and tail[0][0] == 'stloc' and tail[1] == ('ldloc', tail[0][1]) and tail[2] == ('stloc', total):
            matches.append((site, ci + 4))
    require(len(matches) == 1, f'原inplace Add/tmp回写匹配数{len(matches)}')
    old_start, old_end = matches[0]
    require(op(a[old_end])[0] == 'br' and op(b[add + 1])[0] == 'br', '更新不是循环体尾')
    cuts = [(create - 1, create + 2), (add_start, add + 1), (value - 1, value + 2)]
    targets = branches(b)
    for begin, end in cuts:
        require(not any(b[j]['Offset'] in targets for j in range(begin + 1, end)), '分支进入改写片段内部')
    entries = []
    for i, ins in enumerate(b):
        if i == add_start:
            # 注入原片段，无分支；各条共享替换段入口，保持其它目标映射稳定。
            entries.extend((i, copy.deepcopy(x)) for x in a[old_start:old_end])
        if not any(start <= i < end for start, end in cuts):
            entries.append((i, copy.deepcopy(ins)))
    holder_uses = [(i, op(x)) for i, x in enumerate(b) if op(x) in [('ldloc', holder), ('stloc', holder)]]
    require([i for i, _ in holder_uses] == [create + 1, add_start, value - 1], 'holder额外使用')
    return entries, {'method': off['Name'], 'removed_pic': op(a[old_start])[1], 'holder': holder, 'total': total, 'off_slice': [old_start, old_end], 'on_slices': cuts}


def compare(off, on):
    require(len(off['Methods']) == len(on['Methods']), '方法数量变更')
    off_sites, on_sites = pic_sites(off), pic_sites(on)
    restored, proofs = [], []
    for a, b in zip(off['Methods'], on['Methods']):
        for key in ('Name', 'Attributes', 'ImplAttributes', 'Signature', 'InitLocals', 'MaxStack'):
            require(a[key] == b[key], f'{a["Name"]}: {key}变更')
        entries, proof = patterns(a, b)
        if proof:
            proofs.append(proof)
        else:
            require(a['Locals'] == b['Locals'], a['Name'] + ': locals变更')
        restored.append(entries)
    removed = sorted(p['removed_pic'] for p in proofs)
    require(len(set(removed)) == len(removed), '重复删除PIC')
    require(len(off_sites) - len(on_sites) == len(removed), 'PIC数差不等于已证明删除点')
    surviving = [n for n in range(len(off_sites)) if n not in removed]
    site_map = dict(enumerate(surviving))
    on_indices = {(mi, idx): n for mi, idx, n, _ in on_sites}
    for mi, (a, b, entries) in enumerate(zip(off['Methods'], on['Methods'], restored)):
        for old_index, ins in entries:
            if (mi, old_index) in on_indices:
                # 原Add注入片段入口不是PIC on位置，禁止碰普通整数。
                expected = site_map[on_indices[mi, old_index]]
                require(op(ins)[0] == 'ldc.i4', 'PIC位置改变')
                ins['Op'], ins['Arg'] = 'ldc.i4', expected
        left = canonical(a, list(enumerate(a['IL'])))
        right = canonical(b, entries)
        if left != right:
            mismatch = next(((i, x, y) for i, (x, y) in enumerate(zip(left[0], right[0])) if x != y), None)
            raise AssertionError(f'{a["Name"]}: 逆变换后IL/EH不一致 {mismatch}; 长度{len(left[0])}/{len(right[0])}')
    return {'methods': len(off['Methods']), 'off_pic_sites': len(off_sites), 'on_pic_sites': len(on_sites), 'deleted_pic_ids': removed, 'proofs': proofs, 'status': 'pass'}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('off', type=pathlib.Path)
    parser.add_argument('on', type=pathlib.Path)
    parser.add_argument('--output', type=pathlib.Path, required=True)
    args = parser.parse_args()
    off, on = decode(args.off), decode(args.on)
    args.output.mkdir(parents=True, exist_ok=True)
    for name, value in [('off.decoded.json', off), ('on.decoded.json', on)]:
        (args.output / name).write_text(json.dumps(value, ensure_ascii=False, indent=2))
    report = compare(off, on)
    report.update(off_sha256=off['Sha256'], on_sha256=on['Sha256'])
    (args.output / 'audit.json').write_text(json.dumps(report, ensure_ascii=False, indent=2))
    print(json.dumps(report, ensure_ascii=False))


if __name__ == '__main__':
    main()
