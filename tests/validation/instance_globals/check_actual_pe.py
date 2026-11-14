# 复用原read_pe_decoder完整指令输出，限定A1四调用/context/thunk与间接路径。
# 不是全程序数据流证明；产品字典身份仍由真实运行验收。
import json
from pathlib import Path
import sys


def require(ok, message):
    if not ok: raise ValueError(message)


def check(data, hosted=False):
    methods = data['methods']
    def get(suffix):
        matches = [m for m in methods if m['method']['name'].endswith('_' + suffix)]
        require(len(matches) == 1, '方法定位不唯一: ' + suffix)
        return matches[0]
    def calls(m):
        return [i for i in m['body']['instructions'] if i['opcode'] in ('0x28', '0x6f')]
    evidence = []
    expected = {'identities': (2, '0x2'), 'direct_rebound': (1, '0x2'), 'shadow': (1, '0x3')}
    all_helpers = [(m, i) for m in methods for i in calls(m)
                   if i.get('target', {}).get('name') == 'CallDirectGlobals']
    require(len(all_helpers) == (6 if hosted else 4), '本图实际helper调用总数不符')
    for name, (count, context_op) in expected.items():
        m = get(name); ins = m['body']['instructions']
        found = [i for i in calls(m) if i.get('target', {}).get('name') == 'CallDirectGlobals']
        require(len(found) == count, '调用数: ' + name)
        for call in found:
            require(call['opcode'] == '0x28' and call['target'] == {
                'owner': 'RyuPy.Runtime.PyModuleInstances', 'name': 'CallDirectGlobals',
                'signature_hex': '00021c1c1c'}, '真实owner/签名/Call不符')
            index = ins.index(call)
            require(ins[index - 2]['opcode'] == context_op, 'context不是实际函数参数')
            require(ins[index - 1]['opcode'] in ('0x8', '0x9', '0x11'), 'callee不是保存局部')
            evidence.append({'method': m['method'], 'token': m['token'], 'offset': call['offset'],
                             'context': ins[index-2], 'callee': ins[index-1]})
        if name != 'shadow':
            loads = [i for i in calls(m) if i.get('target', {}).get('name') == 'LoadName']
            require(len(loads) == count, '真实globals词法读取数')
            for load in loads:
                index = ins.index(load)
                require(ins[index-1].get('string') == 'globals' and ins[index-2]['opcode'] == '0x2', 'LoadName来源')
        else:
            require(any(i['opcode'] == '0x2' and ins[n+1]['opcode'] == '0xc'
                        for n,i in enumerate(ins[:-1])), 'shadow真实参数未保存')
        # 既有owned thunk真实Call，而非MemberRef表存在；快/慢绑定两边都必须到同一函数。
        thunk = [x for x in methods if x['method']['name'].startswith('__thunk_')
                 and any(i.get('token') == m['token'] for i in calls(x))]
        require(len(thunk) == 1, 'owned thunk不唯一')
        edges = [i for i in calls(thunk[0]) if i.get('token') == m['token']]
        require(len(edges) == 2, 'owned thunk两绑定分支未闭合')
    if hosted:
        body_read = get('body_read')
        prefix = body_read['method']['name'].removesuffix('body_read')
        mains = [m for m in methods if m['method']['name'] == prefix + '__module_main']
        require(len(mains) == 1, 'module_body真实初始化方法不唯一')
        for m in [body_read, mains[0]]:
            ins = m['body']['instructions']
            found = [i for i in calls(m) if i.get('target', {}).get('name') == 'CallDirectGlobals']
            require(len(found) == 1, 'module_body各需一个实际Call')
            call = found[0]; index = ins.index(call)
            require(call['opcode'] == '0x28' and call['target'] == {
                'owner': 'RyuPy.Runtime.PyModuleInstances', 'name': 'CallDirectGlobals',
                'signature_hex': '00021c1c1c'}, 'module_body真实签名')
            require(ins[index-2]['opcode'] == '0x2', 'module_body当前context')
            evidence.append({'method': m['method'], 'token': m['token'], 'offset': call['offset'],
                             'context': ins[index-2], 'callee': ins[index-1]})
        legacy = get('invoke')
        require(not any(i.get('target', {}).get('name') == 'CallDirectGlobals' for i in calls(legacy)), 'Legacy误授新能力')
        ordinary = [i for i in calls(legacy) if i.get('target', {}).get('name') == 'CallValue0']
        require(len(ordinary) == 1 and ordinary[0]['target'] == {
            'owner': 'RyuPy.Runtime.PyPic', 'name': 'CallValue0',
            'signature_hex': '00021c081c'}, 'Legacy普通PIC callback签名不符')
        legacy_ins = legacy['body']['instructions']
        require(legacy_ins[legacy_ins.index(ordinary[0])-1]['opcode'] == '0x2',
                'Legacy callback不是真实参数')
        legacy_one = get('invoke_one')
        require(not any(i.get('target', {}).get('name') == 'CallDirectGlobals' for i in calls(legacy_one)), 'Legacy带参误授新能力')
        one = [i for i in calls(legacy_one) if i.get('target', {}).get('name') == 'CallValue1']
        require(len(one) == 1 and one[0]['target'] == {
            'owner': 'RyuPy.Runtime.PyPic', 'name': 'CallValue1',
            'signature_hex': '00031c081c1c'}, 'Legacy带参PIC签名不符')
        one_ins = legacy_one['body']['instructions']; index = one_ins.index(one[0])
        require(one_ins[index-2]['opcode'] == '0x2' and one_ins[index-1]['opcode'] == '0x8',
                'Legacy带参callee/实参局部来源不符')
    indirect = get('indirect')
    require(not any(i.get('target', {}).get('name') == 'CallDirectGlobals' for i in calls(indirect)), '间接误授context')
    actual = [i for i in calls(indirect) if i.get('target', {}).get('owner') == 'RyuPy.Runtime.PyOps'
              and i['target']['name'] == 'CallZeroWithFrame']
    require(len(actual) == 1 and actual[0]['target']['signature_hex'] == '00061c081c1c0a1c0a', '间接普通调用丢失')
    return {'pe_sha256': data['sha256'], 'actual_helper_calls': evidence,
            'indirect': actual, 'scope': '有限实际IL静态合同；未证明执行结果或Native存活'}

if __name__ == '__main__':
    result = check(json.loads(Path(sys.argv[1]).read_text()), hosted=sys.argv[2:] == ['--hosted'])
    print(json.dumps(result, indent=2))
