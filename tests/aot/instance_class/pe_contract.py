"""独立AST→MIR FnId→实际PE静态边；类型签名另由既有Rust schema2门完整核验。"""
import ast
from collections import Counter
from contract import require


def definitions(source):
    result = []
    def visit(node, prefix=''):
        if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)):
            result.append((prefix + node.name, node.lineno))
            return
        if isinstance(node, ast.ClassDef):
            for child in node.body: visit(child, prefix + node.name + '.')
            return
        # 包含ExceptHandler、match等非stmt容器，不按执行覆盖漏死分支。
        for child in ast.iter_child_nodes(node): visit(child, prefix)
    visit(ast.parse(source))
    return result


def check(decoded, expected, mapping, sources):
    require(decoded['sha256'] == expected['pe']['sha256'], 'PE与期望身份不同')
    owners = {x['function_id']: x for x in expected['owners']}
    require(len(owners) == len(expected['owners']), '重复owner FnId')
    require(Counter(x['function_id'] for x in mapping) == Counter(owners.keys()), 'MIR映射与owner全集不同')
    for module, source in sources.items():
        rows = [x for x in mapping if x['module'] == module]
        require(Counter((x['lexical_qualname'], x['firstlineno']) for x in rows)
                == Counter(definitions(source['text'])), '独立AST定义全集与MIR不等')
        require(all(x['source_sha256'] == source['sha256'] for x in rows), 'MIR源码SHA不等')
    require({x['module'] for x in mapping} == set(sources), '未知来源模块')
    methods = decoded['methods']
    def locate(row):
        candidates = [m for m in methods if m['method']['owner'] == row['owner']]
        require(0 <= row['slot'] < len(candidates), 'MethodDef slot越界')
        method = candidates[row['slot']]
        require(method['method']['name'] == row['name'] and method['body'], 'slot/name/body不符')
        return method
    edges = []
    fields = set()
    for function, row in owners.items():
        core = locate(row['core']); root = row['root']
        initializer, thunk = locate(root['source']), locate(root['target'])
        ins = initializer['body']['instructions']; index = root['instruction']
        require(index + 2 < len(ins), '静态根索引越界')
        pointer, ctor, store = ins[index:index+3]
        require(pointer['opcode'] == '0xfe06' and pointer.get('token') == thunk['token'], '不是实际ldftn到本thunk')
        require(ctor['opcode'] == '0x73' and ctor.get('target', {}).get('owner') == 'RyuPy.Runtime.PyOwnedCall'
                and ctor['target']['name'] == '.ctor', '无实际owned delegate构造')
        require(store['opcode'] == '0x80' and 'field_token' in store, '无真实委托静态字段保存')
        field = store['field_token']; require(field not in fields, 'owner共用委托字段'); fields.add(field)
        calls = [i for i in thunk['body']['instructions'] if i['opcode'] == '0x28' and i.get('token') == core['token']]
        require(len(calls) == row['calls'], 'thunk实际core调用分支不符')
        uses = []
        for method in methods:
            body = method.get('body')
            if not body: continue
            instructions = body['instructions']
            for pos, instruction in enumerate(instructions):
                if instruction['opcode'] != '0x7e' or instruction.get('field_token') != field: continue
                # 定义发射将默认值数组先计算为locals，再装载；这条字段load必须流向下一owned构造。
                following = [i for i in instructions[pos+1:] if i['opcode'] in ('0x28', '0x6f')]
                target = next((i for i in following if i.get('target', {}).get('name') == 'CreateOwnedFunction'), None)
                require(target is not None and target['opcode'] == '0x28'
                        and target['target']['owner'] == 'RyuPy.Runtime.PyModuleInstances', '缺实际CreateOwnedFunction边')
                between=instructions[pos+1:instructions.index(target)]
                require(not any(int(i['opcode'],16) in set(range(0x2b,0x46)) | {0x2a,0x7a,0xdd,0xde}
                                for i in between), '委托load与owned创建之间有未证明控制流')
                uses.append({'method': method['token'], 'load': instruction['offset'], 'create': target['offset']})
        require(len(uses) == 1, '每FnId须恰一次实际owned创建')
        edges.append({'function_id': function, 'core': core['token'], 'thunk': thunk['token'],
                      'pointer_offset': pointer['offset'], 'field': field, 'create': uses[0]})
    return {'pe_sha256': decoded['sha256'], 'owners': len(owners), 'edges': edges,
            'scope': '静态实际边；非Native存活/业务通过，签名须另经Rust门'}
