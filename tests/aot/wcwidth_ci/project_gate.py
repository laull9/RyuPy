"""官方锁定源码恢复后复用P0逐ID门禁；冻结工具，无历史结果复用。"""
import json
import os
from pathlib import Path
import shutil
import sys
import tarfile
import run as h
sys.path.insert(0, str(Path(__file__).resolve().parents[3] / "scripts"))
import default_source_deployment_validation_plans as strict


# 只批准已验收的普通MI/ABC源码编译晋升，不扩大默认模块选择或候选身份。
SORTEDCONTAINERS_COMPILED_PROMOTIONS = frozenset({
    'sortedcontainers.sorteddict', 'sortedcontainers.sortedlist', 'sortedcontainers.sortedset',
})


# 真实toml原入口及扩展四图双PIC通过后，仅准许这三个Legacy模块编译晋升。
TOML_COMPILED_PROMOTIONS = frozenset({'toml.decoder', 'toml.encoder', 'toml.tz'})

# 仅单模块编译进展：click 原入口仍 unsupported，不计项目成功。
CLICK_COMPILED_PROMOTIONS = frozenset({'click.core'})

def audit_plans(result, expected):
    """逐项目核实际计划；固定选择集合不等于所有被选模块已编译/执行。"""
    originals = strict.baseline(expected)
    matrix = json.loads((result / 'matrix.json').read_text())
    summary = json.loads((result / 'summary.json').read_text())
    strict.require(summary['valid'] is True and summary['denominator'] == 20
                   and summary['cp_passed'] == 20 and summary['preserve_pass'] is True
                   and not summary['protected_lost'] and len(matrix) == 20
                   and {r['id'] for r in matrix} == set(originals)
                   and all(r['run_id'] == summary['run_id'] for r in matrix), '完整同轮保护结果不符')
    toml = [row for row in matrix if row['id'] == 'toml']
    strict.require(len(toml) == 1 and toml[0]['status'] == 'native'
                   and type(toml[0]['run_exit']) is int and toml[0]['run_exit'] == 0
                   and toml[0]['failure_kind'] == '' and toml[0]['mode'] == 'default-jit',
                   'toml原入口未在同轮真实通过；不能只凭计划晋升')
    plans = {}
    for pid, selected in expected['selected'].items():
        path = result / pid / 'native' / pid / 'module-environment.json'
        actual = json.loads(path.read_text())
        additions = strict.audit_old_rows(actual, originals[pid], selected,
            compiled_promotions=SORTEDCONTAINERS_COMPILED_PROMOTIONS
            if pid == 'sortedcontainers' else TOML_COMPILED_PROMOTIONS
            if pid == 'toml' else CLICK_COMPILED_PROMOTIONS
            if pid == 'click' else frozenset())
        level = strict.evidence_layer(result, pid, matrix)
        if pid in {'markdown', 'pyparsing'}:
            strict.audit_candidates(additions, pid, compiled=level == 'deployment')
        else:
            strict.require(not additions, '普通18图出现未批准候选：' + pid)
        output = path.parent
        if not additions:
            strict.require(not (output / 'optional-sources.json').exists(),
                           '普通18图出现未批准的optional报告：' + pid)
        cross = {'level': level, 'source_absolute': 'not-run', 'optional_cross_audit': 'not-run'}
        if additions and (level == 'deployment' or (output / 'optional-sources.json').exists()):
            cross = strict.audit_deployment(output, additions, pid,
                h.REPO / 'third_party/projects/markdown-3.5.2/Markdown-3.5.2')
        plans[pid] = {'sha256': h.sha(path), 'selected': selected, 'evidence': cross}

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
        'contract_sha256': h.sha(h.HERE / 'project-pilot-plans.json'),
        'full_graph_contract_sha256': h.sha(strict.CONTRACT), 'plans': plans})
    if any(h.sha(Path(p)) != digest for p, digest in tools.items()):
        raise RuntimeError('冻结工具被修改')


if __name__ == '__main__':
    main()
