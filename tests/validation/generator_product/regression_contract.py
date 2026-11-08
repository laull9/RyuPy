"""只读复用同源完整门证据；独立构建按各自计划核身份，不强求跨模式二进制同字节。"""
import importlib
import json
import re
from pathlib import Path
import sys
import build_provenance as provenance
from product_contract import require, sha


def same_source(actual, product):
    expected = product['candidate']
    require(actual['head'] == expected['head'] and not actual['status']
            and actual['files'] == expected['files'], '不是同最终HEAD及完整源码身份')


def fixed20(repo, evidence, product_freeze):
    """全20逐行复用原验证；九protected+progress为原十项，非只查总数。"""
    product = provenance.freeze(product_freeze)
    sys.path.insert(0, str(repo / 'tests'))
    old = importlib.import_module('project_gate_contract')
    contract, projects = old.load_inputs(repo)
    root = evidence / 'gate'
    actual = json.loads((root / 'run.json').read_text())
    summary = json.loads((root / 'summary.json').read_text())
    rows = json.loads((root / 'matrix.json').read_text())
    require(actual['completed'] is True, 'P0未完成')
    same_source(actual['candidate'], product)
    require(actual['contract'] == contract, '实际P0合同被更改')
    recomputed = old.evaluate(contract, rows)
    require(recomputed['preserve_pass'] and recomputed['promotion_pass'], '原十个成功ID有丢失')
    require(summary['valid'] is True and summary['cp_passed'] == 20
            and summary['run_id'] == actual['run_id'], 'P0完成身份/CP不完整')
    require(all(summary.get(key) == value for key, value in recomputed.items()), 'P0摘要与逐ID矩阵矛盾')
    frozen = json.loads((evidence / 'tools.json').read_text())
    require(frozen['feature'] == 'ryupy-driver/ordinary-dynamic-module-pilot'
            and frozen['session_mode'] == 'default-jit', 'P0不是既有pilot构建/默认JIT计划')
    require(all(actual['tools'].get(path) == digest for path, digest in frozen['files'].items()), 'P0实际runtime/runner不符')
    for path, digest in frozen['files'].items():
        require(sha(evidence / 'frozen' / Path(path).name) == digest, 'P0本轮冻结工具原件hash不符')
    runtime_path = frozen['runtime_argument']
    runtime_sha = frozen['files'][runtime_path]
    commands = json.loads((evidence / 'commands.json').read_text())
    provenance.command_evidence(evidence, commands, 'runtime-build', ['make', 'runtime'])
    provenance.command_evidence(evidence, commands, 'runner-build', ['cargo', 'build', '--locked', '--features',
        'ryupy-driver/ordinary-dynamic-module-pilot', '-p', 'ryupy-e2e', '--bin', 'projects'])
    command = provenance.command_evidence(evidence, commands, 'project-gate')
    argv = command['argv']
    require(argv.count('--runtime') == 1 and argv[argv.index('--runtime') + 1] == runtime_path,
            'P0未显式选择本job冻结runtime')
    if 'p0-runtime' in product['contract'].get('shared_artifacts', []):
        require(runtime_sha == product['contract']['runtime']['sha256'], '宣称共用的P0/runtime实际不同')
    for row in rows:
        pid = row['id']
        for name, digest in row['evidence'].items():
            relative = Path(name)
            require(not relative.is_absolute() and '..' not in relative.parts, 'P0证据路径越界')
            require(sha(root / relative) == digest, 'P0逐文件证据改变：' + name)
        directory = root / pid / 'native' / pid
        project = next(p for p in projects if p['id'] == pid)
        golden = repo / 'tests/projects/baselines' / pid / 'stdout.txt'
        # 所有20行，不只native：沿用身份、failure_kind、编译错误与产物互斥等既有验证。
        old.validate_result(row, project, actual['run_id'], directory, golden, runtime_sha)
        if row['status'] == 'native':
            require((directory / 'jit.stderr').read_bytes() == b'', '原成功ID stderr不符：' + pid)
    protected = set(contract['protected_native']) | set(contract['promotion_requires'])
    require(len(protected) == 10, '保护合同不是本轮原十项')
    return {'all_ids': contract['project_ids'], 'protected_ids': [p for p in contract['project_ids'] if p in protected],
            'results_by_id': {r['id']: r['status'] for r in rows}, 'run_id': actual['run_id'],
            'common_commit': product['contract']['commit'], 'common_tree': product['contract']['tree'],
            'runtime_sha256': runtime_sha, 'runtime_matches_product': runtime_sha == product['contract']['runtime']['sha256'],
            'identity_scope': 'same frozen sources; independently built qualified-pilot/default-jit artifacts'}


def fullcheck(evidence, product_freeze, mode):
    """新增identity只在原流程内采集；缺失旧artifact不可通过，也不为补证重跑。"""
    product = provenance.freeze(product_freeze)
    require(mode in ('default', 'pilot'), '未知模式')
    actual = json.loads((evidence / 'mode.json').read_text())
    result = json.loads((evidence / 'result.json').read_text())
    commands = json.loads((evidence / 'commands.json').read_text())
    require(actual['commit'] == product['contract']['commit'] and actual['mode'] == mode
            and result == {'passed': True, 'mode': mode}, 'fullcheck未同HEAD完整通过')
    features = 'ryupy-driver/ordinary-dynamic-module-pilot' if mode == 'pilot' else ''
    provenance.command_evidence(evidence, commands, 'make-check', ['make', 'check', 'CARGO_FEATURES=' + features])
    provenance.command_evidence(evidence, commands, 'git', ['git', 'rev-parse', 'HEAD', 'HEAD^{tree}'])
    require((evidence / 'git.stdout').read_text().splitlines() == [product['contract']['commit'], product['contract']['tree']],
            'fullcheck真实git原流不符')
    # 原流程每项command都必须有两条原流；不能只用三个summary JSON制造通过。
    for command in commands:
        for suffix in ('.stdout', '.stderr'):
            require((evidence / (command['name'] + suffix)).is_file(), 'fullcheck命令原流缺失')
    identity = json.loads((evidence / 'build-identity.json').read_text())
    require(identity['mode'] == mode and identity['cargo_features'] == features, 'fullcheck构建配置不符')
    same_source(identity['candidate_before'], product)
    same_source(identity['candidate_after'], product)
    require(identity['tools_before'] == identity['tools_after']
            and set(identity['tools_before']) == {'python', 'dotnet', 'cargo', 'rustc'}, 'fullcheck实际工具改变/缺失')
    for tool in identity['tools_before'].values():
        require(Path(tool['path']).is_absolute() and re.fullmatch('[0-9a-f]{64}', tool['sha256']), 'fullcheck工具身份格式错误')
    require(identity['tools_before']['python']['sha256'] == actual['python_sha256'], 'fullcheck实际Python身份不符')
    runtimes = identity['runtimes_before_tests']
    require(runtimes == identity['runtimes_after_tests'] and set(runtimes) == {'managed-tests', 'driver-e2e'},
            'fullcheck两个实际runtime role在测试前后改变/缺失')
    for role, runtime in runtimes.items():
        require(Path(runtime['path']).is_absolute() and re.fullmatch('[0-9a-f]{64}', runtime['sha256']), 'runtime身份格式错误')
        if 'fullcheck-' + mode + '-' + role in product['contract'].get('shared_artifacts', []):
            require(runtime['sha256'] == product['contract']['runtime']['sha256'], '宣称共用的fullcheck runtime不同')
    return {'mode': mode, 'head': product['contract']['commit'], 'make_check_exit': 0,
            'runtimes': runtimes, 'runtime_matches_product': {role: runtime['sha256'] == product['contract']['runtime']['sha256'] for role, runtime in runtimes.items()},
            'identity_scope': 'same frozen sources; independent fullcheck build configuration'}


def isolated_experiments(repo, evidence, head):
    sys.path.insert(0, str(repo / 'tests/aot/wcwidth_ci'))
    old = importlib.import_module('trace_rust_preflight')
    result = json.loads((evidence / 'result.json').read_text())
    codes = result['exit_codes']
    outputs = {name: (evidence / (name + '.stdout')).read_text()
               for name, code in codes.items() if code is not None}
    actual = old.aggregate(codes, outputs)
    require(actual['success'] and outputs['git-head'].strip() == head, '既有Rust安全门未同HEAD通过')
    require(actual['isolated_experiments'] == result['isolated_experiments'], '四实验隔离记录矛盾')
    require({k: len(v) for k, v in old.EXPERIMENTAL.items()} == {'frame': 3, 'dataflow': 1}, '只能隔离既有3+1')
    return actual['isolated_experiments']


def pic_mechanism(trx):
    """只核原完整C#门中的精确Theory两项；不是产品hit计数，不能替代产品双流。"""
    import xml.etree.ElementTree as ET
    name = 'RyuPy.Runtime.Tests.PyTraceProductPicTests.ColdAndHotGetterFailureForwardSameOwnerWithoutLegacyAttach'
    rows = [row for row in ET.parse(trx).findall('.//{*}UnitTestResult')
            if row.attrib.get('testName', '').startswith(name)]
    require(len(rows) == 2 and len({row.attrib.get('testId') for row in rows}) == 2,
            'PyPic机制Theory必须实际两项/不重复')
    observed = set()
    for row in rows:
        require(row.attrib.get('outcome') == 'Passed', 'PyPic机制有失败/跳过')
        match = re.fullmatch(re.escape(name) + r'\(warm: (False|True), site: (3801|3802)\)', row.attrib['testName'])
        require(match is not None, 'PyPic机制Theory实参身份未知')
        observed.add(match.groups())
    require(observed == {('False', '3801'), ('True', '3802')}, '冷热两个实际分支不完整')
    return {'tests': [row.attrib['testName'] for row in rows], 'trx_sha256': sha(trx),
            'scope': 'direct C# cold/warm mechanism only; no generated product hit counts'}
