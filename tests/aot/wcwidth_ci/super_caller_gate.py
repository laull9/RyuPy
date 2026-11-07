"""小Instance caller由生产driver测试入口发射；同一PE独审后双PIC执行，不冒CLI产物。"""
import json
import os
import run as h
from instance_pe import envelope, unchanged
from super_contract import CALLER_FUNCTIONS, audit_caller, check_test_count

EXPECTED_CALLER = b'instance caller explicit super construction capture; legacy model\n'


def encode_role_inputs(roles, role_data):
    rows = []
    parameters = {'canonical': [], 'construct': ['start', 'receiver'],
                  'direct': ['start', 'receiver'], 'invoke_read': ['proxy'],
                  'invoke_callback': ['proxy', 'callback']}
    for role in sorted(roles, key=lambda r: r['role']):
        names = parameters[role['role']]
        if set(role['parameter_slots']) != set(names):
            raise RuntimeError('源码参数身份不符')
        slots = [role['state_slot']] + [role['parameter_slots'][name] for name in names]
        if any(type(slot) is not int or not 0 <= slot < 65535 for slot in slots):
            raise RuntimeError('非法实际参数slot')
        if len(set(slots)) != len(slots):
            raise RuntimeError('隐藏state与Python参数重叠')
        slots += [65535] * (3 - len(slots))
        rows.append('\t'.join(map(str, [role['role'], role['function_id']] + slots)))

    state = role_data['module_init_state_slot']
    if type(state) is not int or not 0 <= state < 65535:
        raise RuntimeError('非法module init state slot')
    return '\n'.join(rows) + '\n', str(state) + '\n'


def run_caller(python, runtime_sha):
    from explicit_super_gate import strict
    source = h.HERE / 'super_caller'
    h.save('ast.json', audit_caller(source / 'caller.py'))
    strict('cp', [python, '-B', source / 'entry.py'], expected=EXPECTED_CALLER)
    structure = h.OUT / 'structure'
    if structure.exists():
        raise RuntimeError('driver输出目录必须全新')
    env = dict(os.environ, RYUPY_SUPER_CALLER_OUT=str(structure))
    name = 'explicit_super_caller_emit_gate'
    h.run('driver', ['cargo', 'test', '--locked', '-p', 'ryupy-driver', name,
                    '--', '--ignored', '--nocapture'], env=env, timeout=900)
    check_test_count((h.OUT / 'driver.stdout').read_text(), name)
    plan = json.loads((structure / 'plan.json').read_text())
    selected = [m for m in plan['modules'] if m['execution'] == 'InstanceGlobals']
    if len(selected) != 1 or selected[0]['module'] != 'caller' or not selected[0]['compiled']:
        raise RuntimeError('小caller没有真实编译为Instance')
    if (structure / 'diagnostics.txt').read_bytes():
        raise RuntimeError('driver存在诊断')
    if h.sha(structure / 'RyuPy.Runtime.dll') != runtime_sha:
        raise RuntimeError('driver复制的runtime不是同轮固定身份')
    identity = envelope(structure, 'super_caller', os.environ['GITHUB_SHA'])
    expected = json.loads((structure / 'super_caller-expected.json').read_text())
    roles_raw = (structure / 'roles.json').read_bytes()
    role_data = json.loads(roles_raw)
    roles = role_data['roles']
    owners = {owner['function_id'] for owner in expected['owners']}
    if (len(roles) != 5 or {r['role'] for r in roles} != CALLER_FUNCTIONS
            or {r['function_id'] for r in roles} != owners
            or {r['module_id'] for r in roles} != {expected['modules'][0]['module_id']}):
        raise RuntimeError('caller角色/真实FnId/module_id映射不符')
    if expected['selected'] != ['caller'] or expected['owner_count'] != 5:
        raise RuntimeError('caller owner全集不符')
    # 源身份由同次driver SourceSummary给出，再对当前原文件复核，不接受只报PE哈希。
    sources = {s['module_name']: s for s in expected['sources']}
    for module, filename in [('__main__', 'entry.py'), ('caller', 'caller.py'), ('model', 'model.py')]:
        if sources[module]['compiled_text_sha256'] != h.sha(source / filename):
            raise RuntimeError('driver源码身份不符：' + module)
    rows, state = encode_role_inputs(roles, role_data)
    (structure / 'roles.tsv').write_text(rows)
    (structure / 'init-state.txt').write_text(state)
    for filename in ['roles.json', 'roles.tsv', 'init-state.txt',
                     'RyuPy.Runtime.dll', 'super_caller.runtimeconfig.json']:
        identity[filename] = h.sha(structure / filename)
    name = 'explicit_super_caller_actual_pe_gate'
    h.run('pe', ['cargo', 'test', '--locked', '-p', 'ryupy-codegen-cil', name,
                '--', '--ignored', '--nocapture'], env=env, timeout=900)
    check_test_count((h.OUT / 'pe.stdout').read_text(), name)
    if (structure / 'negative-checks.txt').read_text() != (
            'context start receiver proxy flag target callback callback-result init-context init-alias: rejected\n'):
        raise RuntimeError('实际PE局部反例覆盖未完成')
    identity['negative-checks.txt'] = h.sha(structure / 'negative-checks.txt')
    unchanged(structure, identity)
    for pic in ['1', '0']:
        strict('jit-pic-' + pic, [os.environ['DOTNET'], structure / 'super_caller.dll'],
               env=dict(os.environ, RYUPY_PIC=pic), expected=EXPECTED_CALLER)
        unchanged(structure, identity)
    h.save('result.json', {'passed': True, 'status': 'passed', 'identity': identity,
                          'scope': '生产driver测试入口同次PE；caller Instance，类与回调Legacy'})
