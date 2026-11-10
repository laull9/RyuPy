#!/usr/bin/env python3
"""hosted单次Markdown原双调用预检；非验收，纯自测不下载/构建/执行产品。"""
import argparse
import hashlib
import json
import os
import re
from pathlib import Path, PurePosixPath
import shutil
import signal
import subprocess
import sys
import tarfile
import tempfile
import time
import traceback
import unittest

REPO = Path(__file__).resolve().parents[2]
TITLE = 'diagnostic-not-acceptance: markdown-preflight-001'
REF = 'refs/heads/ci/wcwidth-legacy-aot-2909056'
BASE_TREE = '955fda16bb06eb9239c1637b6a8862eb27ebb1b4'
BASE_SOURCE_HEAD = '24b92770396996b33ba81fc9f25b7115f395fe21'
FEATURE = 'ryupy-driver/ordinary-dynamic-module-pilot'
ALLOWED = {'.github/workflows/wcwidth-legacy-aot.yml', 'tests/validation/markdown_preflight.py',
           'tests/validation/generator_product/test_diagnostic_once.py'}
ENTRY_SHA = '78a03254033372f140726edaa44e1b9b70238c591b28403cdd4fdf30a45c0777'
GOLDEN_SHA = 'a824d6deda7b3f0a3f627045ea4046baa2ac6e49ec62e22ea4b367ae759923cf'
ARCHIVE_SHA = 'e1ac7b3dc550ee80e602e71c1d168002f062e49f1b11e26a36264dafd4df2ef8'
PYTHON_SHA = '021044895e95be79dc2f110367607e684119afbc8ce75f6f0eec94844e0acec7'
STDLIB = {
    'html/__init__.py': '923d82d821e75e8d235392c10c145ab8587927b3faf9c952bbd48081eebd8522',
    'html/entities.py': 'd9c65fb2828dbc1f3e399058a341d51e9375ec5bca95a8e92599c41bd5b78bde',
    'html/parser.py': '952f1d7dd5be98b6f78801c6904a38e3e2beceb4783efb76c5e22948f0a58092',
    '_markupbase.py': 'cb14dd6f2e2439eb70b806cd49d19911363d424c2b6b9f4b73c9c08022d47030',
}
INCLUDES = ['html.parser'] + ['markdown.extensions.' + name for name in
    ['extra', 'fenced_code', 'footnotes', 'attr_list', 'def_list', 'tables', 'abbr', 'md_in_html']]
NOT_RUN = ['full make check (default/pilot)', '20 default projects and all 11 protected IDs',
           'NativeAOT and relocation', 'Legacy/Instance/Trace regression gates',
           'source provenance and import-path regression gates', 'performance',
           'Markdown upstream suite and full extension feature coverage']


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as stream:
        for data in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(data)
    return h.hexdigest()


def save(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n')


def check_context(env, event):
    require(env.get('GITHUB_ACTIONS') == 'true' and env.get('GITHUB_EVENT_NAME') == 'push'
            and env.get('GITHUB_REF') == REF and event.get('ref') == REF
            and env.get('GITHUB_RUN_ATTEMPT') == '1', '仅允许专用分支首次push，不自动重试')
    head = event.get('head_commit') or {}
    require(head.get('message') == TITLE and head.get('id') == env.get('GITHUB_SHA')
            and event.get('after') == env.get('GITHUB_SHA'), '精确标题或CI提交身份不符')
    for name in ('RUSTFLAGS', 'RUSTC_WRAPPER', 'RUSTC_WORKSPACE_WRAPPER', 'CARGO_ENCODED_RUSTFLAGS',
                 'RUSTUP_TOOLCHAIN', 'CARGO_TARGET_DIR', 'LD_PRELOAD', 'LD_LIBRARY_PATH'):
        require(not env.get(name), '不接受未声明工具注入：' + name)


def tree_records(raw):
    rows = {}
    for row in raw.split(b'\0'):
        if row:
            meta, name = row.split(b'\t', 1)
            mode, kind, blob = meta.decode().split()
            require(kind == 'blob' and mode in ('100644', '100755'), '拒绝非普通源码')
            rows[name.decode()] = {'mode': mode, 'blob': blob}
    return rows


def source_delta(base, current):
    changed = {name for name in base.keys() | current.keys() if base.get(name) != current.get(name)}
    require(changed <= ALLOWED, '与24基树相比出现未授权差异：' + ', '.join(sorted(changed - ALLOWED)))
    return sorted(changed)


def capture(prefix, argv, cwd, timeout, env=None, limit=32 * 1024 * 1024):
    """启动前保存命令；超时/超量杀本进程组，保已写原流，不重跑。"""
    argv = list(map(str, argv))
    record = {'argv': argv, 'cwd': str(cwd), 'timeout_seconds': timeout, 'exit': None,
              'failure': None, 'raw_streams_complete': False}
    save(prefix.with_suffix('.argv.json'), record)
    start = time.monotonic()
    process = None
    with prefix.with_suffix('.stdout').open('xb') as out, prefix.with_suffix('.stderr').open('xb') as err:
        try:
            process = subprocess.Popen(argv, cwd=cwd, env=env, stdin=subprocess.DEVNULL,
                                       stdout=out, stderr=err, start_new_session=True)
            while process.poll() is None:
                if time.monotonic() - start > timeout:
                    raise TimeoutError('single window expired')
                if os.fstat(out.fileno()).st_size > limit or os.fstat(err.fileno()).st_size > limit:
                    raise RuntimeError('stream limit exceeded')
                time.sleep(.05)
            record['exit'] = process.returncode
            require(os.fstat(out.fileno()).st_size <= limit and os.fstat(err.fileno()).st_size <= limit,
                    'stream limit exceeded')
            record['raw_streams_complete'] = True
        except Exception as error:
            record['failure'] = type(error).__name__ + ': ' + str(error)
            if process is not None:
                try:
                    os.killpg(process.pid, signal.SIGKILL)
                except ProcessLookupError:
                    pass
                record['exit'] = process.wait(timeout=2)
    record['elapsed_seconds'] = time.monotonic() - start
    record['streams'] = {key: {'bytes': prefix.with_suffix('.' + key).stat().st_size,
                              'sha256': sha(prefix.with_suffix('.' + key))} for key in ('stdout', 'stderr')}
    save(prefix.with_suffix('.json'), record)
    return record


def selected_member(member):
    path = PurePosixPath(member.name)
    require(not path.is_absolute() and '..' not in path.parts and not member.issym()
            and not member.islnk(), '不安全归档成员')
    return member.isfile() and (member.name.startswith('Markdown-3.5.2/markdown/')
        and member.name.endswith('.py') or member.name.startswith('Markdown-3.5.2/Markdown.egg-info/'))


def extract_sources(archive, target):
    target.mkdir()
    names = set()
    with tarfile.open(archive) as bundle:
        for member in bundle:
            if not selected_member(member):
                continue
            require(member.size <= 1024 * 1024, '单源码超预算')
            relative = PurePosixPath(member.name).relative_to('Markdown-3.5.2')
            require(str(relative) not in names, '重复源码成员')
            names.add(str(relative))
            destination = target / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            with destination.open('xb') as stream:
                stream.write(bundle.extractfile(member).read())
    require(len(names) == 39, '锁包33源码及6原egg-info集合改变')


def snapshot(root):
    result = {}
    for path in sorted(root.rglob('*')):
        require(not path.is_symlink(), '输入不能含符号链接')
        if path.is_file():
            result[str(path.relative_to(root))] = {'bytes': path.stat().st_size, 'sha256': sha(path)}
    return result


def build_command(cli, entry, inputs, output):
    args = [str(cli), 'build', str(entry), '--diagnostic-stages', '--out-dir', str(output),
            '-I', str(inputs / 'stdlib'), '-I', str(inputs / 'markdown'), '--instance-module', 'html.parser']
    for name in INCLUDES:
        args.extend(['--include-module', name])
    return args


def target_window(command, commands, golden, out, env, result, clock=time.monotonic):
    """一次CP，再一次compile；仅成功才一次JIT。纯mock可验停止/预算，不执行目标。"""
    deadline = clock() + 180
    for label, budget in [('cp', 30), ('compile', 90), ('jit', 30)]:
        remaining = deadline - clock()
        require(remaining > 0, '目标总窗口180秒用尽')
        record = command(label, commands[label], min(budget, remaining), env)
        result['target_stages'][label] = record
        if record['failure'] or not record['raw_streams_complete'] or record['exit'] != 0:
            result['stop_reason'] = label + ' failed; no retry'
            if label == 'jit':
                result['plain_prefix_matches'] = (out / 'jit.stdout').read_bytes().startswith(golden[:48])
            return False
        if label == 'cp':
            require((out / 'cp.stdout').read_bytes() == golden and (out / 'cp.stderr').stat().st_size == 0,
                    'CP原77B oracle/空stderr不符')
        elif label == 'jit':
            actual = (out / 'jit.stdout').read_bytes()
            result['plain_prefix_matches'] = actual.startswith(golden[:48])
            result['explicit_full_pass'] = actual == golden and (out / 'jit.stderr').stat().st_size == 0
            return result['explicit_full_pass']
    return False


def tool_build(command, clock=time.monotonic):
    deadline = clock() + 1200
    for label, argv in [('runtime-build', ['make', 'runtime']),
                        ('compiler-build', ['cargo', 'build', '--locked', '-p', 'ryupy-cli', '--features', FEATURE])]:
        remaining = deadline - clock()
        require(remaining > 0, '一次工具构建总窗口1200秒用尽')
        record = command(label, argv, remaining)
        require(record['exit'] == 0 and record['failure'] is None, label + '失败，不继续构建')


class Runner:
    def __init__(self):
        self.out = REPO / 'target/markdown-preflight'
        self.out.mkdir(parents=True, exist_ok=False)
        self.labels = set()
        self.result = {'scope': 'NOT-ACCEPTANCE: explicit Markdown preflight', 'accepted': False,
                       'default_project_delta': 0, 'not_run': NOT_RUN, 'target_stages': {},
                       'explicit_full_pass': False, 'plain_prefix_matches': False, 'retries': 0}

    def command(self, name, argv, timeout, env=None):
        require(name not in self.labels, '禁止重复命令：' + name)
        self.labels.add(name)
        return capture(self.out / name, argv, REPO, timeout, env)

    def checked(self, name, argv, timeout):
        record = self.command(name, argv, timeout)
        require(record['exit'] == 0 and record['failure'] is None, name + '失败，见原双流')
        return record

    def identity(self, label):
        self.checked(label + '-head', ['git', 'rev-parse', 'HEAD', 'HEAD^{tree}'], 20)
        head, tree = (self.out / (label + '-head.stdout')).read_text().splitlines()
        self.checked(label + '-status', ['git', 'status', '--porcelain=v1', '--untracked-files=all'], 20)
        require(not (self.out / (label + '-status.stdout')).read_text(), '候选工作树不干净')
        self.checked(label + '-files', ['git', 'ls-tree', '-r', '-z', 'HEAD'], 20)
        records = tree_records((self.out / (label + '-files.stdout')).read_bytes())
        return {'head': head, 'tree': tree, 'files': records}

    def inputs(self, python):
        inputs = self.out / 'inputs'
        inputs.mkdir()
        source = REPO / 'tests/projects'
        for name, expected in [('entries/markdown_sample.py', ENTRY_SHA), ('baselines/markdown/stdout.txt', GOLDEN_SHA)]:
            require(sha(source / name) == expected, '原入口/oracle变更')
            shutil.copyfile(source / name, inputs / Path(name).name)
        rows = [line.split('\t') for line in (source / 'lock.tsv').read_text().splitlines()
                if line.startswith('markdown\t')]
        require(len(rows) == 1, 'Markdown锁记录不唯一')
        name, version, filename, expected, url = rows[0]
        require((name, version, filename, expected) == ('markdown', '3.5.2', 'Markdown-3.5.2.tar.gz', ARCHIVE_SHA)
                and url.startswith('https://files.pythonhosted.org/'), 'Markdown锁身份改变')
        archive = inputs / filename
        cached = REPO / 'third_party/projects/_dist' / filename
        if cached.is_file():
            shutil.copyfile(cached, archive)
        else:
            self.checked('fetch-locked-markdown', ['curl', '--fail', '--location', '--show-error',
                '--max-time', '60', '--max-filesize', '1048576', '--proto', '=https', '--proto-redir', '=https',
                url, '-o', archive], 70)
        require(sha(archive) == ARCHIVE_SHA, '锁归档hash不符')
        extract_sources(archive, inputs / 'markdown')
        stdlib = python.parent.parent / 'lib/python3.12'
        for name, expected in STDLIB.items():
            require(sha(stdlib / name) == expected, 'CP四源hash不符：' + name)
            dest = inputs / 'stdlib' / name
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(stdlib / name, dest)
        for name in ['lock.tsv', 'manifest.tsv']:
            shutil.copyfile(source / name, inputs / name)
        save(self.out / 'source-inputs.json', snapshot(inputs))
        return inputs

    def execute(self):
        before = inputs_before = tools = None
        code = 2
        try:
            check_context(os.environ, json.loads(Path(os.environ['GITHUB_EVENT_PATH']).read_text()))
            before = self.identity('before')
            require(before['head'] == os.environ['GITHUB_SHA'], '真实HEAD与CI不符')
            self.checked('base-files', ['git', 'ls-tree', '-r', '-z', BASE_TREE], 20)
            changed = source_delta(tree_records((self.out / 'base-files.stdout').read_bytes()), before['files'])
            save(self.out / 'source-identity.json', {'ci_head': before['head'], 'ci_tree': before['tree'],
                'production_base_head': BASE_SOURCE_HEAD, 'production_base_tree': BASE_TREE,
                'test_ci_changes': changed, 'production_and_original_contract_unchanged': True})
            python = Path(os.environ['PYTHON']).resolve()
            dotnet = Path(os.environ['DOTNET']).resolve()
            require(sys.version_info[:3] == (3, 12, 13) and sha(python) == PYTHON_SHA,
                    '固定CPython身份不符')
            inputs = self.inputs(python)
            inputs_before = snapshot(inputs)
            # 下载身份不代替PATH实际选择；版本和二进制hash属于工具准备，不占目标180秒。
            selected_tools = {'python': python, 'dotnet': dotnet}
            versions = {'rustc': 'rustc 1.99.1 (58cf8f9c3 2026-10-13)',
                        'cargo': 'cargo 1.99.1 (5f94df478 2026-08-27)', 'dotnet': '10.0.403'}
            for name, version in versions.items():
                path = dotnet if name == 'dotnet' else Path(shutil.which(name) or '').resolve()
                require(path.is_file(), '实际工具缺失：' + name)
                self.checked(name + '-version', [path, '--version'], 30)
                require((self.out / (name + '-version.stdout')).read_text().strip() == version,
                        '实际工具版本不符：' + name)
                selected_tools[name] = path
            selected_hashes = {name: sha(path) for name, path in selected_tools.items()}
            # 两条命令各保真实exit，合用一个1200秒截止时间；前一失败不启动后一。
            tool_build(self.checked)
            require(all(sha(path) == selected_hashes[name] for name, path in selected_tools.items()),
                    '构建期间工具身份改变')
            frozen = self.out / 'tools'
            frozen.mkdir()
            originals = {'ryupy': REPO / 'target/debug/ryupy',
                         'RyuPy.Runtime.dll': REPO / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'}
            for name, path in originals.items():
                shutil.copy2(path, frozen / name)
            tools = {name: {'path': str(path), 'sha256': sha(path), 'bytes': path.stat().st_size,
                           'mode': oct(path.stat().st_mode & 0o777)} for name, path in
                     dict(originals, **selected_tools).items()}
            save(self.out / 'tools.json', {'tools': tools, 'cli_features': [FEATURE],
                'cli_configuration': 'debug', 'runtime_configuration': 'Release', 'selected_versions': versions,
                'ci_head': before['head'], 'base_source_tree': BASE_TREE,
                'note': 'artifact下载可能丢执行位；恢复tools.json记录后才可运行。'})
            cli, runtime = frozen / 'ryupy', frozen / 'RyuPy.Runtime.dll'
            env = {key: os.environ[key] for key in ('PATH', 'HOME', 'RUNNER_TEMP') if key in os.environ}
            env.update(PYTHONPATH=os.pathsep.join(map(str, [inputs / 'stdlib', inputs / 'markdown'])),
                       PYTHONDONTWRITEBYTECODE='1', PYTHONNOUSERSITE='1', PYTHONHASHSEED='0',
                       RYUPY_RUNTIME_DIR=str(frozen), DOTNET_ROOT=str(dotnet.parent),
                       DOTNET_MULTILEVEL_LOOKUP='0', DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1',
                       LC_ALL='C.UTF-8', TZ='UTC')
            entry = REPO / 'tests/projects/entries/markdown_sample.py'
            pe = self.out / 'pe'
            commands = {'cp': [python, '-S', '-B', entry], 'compile': build_command(cli, entry, inputs, pe),
                        'jit': [dotnet, pe / 'markdown_sample.dll']}
            save(self.out / 'target-plan.json', {'commands': {k: list(map(str, v)) for k, v in commands.items()},
                'timeouts_seconds': [30, 90, 30], 'total_seconds': 180, 'includes': INCLUDES,
                'instance_modules': ['html.parser'], 'scope': self.result['scope']})
            golden = (inputs / 'stdout.txt').read_bytes()
            require(len(golden) == 77, '完整oracle长度改变')
            target_tools = dict(selected_tools, ryupy=cli, **{'RyuPy.Runtime.dll': runtime})
            def verify_target_identity():
                require(all(sha(path) == tools[name]['sha256'] for name, path in target_tools.items()),
                        '目标步骤前后工具身份改变')
                require(snapshot(inputs) == inputs_before and sha(entry) == ENTRY_SHA,
                        '目标步骤前后输入改变')
            def target_command(label, argv, timeout, process_env):
                verify_target_identity()
                record = self.command(label, argv, timeout, process_env)
                verify_target_identity()
                if label == 'compile' and record['exit'] == 0 and not record['failure']:
                    require((pe / 'markdown_sample.dll').is_file(), 'compile0但缺少PE')
                    require(sha(pe / 'RyuPy.Runtime.dll') == tools['RyuPy.Runtime.dll']['sha256'],
                            'JIT前复制Runtime身份不符')
                return record
            success = target_window(target_command, commands, golden, self.out, env, self.result)
            if pe.exists() and (pe / 'RyuPy.Runtime.dll').is_file():
                require(sha(pe / 'RyuPy.Runtime.dll') == sha(runtime), 'PE runtime身份改变')
            code = 0 if success else 1
        except Exception as error:
            self.result.update(error=str(error), traceback=traceback.format_exc())
        finally:
            try:
                if before is not None:
                    require(self.identity('after') == before, '候选源码改变')
                if inputs_before is not None:
                    require(snapshot(self.out / 'inputs') == inputs_before, '精确输入改变')
                if tools is not None:
                    for name in ['ryupy', 'RyuPy.Runtime.dll']:
                        require(sha(self.out / 'tools' / name) == tools[name]['sha256'], '冻结工具改变')
                self.result['identity_preserved'] = True
            except Exception as error:
                self.result.update(identity_preserved=False, identity_error=str(error))
                code = 2
            self.result['runner_exit'] = code
            save(self.out / 'result.json', self.result)
        return code


class PureTests(unittest.TestCase):
    """不使用任何CP目标、编译器、网络或真实构建的轻量控制流自测。"""
    def test_context_exact_and_source_delta(self):
        head = '1' * 40
        env = dict(GITHUB_ACTIONS='true', GITHUB_EVENT_NAME='push', GITHUB_REF=REF,
                   GITHUB_RUN_ATTEMPT='1', GITHUB_SHA=head)
        event = {'ref': REF, 'after': head, 'head_commit': {'id': head, 'message': TITLE}}
        check_context(env, event)
        for name, value in [('GITHUB_RUN_ATTEMPT', '2'), ('GITHUB_REF', REF.upper()), ('RUSTFLAGS', '-Awarnings')]:
            with self.assertRaises(ValueError):
                check_context(dict(env, **{name: value}), event)
        with self.assertRaises(ValueError):
            check_context(env, dict(event, head_commit={'id': head, 'message': TITLE.upper()}))
        self.assertEqual(source_delta({'runtime/x': 1}, {'runtime/x': 1}), [])
        with self.assertRaises(ValueError):
            source_delta({'runtime/x': 1}, {'runtime/x': 2})

    def test_archive_and_exact_command(self):
        for name in ['/absolute', '../escape']:
            with self.assertRaises(ValueError):
                selected_member(tarfile.TarInfo(name))
        linked = tarfile.TarInfo('Markdown-3.5.2/markdown/link.py')
        linked.type = tarfile.SYMTYPE
        with self.assertRaises(ValueError):
            selected_member(linked)
        args = build_command('ryupy', 'markdown_sample.py', Path('input'), Path('pe'))
        self.assertEqual(args.count('--instance-module'), 1)
        self.assertEqual(args[args.index('--instance-module') + 1], 'html.parser')
        self.assertEqual([args[i + 1] for i, x in enumerate(args) if x == '--include-module'], INCLUDES)
        self.assertNotIn('--aot', args)
        self.assertNotIn('--trace-product-v1', args)

    def test_tool_build_records_separate_exits_and_shared_budget(self):
        for failure in ['runtime-build', 'compiler-build', None]:
            calls = []
            def fake(name, argv, timeout):
                calls.append(name)
                self.assertLessEqual(timeout, 1200)
                return {'exit': 1 if name == failure else 0, 'failure': None}
            if failure:
                with self.assertRaises(ValueError):
                    tool_build(fake)
            else:
                tool_build(fake)
            self.assertEqual(calls, ['runtime-build'] if failure == 'runtime-build'
                             else ['runtime-build', 'compiler-build'])

    def test_workflow_job_filters_keep_original_chains(self):
        text = (REPO / '.github/workflows/wcwidth-legacy-aot.yml').read_text()
        jobs = {match[1]: match[2] for match in re.finditer(r'^  ([a-z][a-z0-9-]*):\n(.*?)(?=^  [a-z][a-z0-9-]*:|\Z)', text.split('\njobs:\n', 1)[1], re.M | re.S)}
        ordinary = {'baseline', 'full-check', 'projects', 'trace-adapter-s2',
                    'trace-rust-preflight', 'pyfiglet-explicit-deployment', 'source-import-paths'}
        self.assertEqual(set(jobs), ordinary | {'select-mode', 'trace-edge-diagnostic', 'markdown-preflight'})
        for diagnostic, markdown, event, attempt, expected in [
                ('false', 'false', 'push', '1', ordinary), ('false', 'false', 'workflow_dispatch', '1', ordinary),
                ('true', 'false', 'push', '1', {'trace-edge-diagnostic'}),
                ('false', 'true', 'push', '1', {'markdown-preflight'}),
                ('false', 'true', 'push', '2', set())]:
            active = set()
            for name, block in jobs.items():
                if name == 'select-mode':
                    continue
                condition = re.search(r'^    if: (.*)$', block, re.M)[1]
                for key, value in [('github.ref', REF), ('github.event_name', event), ('github.run_attempt', attempt),
                        ('needs.select-mode.outputs.diagnostic', diagnostic),
                        ('needs.select-mode.outputs.markdown', markdown)]:
                    condition = condition.replace(key, repr(value))
                condition = condition.replace('&&', 'and')
                # 只求值本仓库固定的纯比较表达式，不读取GitHub事件文本/第三方内容。
                if eval(condition, {'__builtins__': {}}, {}):
                    active.add(name)
            self.assertEqual(active, expected)
        self.assertIn('fetch-depth: 2', jobs['markdown-preflight'])
        self.assertIn('if: always()', jobs['markdown-preflight'])

    def test_single_window_stops_and_preserves_both_calls(self):
        golden = b'x' * 48 + b'y' * 29
        for failure, failure_code, calls_expected in [('cp', 1, ['cp']), ('compile', 1, ['cp', 'compile']),
                ('jit', 1, ['cp', 'compile', 'jit']), (None, 0, ['cp', 'compile', 'jit'])]:
            with self.subTest(failure=failure), tempfile.TemporaryDirectory() as temp:
                root = Path(temp)
                result = {'target_stages': {}, 'explicit_full_pass': False, 'plain_prefix_matches': False}
                calls = []
                def fake(name, argv, timeout, env):
                    calls.append(name)
                    self.assertLessEqual(timeout, {'cp': 30, 'compile': 90, 'jit': 30}[name])
                    (root / (name + '.stdout')).write_bytes(golden[:48] if name == failure else golden)
                    (root / (name + '.stderr')).write_bytes(b'failure' if name == failure else b'')
                    return {'exit': failure_code if name == failure else 0, 'failure': None, 'raw_streams_complete': True}
                ok = target_window(fake, {x: [x] for x in ['cp', 'compile', 'jit']}, golden, root, {}, result)
                self.assertEqual(calls, calls_expected)
                self.assertEqual(ok, failure is None)
                self.assertFalse(result['explicit_full_pass'] if failure else False)
        with tempfile.TemporaryDirectory() as temp:
            with self.assertRaises(ValueError):
                target_window(lambda *a: self.fail('exhausted window ran'), {}, golden, Path(temp), {},
                              {'target_stages': {}}, clock=iter([0, 181]).__next__)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--self-test', action='store_true')
    if parser.parse_args().self_test:
        unittest.main(argv=[sys.argv[0]], verbosity=2)
    else:
        raise SystemExit(Runner().execute())
