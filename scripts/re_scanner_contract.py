"""Scanner 验收纯控制合同：复用旧双流/TRX 校验，拒绝残流、换源和旧恢复状态。"""
import hashlib
from pathlib import Path
import textwrap_dedent_contract as shared

ROOT = shared.ROOT
MANIFEST = ROOT / 'tests/validation/re_scanner.json'
TITLE = 'validation: re-scanner-001'
require, sha, parse, relative = shared.require, shared.sha, shared.parse, shared.relative
verify_output, verify_trx = shared.verify_output, shared.verify_trx
TIMEOUT_OUTPUTS = {'resource-timeout RESOURCE_COMPLETED\n': False,
                   'resource-timeout RESOURCE_TIMED_OUT\n': True}
RUNTIME_PATHS = {f'runtime/RyuPy.Runtime/{name}.cs' for name in (
    'PyRegex', 'PyRegexPattern', 'PyRegexSplit', 'PyRegexMatch', 'PyRegexParser', 'PyRegexBytes',
    'PyRegexBytesOperations', 'PyRegexMutableBytes', 'PyRegexMutableIterator',
    'PyRegexMutableSubstitution', 'PyRegexScanner', 'PyRegexScannerPattern')} | {
    f'runtime/RyuPy.Runtime.Tests/PyRegexScanner{name}Tests.cs'
    for name in ('Boundary', 'Construction', 'Domain', 'State')}


def safe_file(root, name):
    file = root / relative(name)
    require(file.is_file() and not file.is_symlink() and file.resolve() == file.absolute(),
            '原件缺失/路径含symlink：' + name)
    return file


def verify_process(prefix, expected_argv, exits=(0, 1)):
    """仅核真实自然退出及原件身份；语义红不能替代launch/信号/超时或结构失败。"""
    record = parse(prefix.with_suffix('.json').read_bytes())
    started = parse(prefix.with_suffix('.argv.json').read_bytes())
    require(type(record) is dict and type(started) is dict
            and {'argv', 'exit', 'failure', 'raw_streams_complete', 'streams'} <= set(record), '原进程结构不完整')
    require(record['argv'] == started.get('argv') == list(map(str, expected_argv)), '原进程argv不符')
    require(type(record['exit']) is int and record['exit'] in exits
            and record['failure'] is None and record['raw_streams_complete'] is True,
            '不是允许的完整自然退出')
    require(type(record['streams']) is dict and set(record['streams']) == {'stdout', 'stderr'}, '原进程双流记录不完整')
    streams = {}
    for suffix in ('stdout', 'stderr'):
        path = prefix.with_suffix('.' + suffix)
        streams[suffix] = path.read_bytes()
        metadata = record['streams'][suffix]
        require(type(metadata) is dict and type(metadata.get('bytes')) is int
                and metadata == {'bytes': len(streams[suffix]), 'sha256': hashlib.sha256(streams[suffix]).hexdigest()},
                '原进程双流长度/hash与record矛盾')
    return record, streams


def semantic_result(record, streams, expected):
    """只接收已核原件的bytes，原exit0/完整双流成功谓词不变；此处不做I/O或吞异常。"""
    detail = {}
    golden = expected['stdout'].encode()
    if expected.get('observation') == 'regex-timeout':
        choices = {text.encode(): observed for text, observed in TIMEOUT_OUTPUTS.items()}
        if streams['stdout'] in choices:
            golden = streams['stdout']
            detail = dict(branch=golden.decode().strip().split(' ')[1], timeout_observed=choices[golden])
    passed = record['exit'] == 0 and streams['stdout'] == golden and streams['stderr'] == b''
    return dict(passed=passed, status='passed' if passed else 'semantic_failed', actual_exit=record['exit'], **detail)


def verify_timeout_output(record, prefix):
    """仅timeout观测允许这两条完整流；进程超时、任意异常和残流仍全部失败。"""
    actual = prefix.with_suffix('.stdout').read_bytes().decode('utf-8')
    require(actual in TIMEOUT_OUTPUTS, 'timeout观测不是精确完成/资源异常marker')
    verify_output(record, prefix, actual)
    return dict(branch=actual.strip().split(' ')[1], timeout_observed=TIMEOUT_OUTPUTS[actual])


def verify_command(prefix, expected_argv):
    """构建/C#命令没有固定stdout，但必须完整成功并绑定真实argv、双流长度与hash。"""
    record, _ = verify_process(prefix, expected_argv, exits=(0,))
    return record


def load_manifest(root=ROOT, path=MANIFEST):
    data = parse(path.read_bytes())
    require(data['schema_version'] == 1 and data['title'] == TITLE, 'Scanner 清单版本/标题不符')
    require(data['cp_sha256'] == shared.old.shared.PYTHON_SHA and data['cp_version'] == '3.12.13', '固定 CP 身份不符')
    require(data['default_project_delta'] == 0, 'Scanner 不能修改默认项目分子')
    actual = {str(p.relative_to(root)) for folder in ('tests/cases/re_scanner', 'tests/fixtures/re_scanner')
              for p in (root / folder).rglob('*') if p.is_file()}
    require(actual == set(data['sources']), 'Scanner 输入集合漏项/增项')
    for name, digest in data['sources'].items():
        path = root / relative(name)
        require(path.is_file() and not path.is_symlink() and path.resolve() == path.absolute()
                and sha(path) == digest, 'Scanner 输入身份不符：' + name)
    require(data['runtime_candidate']['revision'] == 'runtime-abi-b-001'
            and set(data['runtime_candidate']['sources']) == RUNTIME_PATHS,
            '必须绑定当前B载体的精确12生产源与4测试源')
    for name, digest in {**data['old_protected'], **data['runtime_candidate']['sources']}.items():
        require(sha(root / relative(name)) == digest, '最终runtime/旧保护源身份不符：' + name)
    identifiers = set()
    for graph in data['graphs']:
        require(graph['id'] not in identifiers, 'Scanner 图重复')
        identifiers.add(graph['id'])
        require(graph['mode'] in ('candidate', 'pending', 'ni'), '正面/NI/pending 必须分账')
        require(graph['source_sha256'] == data['sources'][graph['source']], '图未绑定实际源码')
        require(graph['expected_exit'] == 0 and graph['expected_stderr'] == '', '正面/精确 NI 门只接受完整成功流')
        require(graph['native'] == (graph['mode'] != 'pending'), 'pending 不得计 Native 支持')
        require(graph['cp_argv'] == (None if graph['mode'] == 'ni' else ['all']), 'CP 窗口只允许整图一次')
        require(graph['expected_stdout'] == ''.join(row['expected_stdout'] for row in graph['scenarios']), '整图预期与逐 argv 不符')
        names = [tuple(row['argv']) for row in graph['scenarios']]
        require(len(names) == len(set(names)), '同图 argv 重复')
        for row in graph['scenarios']:
            if row.get('observation') is not None:
                require(graph['id'] == 'negative' and row['argv'] == ['timeout']
                        and row['observation'] == 'regex-timeout'
                        and row['stdout_alternatives'] == list(TIMEOUT_OUTPUTS), '只有固定timeout场景允许两条精确完整流')
        if graph['mode'] != 'ni':
            require(b'# ryupy: pending' in (root / graph['source']).read_bytes(), '尚未验收的正面合同丢 pending')
    real = data['real_workload']
    require(real['stdout_bytes'] == 202 and sha(root / real['golden']) == real['stdout_sha256']
            and len((root / real['golden']).read_bytes()) == 202, '完整 attr_list 202B 原流改变')
    return data


def verify_oracle(graph, record, prefix, python_sha256, source_sha256):
    require(graph['mode'] in ('candidate', 'pending'), 'NI 不得冒称 CP oracle')
    require(python_sha256 == shared.old.shared.PYTHON_SHA
            and source_sha256 == graph['source_sha256'], 'CP 实际工具/输入身份不符')
    verify_output(record, prefix, graph['expected_stdout'])
    return {'status': 'observed', 'source_sha256': source_sha256,
            'python_sha256': python_sha256,
            'stdout_sha256': hashlib.sha256(prefix.with_suffix('.stdout').read_bytes()).hexdigest()}


def fail_closed(stages, reason):
    # 恢复校验失败必须清掉旧 passed；调用方仍需保存 state 原件，不自动重跑任何已开始命令。
    return {name: {'passed': False, 'status': 'invalidated-identity', 'reason': str(reason)} for name in stages}


def verify_checkpoint(state, fresh_identity, expected_runs, root, required_files=()):
    """先核全部执行身份再分类语义；这里只返回重算结果，不授权Native采集。"""
    require(type(state) is dict and set(state) == {'schema_version', 'identity', 'runs', 'files'}, '恢复 state 结构不符')
    require(state['schema_version'] == 1 and state['identity'] == fresh_identity, '恢复 state 与本轮身份不符')
    require(set(fresh_identity) == {'head', 'tree', 'run_id', 'run_attempt', 'manifest_sha256', 'sources', 'tools'}, '本轮完整身份缺失')
    require(fresh_identity['run_attempt'] == '1', '不允许自动重试补采')
    require(type(state['runs']) is dict and set(state['runs']) == set(expected_runs), '恢复原流场景漏项/越界')
    require(type(state['files']) is dict and state['files'], 'PE/TRX 等原件清单为空')
    require(set(required_files) <= set(state['files']), '恢复缺少强制PE/TRX/工具原件')
    for name, digest in state['files'].items():
        require(sha(safe_file(root, name)) == digest, 'checkpoint 原件改变：' + name)
    processes = {}
    for name, expected in expected_runs.items():
        row = state['runs'][name]
        require(set(row) == {'record', 'prefix', 'argv', 'source_sha256'}, '原进程记录缺字段/混入旧 passed')
        require(row['argv'] == expected['argv'] and row['source_sha256'] == expected['source_sha256'], '原进程 argv/输入换源')
        require(row['prefix'] == name, '原进程prefix必须等于唯一预定ID')
        prefix = root / relative(name)
        # 两份流必须同时属于已重核文件清单；仅存在同名文件不足以证明是这次执行。
        require(all(str(prefix.with_suffix('.' + suffix).relative_to(root)) in state['files']
                    for suffix in ('stdout', 'stderr', 'json', 'argv.json')), '恢复缺少绑定的双流/原命令记录')
        original, streams = verify_process(prefix, expected['argv'])
        require(original == row['record'],
                'state与原进程record/argv矛盾，不能靠修改exit沿用通过')
        if expected.get('observation') is not None:
            require(name == 'negative-timeout-jit-run' and expected['observation'] == 'regex-timeout', '不得放宽其它JIT原流')
        processes[name] = (original, streams)
    return {name: semantic_result(*processes[name], expected) for name, expected in expected_runs.items()}


def check_context(env, event):
    require(env.get('RUNNER_ENVIRONMENT') == 'github-hosted' and env.get('GITHUB_ACTIONS') == 'true'
            and env.get('GITHUB_EVENT_NAME') == 'push' and env.get('GITHUB_RUN_ATTEMPT') == '1'
            and env.get('GITHUB_REF') == event.get('ref') == shared.old.shared.REF, '仅专用 hosted 首次 push')
    head = event.get('head_commit') or {}
    require(head.get('message') in (TITLE, 'validation: decimal-percent-001', 'validation: keyerror-construction-001', 'validation: treebuilder-state-001') and head.get('id') == env.get('GITHUB_SHA')
            and event.get('after') == env.get('GITHUB_SHA'), 'Scanner 精确标题/提交不符')
    for key in ('RUSTFLAGS', 'RUSTC_WRAPPER', 'RUSTC_WORKSPACE_WRAPPER', 'CARGO_ENCODED_RUSTFLAGS',
                'RUSTUP_TOOLCHAIN', 'CARGO_TARGET_DIR', 'LD_PRELOAD', 'LD_LIBRARY_PATH'):
        require(not env.get(key), '不接受工具注入：' + key)


def qualified_oracles(data):
    for graph in data['graphs']:
        if graph['mode'] == 'ni':
            continue
        row = graph['oracle']
        require(graph['oracle_status'] == 'observed' and row['assertions_enabled'] is True
                and row['source_sha256'] == graph['source_sha256']
                and row['python_sha256'] == data['cp_sha256'] and row['exit'] == row['stderr_bytes'] == 0,
                '未完成同源固定 CP；不把预测当 oracle')
        stdout, stderr = (ROOT / relative(row[name + '_file']) for name in ('stdout', 'stderr'))
        require(stdout.read_bytes() == graph['expected_stdout'].encode() and sha(stdout) == row['stdout_sha256']
                and stdout.stat().st_size == row['stdout_bytes'] and stderr.read_bytes() == b'', 'CP 参考完整双流改变')


def native_cases(graph):
    return [dict(id=graph['id'] + '-' + row['argv'][0], source=graph['source'], include=[],
                 modules={'__main__': graph['source']}, argv=row['argv'], stdout=row['expected_stdout'],
                 mode='ni' if graph['mode'] == 'ni' else 'cp', observation=row.get('observation')) for row in graph['scenarios']]
