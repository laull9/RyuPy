"""warning分域的小合同：固定原源/派生关系、CP原流与两种精确拒绝；不扩产品支持。"""
import ast
import hashlib
import json
import re


def need(ok, message):
    if not ok:
        raise RuntimeError(message)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def cases(contract):
    """原组合是单独负例集合，不能作为正例跳过。"""
    return contract['cases'] + contract['boundary_cases']


def instance_modules(case, arm):
    return arm.get('instance_modules', case['instance_modules'])


def load(root, contract):
    path = root / contract['warning_split']
    need(sha(path) == contract['files'][contract['warning_split']], 'warning分域合同SHA改变')
    split = json.loads(path.read_text())
    for name, expected in (split['original_files'] | split['derived_files']).items():
        need(contract['files'][name] == expected and sha(root / name) == expected, 'warning原源/派生源改变：' + name)
    entry = (root / 'fixtures/warning_names/entry.py').read_text()
    subject = (root / 'fixtures/warning_names/subject.py').read_text()
    start = entry.index('with warnings.catch_warnings(record=True) as records:\n    subject.events.clear()\n    subject.remove_name()')
    end = entry.index('\nfor label, message, category', start)
    need((root / 'fixtures/warning_names_legacy/entry.py').read_text() == entry[:start] + entry[end+1:],
         'Legacy变体必须只移除原缺name观察块')
    need((root / 'fixtures/warning_names_legacy/subject.py').read_text() == subject[:subject.index('\ndef remove_name():')],
         'Legacy变体必须只移除remove_name定义')
    missing = root / 'fixtures/warning_name_missing'
    for p in missing.glob('*.py'):
        need(not any(isinstance(n, ast.ClassDef) for n in ast.walk(ast.parse(p.read_text()))), 'Instance变体必须真正class-free')
    need(entry[start:end] in (missing / 'entry.py').read_text(), '原缺name观察块必须逐字保留')
    need((missing / 'subject.py').read_text().endswith(subject[subject.index('def issue(message, category):'):]),
         'Instance两个helper必须逐字保留')
    positive = {c['id']: c for c in contract['cases']}
    need(len(positive) == len(contract['cases']) and 'warning_names' not in positive, '原组合不能混入正向集合')
    for name, modules in [('warning_names_legacy', []), ('warning_name_missing', ['subject'])]:
        need(positive[name]['instance_modules'] == modules and len(positive[name]['arms']) == 1
             and positive[name]['entry'] == 'fixtures/' + name + '/entry.py', 'warning正向域模式/输入改变')
    boundaries = contract['boundary_cases']
    need(len(boundaries) == 1 and boundaries[0]['id'] == 'warning_names'
         and boundaries[0]['entry'] == 'fixtures/warning_names/entry.py', '原组合负例缺失/被替换')
    expected_arms = [('legacy-rejected', 'build', []), ('instance-rejected', 'jit', ['subject'])]
    need([(a['id'], a['reject_phase'], instance_modules(boundaries[0], a)) for a in boundaries[0]['arms']] == expected_arms,
         '原组合两种真实阶段/模式拒绝必须同时保留')
    need(hashlib.sha256(split['original_cp_stdout'].encode()).hexdigest() == split['original_cp_stdout_sha256'],
         '原完整CP观察改变')
    return split


def check_cp(split, name, actual):
    expected = split['expected_cp'].get(name)
    if name == 'warning_names':
        expected = {'exit': 0, 'stdout': split['original_cp_stdout'], 'stderr': ''}
    if expected is not None:
        need((actual.returncode, actual.stdout, actual.stderr) ==
             (expected['exit'], expected['stdout'].encode(), expected['stderr'].encode()),
             'warning分域CP与执行前冻结预期不符，停止：' + name)


def rejection(split, arm, actual, subject):
    """Recorder只在自然结束返回；超时/启动失败先抛异常，信号/崩溃不能匹配exit1。"""
    expected = split['boundaries'][arm['id']]
    need(arm['reject_phase'] == expected['phase'], '负例阶段不符')
    need(actual.returncode == expected['exit'] == 1 and actual.stdout == expected['stdout'].encode(),
         '负例不是精确自然exit1/空stdout')
    if expected['phase'] == 'jit':
        need(actual.stderr == expected['stderr'].encode(), 'Instance负例不是原精确NI')
    else:
        # 只去除真实诊断前的stage计时行；不得删除其它诊断/用户输出/路径。
        lines = actual.stderr.decode().splitlines(keepends=True)
        stage = []
        while lines and lines[0].startswith('[ryupy-stage] '):
            line = lines.pop(0)
            need(re.fullmatch(r'\[ryupy-stage\] event=(?:begin|end|result) stage=[a-z-]+ module="[^"\n]+"(?: pass_index=\d+ pass="[a-z_]+")? elapsed_us=\d+\n', line),
                 '未声明的stage行形状')
            stage.append(line)
        need(stage and stage[-1].startswith(expected['last_stage_prefix']), 'Legacy负例没有止于subject HIR')
        diagnostic = ''.join(lines)
        need(diagnostic.count(str(subject)) == 1, 'Legacy诊断不是唯一原subject源路径')
        need(diagnostic.replace(str(subject), '{SOURCE}') == expected['diagnostic'], 'Legacy不是原完整E0100元数据删除诊断')
    return {'classification': 'expected-capability-rejection', 'phase': expected['phase'],
            'exit': 1, 'positive_compatibility': False, 'combined_input_supported': False}
