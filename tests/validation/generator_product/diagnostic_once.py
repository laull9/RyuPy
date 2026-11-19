"""单次首边拒绝诊断：只构建普通CLI、运行三个精确Rust测试和原A编译，绝不运行产物。"""
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import signal
import subprocess
import time

TITLE = 'diagnostic-not-acceptance: trace-v1-first-edge-001'
REF = 'refs/heads/ci/wcwidth-legacy-aot-2909056'
SOURCE_SHA = 'be2713d75c2576c7100c0c33ffdf2340d5850de4b367817900e424638dd6d9ed'
FEATURE = 'ryupy-driver/ordinary-dynamic-module-pilot'
TESTS = (
    'generator_product_edge_witness::tests::cleared_empty_bridge_keeps_capture_time_witness_and_rejection',
    'generator_product_edge_witness::tests::real_redirect_still_rejects_and_is_not_described_as_erased_bridge',
    'generator_product_edge_witness::tests::witness_bounds_chains_utf8_and_redacts_literal_contents',
)
REASON = 'pass改变了实际源effect/行号/控制边'
NOT_RUN = ['CP fixtures', 'B/C/D/HOT', 'product execution', 'NativeAOT',
           'trace-rust-preflight acceptance groups', 'S2', 'full make check',
           'S1', '20 projects', 'Legacy baseline and dynamic frames']


def require(value, message):
    if not value:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def save(path, value):
    Path(path).write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n')


def check_context(env, event):
    require(env.get('GITHUB_ACTIONS') == 'true' and env.get('GITHUB_EVENT_NAME') == 'push'
            and env.get('GITHUB_REF') == REF and env.get('GITHUB_RUN_ATTEMPT') == '1',
            '只允许专用ref第一次push诊断，禁止dispatch/rerun')
    require(event.get('head_commit', {}).get('message') == TITLE, '诊断完整提交标题不符')
    require(event.get('ref') == REF and event.get('after') == env.get('GITHUB_SHA')
            and event.get('head_commit', {}).get('id') == env.get('GITHUB_SHA')
            and re.fullmatch('[0-9a-f]{40}', env.get('GITHUB_SHA', '')), 'push源身份不符')
    for key in ('RUSTFLAGS', 'RUSTC_WRAPPER', 'RUSTC_WORKSPACE_WRAPPER', 'CARGO_BUILD_RUSTFLAGS'):
        require(not env.get(key), '未批准构建覆盖：' + key)


def capture(prefix, argv, cwd, timeout):
    """文件直收原流；独立进程组到时只杀本次进程，无截断、无重试。"""
    argv = [str(arg) for arg in argv]
    record = {'argv': argv, 'cwd': str(cwd), 'timeout_seconds': timeout,
              'exit': None, 'failure': None, 'raw_streams_complete': False}
    save(prefix.with_suffix('.argv.json'), record)
    started = time.monotonic()
    with prefix.with_suffix('.stdout').open('xb') as out, prefix.with_suffix('.stderr').open('xb') as err:
        try:
            process = subprocess.Popen(argv, cwd=cwd, stdout=out, stderr=err, start_new_session=True)
            try:
                record['exit'] = process.wait(timeout=timeout)
            except subprocess.TimeoutExpired:
                record['failure'] = 'timeout'
                try:
                    os.killpg(process.pid, signal.SIGKILL)
                except ProcessLookupError:
                    pass
                record['exit'] = process.wait()
            record['raw_streams_complete'] = True
        except OSError as error:
            record['failure'] = type(error).__name__ + ': ' + str(error)
    record['elapsed_seconds'] = time.monotonic() - started
    record['streams'] = {suffix: {'bytes': prefix.with_suffix('.' + suffix).stat().st_size,
                                  'sha256': sha(prefix.with_suffix('.' + suffix))}
                         for suffix in ('stdout', 'stderr')}
    save(prefix.with_suffix('.json'), record)
    return record


def witness(stderr):
    """核有界说明的结构；不把说明当成产品语义或更改比较器的理由。"""
    text = stderr.decode('utf-8')
    require(text.count('[E0310]') == 1 and 'trace V1 整产物拒绝：' in text, '不是唯一真实E0310')
    require(text.count('[edge-witness-v1 fn=') == 1, '缺少或重复首边见证')
    start = text.index(REASON)
    end_match = re.search(r'\[edge-witness-v1 total_truncated=(true|false)\]\n', text)
    require(end_match is not None and text.count('[edge-witness-v1 total_truncated=') == 1,
            '有界见证缺少/重复结束标记')
    payload = text[start:end_match.end()]
    require(len(payload.encode('utf-8')) <= 8192, 'Err说明超过8192字节')
    require(re.search(r'\[edge-witness-v1 fn=\d+ block=bb\d+\]', payload), '结构ID缺失')
    sections = ('captured', 'final', 'old-target-in-final')
    for label in sections:
        require(len(re.findall('^' + re.escape(label) + r'\.terminator=', payload, re.M)) == 1,
                '图来源说明缺失/重复：' + label)
        headers = list(re.finditer('^' + re.escape(label) + r'\.chain\[(\d+)\] [^\n]*\n', payload, re.M))
        require(len(headers) <= 2 and [int(m[1]) for m in headers] == list(range(len(headers))),
                '目标数量/顺序不符')
        for header in headers:
            tail = payload[header.end():]
            nodes = re.match(r'(?:  bb\d+ statements=\d+ empty=(?:true|false) [^\n]*\n)*', tail)[0]
            require(len(nodes.splitlines()) <= 8, '单链超过8节点')
    return {'utf8_bytes': len(payload.encode('utf-8')),
            'sha256': hashlib.sha256(payload.encode('utf-8')).hexdigest(),
            'total_truncated': end_match[1] == 'true', 'text': payload}


def seven_passes(stderr):
    text = stderr.decode('utf-8')
    expected = list(enumerate(('simplify_cfg', 'dead_values', 'numeric_accumulator',
                              'tuple_scalar', 'dead_values', 'interval_narrow', 'float_chain'), 1))
    for event, stage in (('begin', 'pass-run'), ('end', 'pass-run'),
                         ('begin', 'pass-validate'), ('end', 'pass-validate')):
        found = re.findall(r'event=' + event + ' stage=' + stage
                           + r' module="__main__" pass_index=(\d+) pass="([^"]+)"', text)
        require([(int(i), name) for i, name in found] == expected, '七个原pass原流不完整/改变')


def no_product(output):
    for path in output.rglob('*'):
        if path.is_file():
            require(path.suffix.lower() not in ('.dll', '.exe')
                    and path.name != 'ryupy-artifact.json' and path.read_bytes()[:4] != b'\x7fELF',
                    '拒绝之后意外生成产物；禁止执行')


class Runner:
    def __init__(self, repo):
        self.repo = Path(repo).resolve()
        self.out = self.repo / 'target/trace-v1-edge-diagnostic'
        self.out.mkdir(parents=True, exist_ok=False)
        self.work = self.repo / 'target/trace-v1-edge-diagnostic-work'
        self.work.mkdir(parents=True, exist_ok=False)
        self.commands = []
        self.compile_started = False

    def command(self, name, argv, timeout, expected=0):
        require(name not in self.commands, '禁止重复命令：' + name)
        self.commands.append(name)
        record = capture(self.out / name, argv, self.repo, timeout)
        require(record['failure'] is None and record['raw_streams_complete'], name + '未正常结束')
        if expected is not None:
            require(record['exit'] == expected, name + '退出码不符：' + str(record['exit']))
        return record

    def identity(self, label):
        for suffix, args in [('head', ['rev-parse', 'HEAD', 'HEAD^{tree}']),
                             ('status', ['status', '--porcelain=v1', '--untracked-files=all']),
                             ('files', ['ls-files', '-s', '-z'])]:
            self.command(label + '-' + suffix, ['git', *args], 30)
        head, tree = (self.out / (label + '-head.stdout')).read_text().splitlines()
        status = (self.out / (label + '-status.stdout')).read_text()
        files = {}
        for row in (self.out / (label + '-files.stdout')).read_bytes().split(b'\0'):
            if not row:
                continue
            metadata, name = row.split(b'\t', 1)
            mode, blob, stage = metadata.decode().split()
            require(stage == '0' and mode in ('100644', '100755'), '非普通受测文件')
            path = name.decode('utf-8')
            actual_mode = '100755' if (self.repo / path).stat().st_mode & 0o111 else '100644'
            require(mode == actual_mode and not (self.repo / path).is_symlink(), '工作树mode/文件类型改变')
            files[path] = {'mode': mode, 'blob': blob, 'sha256': sha(self.repo / path)}
        result = {'head': head, 'tree': tree, 'status': status, 'files': files}
        save(self.out / (label + '-identity.json'), result)
        return result

    def execute(self):
        result = {'scope': 'diagnostic-not-acceptance', 'accepted': False, 'diagnostic_collected': False,
                  'not_run': NOT_RUN, 'compile_attempts': 0, 'product_execution_count': 0, 'retries': 0}
        before = None
        tools = {}
        source = entry = None
        final_exit = 2
        try:
            event = json.loads(Path(os.environ['GITHUB_EVENT_PATH']).read_text())
            check_context(os.environ, event)
            before = self.identity('before')
            require(before['head'] == os.environ['GITHUB_SHA'] and not before['status'], '源码非clean固定SHA')
            source = self.repo / 'tests/validation/generator_product/goldens/a_all_miss.py'
            require(sha(source) == SOURCE_SHA, '原A输入改变')
            entry = self.work / 'entry.py'
            shutil.copyfile(source, entry)
            shutil.copyfile(source, self.out / 'source.py')
            save(self.out / 'source.json', {'origin': str(source.relative_to(self.repo)),
                 'copy': str(entry), 'sha256': SOURCE_SHA, 'executed_as_python': False})
            versions = {'rustc': 'rustc 1.99.1 (58cf8f9c3 2026-10-13)',
                        'cargo': 'cargo 1.99.1 (5f94df478 2026-08-27)'}
            for tool, version in versions.items():
                path = Path(shutil.which(tool) or '').resolve()
                require(path.is_file(), '固定Rust工具不存在：' + tool)
                self.command(tool + '-version', [path, '--version'], 30)
                require((self.out / (tool + '-version.stdout')).read_text().strip() == version,
                        '固定Rust版本不符：' + tool)
                tools[tool] = {'path': str(path), 'sha256': sha(path), 'version': version}
            self.command('compiler-build', ['cargo', 'build', '--locked', '-p', 'ryupy-cli',
                         '--features', FEATURE], 600)
            cli = self.repo / 'target/debug/ryupy'
            tools['cli'] = {'path': str(cli), 'sha256': sha(cli), 'feature': FEATURE, 'configuration': 'debug'}
            save(self.out / 'tools.json', tools)
            tests_start = time.monotonic()
            for index, name in enumerate(TESTS):
                remaining = 180 - (time.monotonic() - tests_start)
                require(remaining > 0, '三个诊断测试预算用尽')
                label = 'diagnostic-test-' + str(index + 1)
                self.command(label, ['cargo', 'test', '--locked', '-p', 'ryupy-mir', '--lib', name,
                             '--', '--exact', '--test-threads=1', '--color', 'never'], remaining)
                text = (self.out / (label + '.stdout')).read_text()
                require('test ' + name + ' ... ok' in text
                        and re.search(r'test result: ok\. 1 passed; 0 failed; 0 ignored; 0 measured;', text),
                        '精确诊断测试未真实执行一次：' + name)
            require(sha(cli) == tools['cli']['sha256'] and sha(entry) == SOURCE_SHA, '诊断前输入/CLI改变')
            self.compile_started = True
            result['compile_attempts'] = 1
            output = self.work / 'output'
            compiled = self.command('compile', [cli, '--trace-product-v1', 'build', entry,
                                    '--diagnostic-stages', '--out-dir', output], 60, expected=None)
            result['compile_exit'] = compiled['exit']
            require(compiled['exit'] == 1, '预期E0310 exit1，实际：' + str(compiled['exit']))
            require((self.out / 'compile.stdout').read_bytes() == b'', '拒绝出现stdout')
            seven_passes((self.out / 'compile.stderr').read_bytes())
            observed = witness((self.out / 'compile.stderr').read_bytes())
            save(self.out / 'witness.json', observed)
            no_product(output)
            require(sha(source) == sha(entry) == SOURCE_SHA and sha(cli) == tools['cli']['sha256'],
                    '诊断后源/CLI改变')
            result['diagnostic_collected'] = True
            final_exit = 1  # 真实编译仍拒绝；不能把诊断采集成功冒充验收绿灯。
        except Exception as error:
            result['error'] = type(error).__name__ + ': ' + str(error)
        finally:
            # 即使超时、见证检查或最终身份采集失败，也保留首次compile的原始结果。
            if (self.out / 'compile.json').exists():
                raw_compile = json.loads((self.out / 'compile.json').read_text())
                result['compile_exit'] = raw_compile['exit']
                result['compile_failure'] = raw_compile['failure']
                result['compile_raw_streams_complete'] = raw_compile['raw_streams_complete']
            if before is not None:
                try:
                    after = self.identity('after')
                    require(after == before, '采集期间完整源码身份改变')
                    after_tools = {name: dict(record, sha256=sha(record['path']))
                                   for name, record in tools.items()}
                    save(self.out / 'tools-after.json', after_tools)
                    require(after_tools == tools, '采集期间工具身份改变')
                    if entry is not None:
                        require(sha(source) == sha(entry) == SOURCE_SHA, '采集期间原A或副本改变')
                    result['source_unchanged'] = True
                except Exception as error:
                    result['identity_error'] = type(error).__name__ + ': ' + str(error)
                    result['diagnostic_collected'] = False
                    final_exit = 2
            result['commands'] = self.commands
            result['collector_exit'] = final_exit
            save(self.out / 'result.json', result)
            print(json.dumps({k: v for k, v in result.items() if k != 'commands'}, ensure_ascii=False))
            summary = '# diagnostic-not-acceptance\n\n真实编译拒绝不是产品验收。\n\n'
            summary += json.dumps({k: v for k, v in result.items() if k != 'commands'}, ensure_ascii=False, indent=2)
            summary += '\n\n以下全门本轮 not-run，既有红灯未改变：\n' + '\n'.join('- ' + x for x in NOT_RUN) + '\n'
            (self.out / 'summary.md').write_text(summary)
            if os.environ.get('GITHUB_STEP_SUMMARY'):
                with open(os.environ['GITHUB_STEP_SUMMARY'], 'a') as stream:
                    stream.write(summary)
        return final_exit


if __name__ == '__main__':
    raise SystemExit(Runner(Path(__file__).resolve().parents[3]).execute())
