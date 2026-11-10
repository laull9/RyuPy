"""KeyError三图五角色的固定合同；复用旧原流/身份门，只显式准许owner实例模块。"""
import ast
import collections
from pathlib import Path
import re
import decimal_percent_contract as previous

ROOT, require, sha, parse = previous.ROOT, previous.require, previous.sha, previous.parse
safe_file, verify_process = previous.safe_file, previous.verify_process
verify_command, verify_checkpoint, verify_trx = previous.verify_command, previous.verify_checkpoint, previous.verify_trx
shared = previous.shared.shared
complete, isolated, IdentityError = shared.complete, shared.isolated, shared.IdentityError
TITLE = 'validation: keyerror-construction-001'
MANIFEST = ROOT / 'scripts/keyerror_construction.json'
ABI = '[RyuPy.Runtime]RyuPy.Runtime.PyOps::LoadDefaultBuiltin[0;0;1](String)->Object'


def check_context(env, event):
    require(event.get('head_commit', {}).get('message') == TITLE, 'KeyError精确标题不符')
    previous.check_context(env, event)


def no_binding(source):
    """直接入口不能因任何同名赋值、定义、参数或global预扫描而变成ModuleVar。"""
    tree = ast.parse(source.read_text())
    for node in ast.walk(tree):
        require(not (isinstance(node, ast.Name) and node.id == 'KeyError' and isinstance(node.ctx, (ast.Store, ast.Del))), '直接源有KeyError绑定')
        require(not (isinstance(node, (ast.Global, ast.Nonlocal)) and 'KeyError' in node.names), '直接源有KeyError声明')
        require(not (isinstance(node, (ast.FunctionDef, ast.ClassDef, ast.arg)) and getattr(node, 'name', getattr(node, 'arg', '')) == 'KeyError'), '直接源有同名定义')
        if isinstance(node, (ast.Import, ast.ImportFrom)):
            require(not any((alias.asname or alias.name) == 'KeyError' for alias in node.names), '直接源有同名导入')
    return tree


def load_manifest(root=ROOT, path=MANIFEST):
    data = parse(path.read_bytes())
    require(data['schema_version'] == 1 and data['title'] == TITLE, '版本/标题改变')
    require(data['cp_version'] == '3.12.13' and data['cp_sha256'] == shared.old.shared.PYTHON_SHA
            and data['sdk_version'] == '10.0.403', '固定工具改变')
    require(data['default_project_delta'] == 0 and data['old_cases'] == [] and data['expected_product_processes'] == 20, '进程数/旧独立门合同改变')
    require([g['id'] for g in data['graphs']] == ['direct', 'shadow', 'default-owner'], '固定三图改变')
    require({str(p.relative_to(root)) for p in (root / 'tests/cases/keyerror_construction').rglob('*.py')} == set(data['sources']), '实际fixture集合漏项/增项')
    for name, digest in {**data['sources'], **data['protected']}.items():
        require(sha(safe_file(root, name)) == digest, '冻结源改变：' + name)
    identifiers = []
    for graph in data['graphs']:
        require(graph['instance_modules'] == (['owner'] if graph['id'] == 'default-owner' else []), '仅owner可显式实例')
        require(graph['modules']['__main__'] == graph['source'] and set(graph['modules']) == {'__main__', *graph['include']}, '入口/include图不符')
        require(set(graph['modules'].values()) <= set(data['sources']), '图含未冻结源')
        for module in graph['include']:
            require(graph['modules'][module] == str(Path(graph['source']).parent / (module + '.py')), 'include实际搜索根不符')
        for row in graph['cases']:
            identifiers.append(row['id'])
            require(row['kind'] in ('cp', 'trace') and row['cp'] is True and row['exit'] == (1 if row['kind'] == 'trace' else 0), '场景不能改NI或跳CP')
            require(type(row['stdout']) is str and type(row['argv']) is list and all(type(x) is str for x in row['argv']), '完整stdout/argv缺失')
    require(identifiers == ['direct-all', 'direct-uncaught', 'direct-bad-repr', 'shadow-all', 'default-owner-all'], '五角色遗漏/重复/换序')
    require([r['argv'] for r in data['graphs'][0]['cases']] == [[], ['uncaught'], ['bad-repr']], '真实错误退出argv改变')
    no_binding(root / data['graphs'][0]['source'])
    no_binding(root / data['graphs'][2]['modules']['consumer'])
    actual = []
    for cls, expected in data['csharp']['classes'].items():
        methods = re.findall(r'\[Fact\]\s+public void (\w+)\(', (root / ('runtime/RyuPy.Runtime.Tests/' + cls + '.cs')).read_text())
        require(collections.Counter(methods) == collections.Counter(expected), '实际Fact改变：' + cls)
        actual += [cls + '.' + method for method in methods]
    require(len(actual) == 5 and collections.Counter(actual) == collections.Counter(data['csharp']['facts']), '精确五Fact改变')
    # 原percent全源、42Facts和五row仍用旧合同独立检查，不由新金样覆盖。
    previous.load_manifest(root)
    return data


def cases(graph):
    return [dict(row, source=graph['source'], include=graph['include'], modules=graph['modules'],
                 instance_modules=graph['instance_modules'], graph_id=graph['id'], mode='cp') for row in graph['cases']]


def verify_row(prefix, argv, row, source):
    return previous.verify_row(prefix, argv, row, source)


def build_argv(runner, case, target, aot=False):
    argv = shared.build_argv(runner, case, target, aot)
    require(case['instance_modules'] == (['owner'] if case['graph_id'] == 'default-owner' else []), '非固定owner选择')
    for module in case['instance_modules']: argv += ['--instance-module', module]
    return argv


def verify_plan(path, case, aot=False):
    plan = parse(path.read_bytes())
    require(plan['compiled_feature'] is False and plan['session_policy'] == 'explicit-only', '必须普通无pilot CLI')
    rows = plan['modules']
    require(len(rows) == len(case['modules']) and {r['module'] for r in rows} == set(case['modules']), '实际模块集合不符')
    for row in rows:
        owner = row['module'] == 'owner' and case['graph_id'] == 'default-owner'
        require(row['compiled'] is True and row['execution'] == ('InstanceGlobals' if owner else 'LegacySingleton')
                and row['reason'] == ('explicit-instance' if owner else 'policy-disabled')
                and row['ordinary_name_handlers'] is owner and row['relative_from_import'] is owner,
                '真实模块选型/能力不符：' + row['module'])
    return plan


def pe_calls(data, pe, plan, case):
    """审计真实PE调用而非仅MemberRef；模块前缀来自同次source顺序，owner不能借新API替身。"""
    require(data['Sha256'].lower() == sha(pe) and Path(data['Path']) == pe.resolve(), 'PE审计没有绑定实际产物')
    calls = []
    for method in data['Methods']:
        for ins in method['IL']:
            if ins['Op'] == 'call' and 'LoadDefaultBuiltin' in str(ins['Arg']):
                require(ins['Arg'] == ABI, '新API实际调用签名不符')
                calls.append(dict(method=method['Name'], offset=ins['Offset'], target=ins['Arg']))
    if case['graph_id'] == 'direct': require(len(calls) >= 17, 'direct真实16构造＋值读取未走新API')
    elif case['graph_id'] == 'shadow': require(not calls, '遮蔽源不应走新API')
    else:
        index = next(i for i, row in enumerate(plan['modules']) if row['module'] == 'consumer')
        require(len(calls) == 3 and all('::m' + str(index) + '_' in row['method'] for row in calls), '必须只有静态consumer的三次读取')
        owner_index = next(i for i, row in enumerate(plan['modules']) if row['module'] == 'owner')
        owner_calls = [i for m in data['Methods'] if '::m' + str(owner_index) + '_' in m['Name'] for i in m['IL']
                       if i['Op'] == 'call' and 'PyModuleInstances::LoadName' in str(i['Arg'])]
        require(owner_calls, '真实owner没有实例名字读取')
    return dict(passed=True, status='passed', pe_sha256=sha(pe), calls=calls, module_plan=plan,
                scope='真实产物精确冷ABI callsites与模块选型；非全IL形式化证明')


def verify_native(runner, case, layout):
    old = shared.old
    sdk = old.read_sdk_version(layout.host / 'sdk.version')
    require(sdk == runner.data['sdk_version'], 'Native SDK不符')
    manifest = old.verify_manifest(layout.dist, layout.native, 'linux-x64', sdk)
    sources = manifest['sources']
    require(len(sources) == len(case['modules']) and {s['module_name'] for s in sources} == set(case['modules']), 'Native逐源集合不符')
    for row in sources:
        name = row['module_name']; source = case['modules'][name]
        logical = 'entry/' + Path(source).name if name == '__main__' else 'modules/' + name + '.py'
        require(row['compiled_text_sha256'] == runner.data['sources'][source] and row['logical_source'] == logical and row['package'] == '', 'Native源身份不符')
    require(next(s['module_id'] for s in sources if s['module_name'] == '__main__') == manifest['entry']['module_id'], 'Native入口身份不符')
    options = manifest['compiler']['options']
    require(options['module_environment_policy'] == 'explicit-only' and options['instance_modules'] == case['instance_modules']
            and all(not value for key, value in options.items() if key.startswith(('experimental_', 'diagnostic_'))), 'Native编译选项不符')
    plan = verify_plan(layout.managed.parent / 'module-environment.json', case, True)
    reasons = {r['module']: r['reason'] for r in plan['modules']}
    require(len(manifest['modules']) == len(reasons) and all(r['environment_reason'] == reasons[r['name']] for r in manifest['modules']), 'Native部署owner原因不符')
    for row in manifest['modules']:
        if row['name'] in case['instance_modules']:
            require(row['getter'] is None and row['setter'] is None and row['exports'] == [], 'owner偷偷退Legacy导出')
    bindings = {r['role']: r for r in manifest['build_inputs']}
    require(bindings['runtime_managed_input']['sha256'] == sha(runner.runtime) == sha(layout.managed.parent / 'RyuPy.Runtime.dll')
            and bindings['compiled_managed_input']['sha256'] == sha(layout.managed), 'Native PE/runtime绑定不符')
    return manifest


def verify_native_structure(runner, case, layout):
    plan = verify_plan(layout.managed.parent / 'module-environment.json', case, True)
    runner.structure(case, layout.managed, plan, 'native-pe')
