"""官方锁定源码恢复后复用P0逐ID门禁；冻结工具，无历史结果复用。"""
import json
import os
from pathlib import Path
import shutil
import sys
import tarfile
import run as h


def audit_plans(result, expected):
    """逐项目核实际计划；固定选择集合不等于所有被选模块已编译/执行。"""
    plans = {}
    for pid, selected in expected['selected'].items():
        path = result / pid / 'native' / pid / 'module-environment.json'
        actual = json.loads(path.read_text())
        if actual['compiled_feature'] is not True or actual['session_policy'] != expected['session_policy']:
            raise RuntimeError('实际runner没有启用原pilot策略：' + pid)
        chosen = sorted((m for m in actual['modules'] if m['execution'] == 'InstanceGlobals'),
                        key=lambda m: m['module'])
        if chosen != sorted(selected, key=lambda m: m['module']):
            raise RuntimeError('自动实例选择/编译状态改变：' + pid)
        if any(m['relative_from_import'] or m['ordinary_name_handlers'] for m in actual['modules']):
            raise RuntimeError('默认项目意外打开显式实例能力：' + pid)
        plans[pid] = {'sha256': h.sha(path), 'selected': chosen}
    return plans


def require_progress_promotion(result):
    """同轮完整矩阵的独立约束，不重跑、不改状态标签或默认模块选择。"""
    summary = json.loads((result / 'summary.json').read_text())
    rows = json.loads((result / 'matrix.json').read_text())
    if (not summary.get('valid') or summary.get('denominator') != 20
            or summary.get('cp_passed') != 20 or not summary.get('preserve_pass')
            or not summary.get('promotion_pass') or summary.get('protected_lost')
            or summary.get('historical_regressions_open')):
        raise RuntimeError('同轮完整20项未满足保九及progress恢复门')
    if len(rows) != 20 or len({row['id'] for row in rows}) != 20:
        raise RuntimeError('晋升约束拒绝半矩阵/重复ID')
    progress = [row for row in rows if row['id'] == 'progress']
    if len(progress) != 1 or progress[0]['status'] != 'native':
        raise RuntimeError('本轮真实progress入口尚未native')
    return {'run_id': summary['run_id'], 'same_matrix': True,
            'summary_sha256': h.sha(result / 'summary.json'),
            'matrix_sha256': h.sha(result / 'matrix.json'),
            'promotion_pass': True, 'defaults_changed': False}


def main():
    h.OUT = h.REPO / 'target/project-ci-evidence'
    h.OUT.mkdir(parents=True, exist_ok=True)
    store = h.REPO / 'third_party/projects'
    store.mkdir(parents=True, exist_ok=True)
    downloads = store / '_dist'
    downloads.mkdir()
    recipe = json.loads((h.HERE / 'project-source-recovery.json').read_text())
    for item in recipe['packages']:
        archive = downloads / item['filename']
        h.run('fetch-' + item['package'], ['curl', '--fail', '--location', '--show-error',
            '--max-time', '180', '--proto', '=https', '--proto-redir', '=https', item['url'], '-o', archive], timeout=200)
        if h.sha(archive) != item['sha256']:
            raise RuntimeError('项目源码下载hash不符：' + item['package'])
        destination = store / item['store_directory']
        destination.mkdir()
        with tarfile.open(archive) as tar:
            tar.extractall(destination, filter='data')
    h.run('runtime-build', ['make', 'runtime'], timeout=300)
    h.run('runner-build', ['cargo', 'build', '--locked', '--features',
        'ryupy-driver/ordinary-dynamic-module-pilot', '-p', 'ryupy-e2e', '--bin', 'projects'], timeout=1200)
    frozen = h.OUT / 'frozen'; frozen.mkdir()
    runtime = frozen / 'RyuPy.Runtime.dll'
    runner = frozen / 'projects'
    shutil.copy2(h.REPO / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll', runtime)
    shutil.copy2(h.REPO / 'target/debug/projects', runner)
    tools = {str(p): h.sha(p) for p in (runtime, runner)}
    h.save('tools.json', {'files': tools, 'feature': 'ryupy-driver/ordinary-dynamic-module-pilot',
                          'session_mode': 'default-jit', 'runtime_argument': str(runtime)})
    result = h.OUT / 'gate'
    h.run('project-gate', [sys.executable, '-B', h.REPO / 'tests/run_projects_gate.py',
        '--gate', 'preserve', '--runner', runner, '--runtime', runtime,
        '--source-store', store, '--output', result], timeout=2400)
    actual = json.loads((result / 'run.json').read_text())
    if not actual['completed'] or any(actual['tools'].get(p) != digest for p, digest in tools.items()):
        raise RuntimeError('实际run.json未引用冻结工具或未完成')
    expected = json.loads((h.HERE / 'project-pilot-plans.json').read_text())
    plans = audit_plans(result, expected)
    h.save('progress-promotion.json', require_progress_promotion(result))
    h.save('plan-audit.json', {'run_id': actual['run_id'], 'run_json_sha256': h.sha(result / 'run.json'),
        'contract_sha256': h.sha(h.HERE / 'project-pilot-plans.json'), 'plans': plans})
    if any(h.sha(Path(p)) != digest for p, digest in tools.items()):
        raise RuntimeError('冻结工具被修改')


if __name__ == '__main__':
    main()
