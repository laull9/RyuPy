#!/usr/bin/env python3
"""单次hosted显式闭包事实窗口；完整parser及原Markdown，不默认化，不替代验收。"""
import argparse
import configparser
import io
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import sys
import tarfile
import tempfile
import time
import traceback
import unittest
from unittest.mock import patch
import markdown_preflight as old

REPO = old.REPO
TITLE = 'diagnostic-not-acceptance: markdown-closure-001'
REPOSITORY = 'laull9/RyuPy'
BASE_TREE = '28c9f2fe18f27d050a7f7281357449c0f9f831d2'
BUNDLE = REPO / 'tests/validation/markdown_closure_inputs.tar.gz'
BUNDLE_SHA = 'fcf1782cee6fa881a9f2e26b55b901dd89393857ddfc97735d77ad3515644eea'
A_PATH = 'tests/cases/importlib_util_find/pending_parser_complete/entry.py'
A_SHA = '994a29e98caedfa952e228617464b61ded841931e76e7fa7f14e53ce0b9ed302'
A_ORACLE = 'c0b5a052844c65afbab79e431cb90e9aedee6ac9bffdeebf1770a5639c81bfb2'
ALLOWED = {'.github/retired-workflows/wcwidth-legacy-aot.yml', '.github/retired-workflows/markdown-closure.yml',
           'tests/validation/markdown_closure_probe.py',
           'tests/validation/markdown_closure_inputs.tar.gz', 'tests/validation/markdown_preflight.py',
           'scripts/textwrap_dedent_selftest.py', 'scripts/decimal_percent_selftest.py',
           'scripts/keyerror_construction_selftest.py'}
SUFFIX = " && needs.select-mode.outputs.closure != 'true'"
require, sha, save, snapshot = old.require, old.sha, old.save, old.snapshot


class IdentityError(ValueError):
    """共享来源或证据身份失败必须中止两个业务窗口。"""


def check_context(env, event):
    adjusted = dict(event, head_commit=dict(event.get('head_commit') or {}, message=old.TITLE))
    old.check_context(env, adjusted)
    require(event.get('head_commit', {}).get('message') == TITLE, '新窗口标题必须字节相等')
    require(env.get('RUNNER_ENVIRONMENT') == 'github-hosted' and env.get('RUNNER_OS') == 'Linux'
            and env.get('RUNNER_ARCH') == 'X64', '仅官方hosted Linux x64')
    require(env.get('GITHUB_REPOSITORY') == REPOSITORY
            and event.get('repository', {}).get('full_name') == REPOSITORY, '仓库身份不符')


def unpack(bundle, target):
    """仅物化已冻结的小原件；不下载、不导入源，也不允许归档链接或额外成员。"""
    require(sha(bundle) == BUNDLE_SHA, '冻结输入包hash不符')
    require(shutil.disk_usage(target.parent).free >= 64 * 1024**2, '保留64MiB空间')
    target.mkdir(exist_ok=False)
    total, seen = 0, set()
    with tarfile.open(bundle) as archive:
        for member in archive:
            path = PurePosixPath(member.name)
            require(member.isfile() and not member.issym() and not member.islnk()
                    and not path.is_absolute() and '..' not in path.parts
                    and str(path) == member.name and '\\' not in member.name
                    and member.name not in seen, '拒绝不安全或重复归档成员')
            total += member.size
            require(0 <= member.size <= 256 * 1024 and total <= 1024**2, '输入包展开超预算')
            seen.add(member.name)
            dest = target / path
            dest.parent.mkdir(parents=True, exist_ok=True)
            with dest.open('xb') as stream:
                stream.write(archive.extractfile(member).read())
    manifest = json.loads((target / 'manifest.json').read_text())
    actual = snapshot(target)
    require(set(actual) == set(manifest['files']) | {'manifest.json'}, '输入包成员集合不符')
    for name, record in manifest['files'].items():
        require(actual[name] == {k: record[k] for k in ('bytes', 'sha256')}, '输入成员改变：' + name)
    source = json.loads((target / 'reference/source-inputs.json').read_text())
    required = {n: value for n, value in source.items() if n.startswith(('stdlib/', 'markdown/'))}
    require(len(required) == 43, '原四stdlib及39份Markdown源/metadata集合改变')
    for name, record in required.items():
        require(actual['inputs/' + name] == record, '与官方来源不符：' + name)
    return manifest


def targets(inputs):
    """只解析原声明组，不执行entry point；整份metadata不删改。"""
    path = inputs / 'markdown/Markdown.egg-info/entry_points.txt'
    require(sha(path) == '94c1328a203f659c9f3c20650ef88197e4a253471fa1eb842a9c31c37eb5b0a4',
            '原entry_points字节改变')
    config = configparser.ConfigParser(interpolation=None)
    config.read_string(path.read_text())
    require(set(config.sections()) == {'console_scripts', 'markdown.extensions'}, 'metadata组改变')
    result = [value.split(':', 1)[0] for _, value in config.items('markdown.extensions')]
    require(len(result) == len(set(result)) == 18 and all(re.fullmatch(r'markdown\.extensions\.[a-z_0-9]+', n)
                                                       for n in result), '真实组target不完整')
    return result


def command_plan(cli, python, dotnet, inputs, products):
    plans = {}
    for window, entry in [('a', 'parser_complete.py'), ('b', 'markdown_sample.py')]:
        dest = products / window
        args = [cli, 'build', inputs / entry, '--diagnostic-stages', '--out-dir', dest,
                '-I', inputs / 'stdlib']
        if window == 'b':
            args += ['-I', inputs / 'markdown']
        args += ['--instance-module', 'html.parser']
        if window == 'b':
            for name in ['html.parser'] + targets(inputs): args += ['--include-module', name]
            args += ['--distribution-metadata', inputs / 'metadata-request.json']
        plans[window] = {'compile': args, 'jit': [dotnet, dest / (Path(entry).stem + '.dll')]}
    plans['a']['cp'] = [python, '-S', '-B', inputs / 'parser_complete.py']
    return {w: {stage: list(map(str, argv)) for stage, argv in plan.items()} for w, plan in plans.items()}


def verify_record(record, prefix, argv, cwd, timeout, env):
    """独立绑定启动前argv/env及完整原流；不能把缺流、篡改或异argv记成有效业务结果。"""
    planned = json.loads(prefix.with_suffix('.argv.json').read_text())
    for key, expected in [('argv', list(map(str, argv))), ('cwd', str(cwd)), ('timeout_seconds', timeout)]:
        require(record.get(key) == planned.get(key) == expected, '命令身份改变：' + key)
    require(json.loads(Path(str(prefix) + '-environment.json').read_text()) == env, '环境记录改变')
    require(json.loads(prefix.with_suffix('.json').read_text()) == record, '结果记录改变')
    for stream in ('stdout', 'stderr'):
        path = prefix.with_suffix('.' + stream)
        require(path.is_file() and record.get('streams', {}).get(stream) ==
                {'bytes': path.stat().st_size, 'sha256': sha(path)}, '原流hash不符：' + stream)


def successful(record):
    return record.get('exit') == 0 and record.get('failure') is None and record.get('raw_streams_complete') is True


def run_windows(plans, command, checkpoint, root, envs, result, clock=time.monotonic):
    """A业务失败仍可取B事实；共享identity异常不捕获。每步提前保存not-run及已完成状态。"""
    for window in ('a', 'b'):
        state = result['windows'][window]
        deadline = clock() + (240 if window == 'a' else 210)
        try:
            golden = ((root / 'inputs/parser_expected.stdout') if window == 'a' else
                      (root / 'reference/markdown-full-cp-run.stdout')).read_bytes()
            require(bool(golden), 'oracle不得为空')
            for stage in (('cp', 'compile', 'jit') if window == 'a' else ('compile', 'jit')):
                remaining = deadline - clock()
                require(remaining > 0, '窗口总预算用尽')
                state['active_stage'] = stage; save(root.parent / 'result.json', result)
                limit = 32 * 1024**2 if stage == 'compile' else 4 * 1024**2
                record = command(window + '-' + stage, plans[window][stage],
                                 min(180 if stage == 'compile' else 30, remaining), envs[stage], limit)
                state['stages'][stage] = record; save(root.parent / 'result.json', result)
                require(successful(record), stage + '失败；不重试，后继not-run')
                if stage == 'compile':
                    checkpoint(window)
                else:
                    prefix = root.parent / (window + '-' + stage)
                    require(prefix.with_suffix('.stdout').read_bytes() == golden
                            and prefix.with_suffix('.stderr').stat().st_size == 0, stage + '完整oracle/空stderr不符')
                    if stage == 'cp': golden = prefix.with_suffix('.stdout').read_bytes()
            state['passed'] = True
        except IdentityError:
            raise
        except Exception as error:
            state['error'] = str(error)
        finally:
            state['active_stage'] = None
            save(root.parent / 'result.json', result)
    return all(v['passed'] for v in result['windows'].values())


class Runner:
    def __init__(self):
        self.out = REPO / 'target/markdown-closure'
        self.out.mkdir(parents=True, exist_ok=False)
        self.root = self.out / 'frozen'
        self.before = self.input_identity = None
        self.frozen, self.labels, self.sequence = {}, set(), 0
        self.result = {'scope': 'NOT-ACCEPTANCE: explicit Markdown closure', 'accepted': False,
                       'default_project_delta': 0, 'retries': 0, 'not_run': old.NOT_RUN + ['arbitrary bad target laziness'],
                       'windows': {w: {'passed': False, 'stages': {s: 'not-run' for s in stages}}
                                   for w, stages in [('a', ['cp', 'compile', 'jit']), ('b', ['compile', 'jit'])]}}
        save(self.out / 'result.json', self.result)

    def command(self, name, argv, timeout, env=None, limit=32 * 1024**2, cwd=None):
        require(name not in self.labels, '禁止重复命令：' + name)
        self.labels.add(name)
        prefix = self.out / name
        cwd = cwd or REPO
        # 工具构建不记录继承环境；目标环境只来自显式白名单，绝不保存凭据。
        save(Path(str(prefix) + '-environment.json'), env)
        record = old.capture(prefix, argv, cwd, timeout, env, limit,
                             512 * 1024**2 if name == 'a-cp' else None)
        try:
            verify_record(record, prefix, argv, cwd, timeout, env)
        except Exception as error:
            raise IdentityError(str(error)) from error
        for suffix in ('.argv.json', '.json', '.stdout', '.stderr', '-environment.json'):
            path = Path(str(prefix) + suffix); self.frozen[str(path)] = sha(path)
        return record

    def checked(self, name, argv, timeout):
        record = self.command(name, argv, timeout)
        require(successful(record), name + '失败，原流已保存')
        return record

    def identity(self):
        self.sequence += 1
        prefix = 'identity-' + str(self.sequence)
        values = {}
        for label, args in [('head', ['rev-parse', 'HEAD', 'HEAD^{tree}']),
                            ('status', ['status', '--porcelain=v1', '--untracked-files=all']),
                            ('files', ['ls-tree', '-r', '-z', 'HEAD'])]:
            self.checked(prefix + '-' + label, ['git'] + args, 20)
            values[label] = (self.out / (prefix + '-' + label + '.stdout')).read_bytes()
        require(not values['status'], '源码工作树不干净')
        head, tree = values['head'].decode().splitlines()
        return {'head': head, 'tree': tree, 'files': old.tree_records(values['files'])}

    def barrier(self):
        try:
            require(self.identity() == self.before, '源码身份改变')
            if self.input_identity is not None: require(snapshot(self.root) == self.input_identity, '输入身份改变')
            for path, digest in self.frozen.items(): require(sha(path) == digest, '工具/PE/原流身份改变：' + path)
        except Exception as error:
            raise IdentityError(str(error)) from error

    def prepare_inputs(self):
        manifest = unpack(BUNDLE, self.root)
        inputs = self.root / 'inputs'
        for original, dest, expected in [(REPO / A_PATH, 'parser_complete.py', A_SHA),
                (REPO / A_PATH.replace('entry.py', 'stdout.txt'), 'parser_expected.stdout', A_ORACLE),
                (REPO / 'tests/projects/entries/markdown_sample.py', 'markdown_sample.py', old.ENTRY_SHA)]:
            require(sha(original) == expected, '原完整入口/oracle变更')
            shutil.copyfile(original, inputs / dest)
        oracle = self.root / 'reference/markdown-full-cp-run'
        record = json.loads(oracle.with_suffix('.json').read_text())
        require(successful(record) and sha(oracle.with_suffix('.stdout')) == old.GOLDEN_SHA
                and oracle.with_suffix('.stdout').stat().st_size == 77
                and oracle.with_suffix('.stderr').stat().st_size == 0, '原真实B CP证据不符')
        for stream in ('stdout', 'stderr'):
            path = oracle.with_suffix('.' + stream)
            require(record['streams'][stream] == {'bytes': path.stat().st_size, 'sha256': sha(path)}, 'B CP原流损坏')
        require(manifest['execution_identity_excerpt']['python_sha256'] == old.PYTHON_SHA, '原CP身份不符')
        self.input_identity = snapshot(self.root)
        save(self.out / 'source-inputs.json', self.input_identity)
        save(self.out / 'input-provenance.json', manifest)
        return inputs

    def prepare_tools(self):
        self.python, self.dotnet = Path(os.environ['PYTHON']).resolve(), Path(os.environ['DOTNET']).resolve()
        require(sys.version_info[:3] == (3, 12, 13) and Path(sys.executable).resolve() == self.python
                and sha(self.python) == old.PYTHON_SHA, '实际固定CPython身份不符')
        tools = {'python': self.python, 'dotnet': self.dotnet}
        versions = {'rustc': 'rustc 1.99.1 (58cf8f9c3 2026-10-13)',
                    'cargo': 'cargo 1.99.1 (5f94df478 2026-08-27)', 'dotnet': '10.0.403'}
        for name, version in versions.items():
            path = self.dotnet if name == 'dotnet' else Path(shutil.which(name) or '').resolve()
            require(path.is_file(), '缺实际工具：' + name)
            tools[name] = path
            self.checked(name + '-version', [path, '--version'], 30)
            require((self.out / (name + '-version.stdout')).read_text().strip() == version, '实际工具版本改变')
        self.checked('dotnet-runtimes', [self.dotnet, '--list-runtimes'], 30)
        require('Microsoft.NETCore.App 10.0.14 [' in (self.out / 'dotnet-runtimes.stdout').read_text(), '固定.NET运行时缺失')
        self.frozen.update({str(path): sha(path) for path in tools.values()})
        self.barrier()
        old.tool_build(self.checked)
        self.barrier()
        self.cli = REPO / 'target/debug/ryupy'
        runtime = REPO / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'
        frozen = self.out / 'runtime'; frozen.mkdir()
        self.runtime = frozen / runtime.name
        shutil.copy2(runtime, self.runtime)
        tools.update(ryupy=self.cli, **{'RyuPy.Runtime.dll': self.runtime})
        self.frozen.update({str(path): sha(path) for path in tools.values()})
        save(self.out / 'tools.json', {'tools': {name: {'path': str(path), 'bytes': path.stat().st_size,
             'sha256': sha(path), 'mode': oct(path.stat().st_mode & 0o777)} for name, path in tools.items()},
             'features': [old.FEATURE], 'cli_configuration': 'debug', 'runtime_configuration': 'Release',
             'source_head': self.before['head'], 'source_tree': self.before['tree'], 'versions': versions,
             'cli_bytes_retained': False, 'recovery': '同源重新构建；不保证CLI同字节恢复'})

    def checkpoint(self, window):
        """产物原目录已受always上传覆盖；先留清单，再核计划，失败不丢PE。"""
        output = self.out / 'products' / window
        files = snapshot(output)
        save(self.out / (window + '-product-files.json'), files)
        stem = 'parser_complete' if window == 'a' else 'markdown_sample'
        require((output / (stem + '.dll')).read_bytes()[:2] == b'MZ', '缺有效PE')
        if sha(output / 'RyuPy.Runtime.dll') != sha(self.runtime):
            raise IdentityError('产品Runtime身份不符')
        plan = json.loads((output / 'module-environment.json').read_text())
        rows = plan['modules']; names = [r['module'] for r in rows]
        require(plan['compiled_feature'] and plan['session_policy'] == 'ordinary-dynamic-module-pilot'
                and len(names) == len(set(names)) and all(r['compiled'] for r in rows), '实际module计划不完整')
        parser = next(r for r in rows if r['module'] == 'html.parser')
        require(parser['execution'] == 'InstanceGlobals' and parser['ordinary_name_handlers']
                and parser['relative_from_import'], 'parser未保显式Instance合同')
        if window == 'b':
            require(set(targets(self.root / 'inputs')) <= set(names), '实际图遗漏metadata target')
            report = json.loads((output / 'distribution-metadata.json').read_text())
            require(report['mode'] == 'closed_snapshot' and report['roots'] == ['markdown']
                    and len(report['distributions']) == 1, '实际metadata环境改变')
            distribution = report['distributions'][0]
            metadata = snapshot(self.root / 'inputs/markdown/Markdown.egg-info')
            require(distribution['root'] == 'markdown' and distribution['path'] == 'Markdown.egg-info'
                    and not distribution['shadowed'] and distribution['directories'] == []
                    and {r['path']: {'bytes': r['size'], 'sha256': r['sha256']} for r in distribution['files']} == metadata,
                    '实际完整metadata不符')
        self.result['windows'][window]['module_plan'] = plan  # 不对36或旧27做硬等式。
        self.frozen.update({str(output / n): r['sha256'] for n, r in files.items()})

    def target(self, name, argv, timeout, env, limit):
        start = time.monotonic()
        self.barrier()
        timeout -= time.monotonic() - start
        require(timeout > 0, '身份屏障耗尽当前窗口预算')
        cwd = self.out / 'cwd' / name; cwd.mkdir(parents=True)
        record = self.command(name, argv, timeout, env, limit, cwd)
        # 完成即保存失败产品目录，不等待业务成功、plan有效或末段finally。
        output = self.out / 'products' / name[0]
        if output.exists(): save(self.out / (name + '-products.json'), snapshot(output))
        for suffix in ('.argv.json', '.json', '.stdout', '.stderr'):
            path = self.out / (name + suffix); self.frozen[str(path)] = sha(path)
        self.barrier()
        return record

    def execute(self):
        code = 2
        try:
            check_context(os.environ, json.loads(Path(os.environ['GITHUB_EVENT_PATH']).read_text()))
            self.before = self.identity()
            require(self.before['head'] == os.environ['GITHUB_SHA'], '实际HEAD与event不符')
            self.checked('base-files', ['git', 'ls-tree', '-r', '-z', BASE_TREE], 20)
            baseline = old.tree_records((self.out / 'base-files.stdout').read_bytes())
            changed = {n for n in baseline.keys() | self.before['files'].keys() if baseline.get(n) != self.before['files'].get(n)}
            require(changed <= ALLOWED, '出现非授权文件变化：' + str(sorted(changed - ALLOWED)))
            save(self.out / 'source-identity.json', {'candidate': self.before, 'production_tree': BASE_TREE,
                                                  'allowed_test_ci_changes': sorted(changed)})
            inputs = self.prepare_inputs()
            save(self.out / 'planned-scope.json', {'scope': self.result['scope'], 'business_process_caps':
                 {'cp': 1, 'compile': 2, 'jit': 2}, 'windows_seconds': {'a': 240, 'b': 210},
                 'tool_builds': ['make runtime', 'cargo build --locked -p ryupy-cli --features ' + old.FEATURE]})
            self.prepare_tools()
            plans = command_plan(self.cli, self.python, self.dotnet, inputs, self.out / 'products')
            common = dict(PATH='/usr/bin:/bin', LANG='C.UTF-8', LC_ALL='C.UTF-8', TZ='UTC',
                          PYTHONDONTWRITEBYTECODE='1', PYTHONNOUSERSITE='1', PYTHONHASHSEED='0')
            product = dict(common, RYUPY_RUNTIME_DIR=str(self.runtime.parent), DOTNET_ROOT=str(self.dotnet.parent),
                           DOTNET_MULTILEVEL_LOOKUP='0', DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1')
            envs = {'cp': dict(common, PYTHONPATH=str(inputs / 'stdlib')), 'compile': product, 'jit': product}
            save(self.out / 'target-plan.json', {'commands': plans, 'budgets': {'a': 240, 'b': 210},
                'stage_seconds': {'cp': 30, 'compile': 180, 'jit': 30}, 'includes_b': ['html.parser'] + targets(inputs),
                'not_acceptance': True, 'business_process_caps': {'cp': 1, 'compile': 2, 'jit': 2}})
            code = 0 if run_windows(plans, self.target, self.checkpoint, self.root, envs, self.result) else 1
        except Exception as error:
            self.result.update(error=str(error), traceback=traceback.format_exc())
        finally:
            try:
                if self.before is not None: self.barrier()
                self.result['identity_preserved'] = self.before is not None
            except Exception as error:
                self.result.update(identity_preserved=False, identity_error=str(error)); code = 2
            self.result['runner_exit'] = code
            save(self.out / 'result.json', self.result)
        return code


class PureTests(unittest.TestCase):
    """只读源包和mock采集；不启动CP业务、CLI、Runtime、下载或构建。"""
    def test_original_inputs_group_and_commands(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp) / 'frozen'; unpack(BUNDLE, root)
            inputs = root / 'inputs'; group = targets(inputs)
            self.assertEqual(len(group), 18)
            plans = command_plan('ryupy', 'python', 'dotnet', inputs, Path('products'))
            self.assertEqual(set(plans['a']), {'cp', 'compile', 'jit'})
            self.assertEqual(set(plans['b']), {'compile', 'jit'})
            self.assertEqual(plans['b']['compile'].count('--include-module'), 19)
            original = json.loads((root / 'reference/markdown-full-compile-command.json').read_text())['argv']
            def without_includes(argv):
                return [arg for i, arg in enumerate(argv) if arg != '--include-module'
                        and (i == 0 or argv[i - 1] != '--include-module')]
            old_base = '/home/runner/work/RyuPy/RyuPy/target/treebuilder-evidence'
            rewritten = [arg.replace(old_base + '/tools/ryupy', 'ryupy').replace(
                old_base + '/inputs', str(inputs)).replace(old_base + '/products/markdown-full', 'products/b')
                for arg in original]
            self.assertEqual(without_includes(plans['b']['compile']), without_includes(rewritten))
            self.assertNotIn('--include-module', plans['a']['compile'])
            self.assertNotIn('--aot', str(plans)); self.assertNotIn('--trace-product-v1', str(plans))
            self.assertEqual(sha(REPO / A_PATH), A_SHA)
            self.assertEqual(sha(REPO / 'tests/projects/entries/markdown_sample.py'), old.ENTRY_SHA)
            self.assertEqual((root / 'reference/markdown-full-cp-run.stdout').stat().st_size, 77)
            (inputs / 'markdown/Markdown.egg-info/entry_points.txt').write_text('')
            with self.assertRaises(ValueError): targets(inputs)

    def test_context_and_real_selector_isolation(self):
        env = dict(GITHUB_ACTIONS='true', GITHUB_EVENT_NAME='push', GITHUB_REF=old.REF,
                   GITHUB_RUN_ATTEMPT='1', GITHUB_SHA='a'*40, GITHUB_REPOSITORY=REPOSITORY,
                   RUNNER_ENVIRONMENT='github-hosted', RUNNER_OS='Linux', RUNNER_ARCH='X64')
        event = dict(ref=old.REF, after='a'*40, head_commit=dict(id='a'*40, message=TITLE), repository=dict(full_name=REPOSITORY))
        check_context(env, event)
        for key, value in [('GITHUB_REPOSITORY', 'other/repo'), ('RUNNER_ENVIRONMENT', 'self-hosted'),
                           ('GITHUB_RUN_ATTEMPT', '2'), ('RUSTFLAGS', '-Awarnings')]:
            with self.assertRaises(ValueError): check_context(dict(env, **{key: value}), event)
        with self.assertRaises(ValueError): check_context(env, dict(event, repository=dict(full_name='other/repo')))
        sys.path.insert(0, str(REPO / 'scripts'))
        import textwrap_dedent_selftest as selector
        self.assertEqual(selector.select(TITLE)[2], {'markdown-closure'})
        self.assertEqual(selector.select(TITLE, attempt='2')[2], set())
        _, blocks = selector.jobs()
        caller = blocks['markdown-closure']
        self.assertEqual(len(caller.splitlines()), 3)
        self.assertIn('    needs: select-mode\n', caller)
        self.assertIn('    uses: ./.github/workflows/markdown-closure.yml\n', caller)
        child = (REPO / '.github/retired-workflows/markdown-closure.yml').read_text()
        self.assertIn('on:\n  workflow_call:\n', child)
        self.assertIn('permissions:\n  contents: read\n', child)
        self.assertEqual(child.count('  markdown-closure:\n'), 1)
        self.assertNotIn('needs.select-mode', child)
        self.assertIn('if: always()', child)
        for path in (REPO / '.github/retired-workflows/wcwidth-legacy-aot.yml',
                     REPO / '.github/retired-workflows/markdown-closure.yml'):
            self.assertLessEqual(len(path.read_text().splitlines()), 600)
        for title, mode, ref in [(TITLE.upper(), 'push', old.REF), (TITLE+'\n', 'push', old.REF),
                                 (TITLE, 'workflow_dispatch', old.REF), (TITLE, 'push', old.REF.upper())]:
            self.assertNotIn('markdown-closure', selector.select(title, mode, ref)[2])

    def test_window_caps_failure_independence_and_identity_stop(self):
        for failure in (None, 'a-cp', 'a-compile', 'a-jit', 'b-compile', 'b-jit', 'identity',
                        'wrong-output', 'nonempty-stderr', 'incomplete-stream', 'timeout', 'bad-plan'):
            with self.subTest(failure=failure), tempfile.TemporaryDirectory() as temp:
                out = Path(temp); root = out / 'frozen'
                (root / 'inputs').mkdir(parents=True); (root / 'reference').mkdir()
                for name in ('inputs/parser_expected.stdout', 'reference/markdown-full-cp-run.stdout'):
                    (root / name).write_bytes(b'oracle\n')
                result = {'windows': {w: {'passed': False, 'stages': {}} for w in ('a', 'b')}}
                plans = {w: {s: [w, s] for s in (('cp','compile','jit') if w=='a' else ('compile','jit'))} for w in ('a','b')}
                calls = []
                def fake(name, argv, timeout, env, limit):
                    calls.append(name)
                    self.assertLessEqual(timeout, 180 if name.endswith('compile') else 30)
                    (out / (name + '.stdout')).write_bytes(b'bad' if failure == 'wrong-output' else b'oracle\n')
                    (out / (name + '.stderr')).write_bytes(b'bad' if failure == 'nonempty-stderr' else b'')
                    if failure == 'identity': raise IdentityError('changed')
                    return dict(exit=1 if name == failure else 0, failure='timeout' if failure == 'timeout' else None,
                                raw_streams_complete=failure != 'incomplete-stream')
                def checkpoint(window):
                    if failure == 'bad-plan': raise ValueError('bad actual plan')
                if failure == 'identity':
                    with self.assertRaises(IdentityError): run_windows(plans, fake, checkpoint, root, dict(cp={},compile={},jit={}), result)
                    self.assertEqual(calls, ['a-cp'])
                else:
                    ok = run_windows(plans, fake, checkpoint, root, dict(cp={},compile={},jit={}), result)
                    self.assertEqual(ok, failure is None)
                    self.assertIn('b-compile', calls)
                self.assertLessEqual(calls.count('a-cp'), 1)
                self.assertNotIn('b-cp', calls)
                self.assertLessEqual(sum(c.endswith('compile') for c in calls), 2)
                self.assertLessEqual(sum(c.endswith('jit') for c in calls), 2)
                self.assertEqual(len(calls), len(set(calls)))

    def test_archive_rejects_escape_links_duplicates_and_budget(self):
        for mode in ('absolute', 'escape', 'link', 'duplicate', 'oversize', 'extra'):
            with self.subTest(mode=mode), tempfile.TemporaryDirectory() as temp:
                root=Path(temp); bundle=root/'bad.tar.gz'
                with tarfile.open(bundle,'w:gz') as archive:
                    names={'absolute':['/bad'], 'escape':['../bad'], 'duplicate':['same','same']}.get(mode,['extra'])
                    for name in names:
                        member=tarfile.TarInfo(name)
                        if mode=='link': member.type=tarfile.SYMTYPE; member.linkname='../bad'
                        data=b'x'*(256*1024+1) if mode=='oversize' else b'x'
                        member.size=len(data); archive.addfile(member,io.BytesIO(data))
                with patch(__name__+'.BUNDLE_SHA',sha(bundle)), self.assertRaises((ValueError,FileNotFoundError)):
                    unpack(bundle,root/'output')
        with tempfile.TemporaryDirectory() as temp, patch.object(shutil,'disk_usage') as disk:
            disk.return_value.free=63*1024**2
            with self.assertRaises(ValueError): unpack(BUNDLE,Path(temp)/'output')

    def test_old_job_hash_guard_accepts_only_single_if_suffix(self):
        sys.path.insert(0,str(REPO/'scripts'))
        import textwrap_dedent_selftest as selector
        _,blocks=selector.jobs()
        original=blocks['baseline']
        self.assertEqual(selector.protected_job_digest('baseline',original),selector.DATA['unchanged_job_blocks']['baseline'])
        for changed in (original.replace(SUFFIX,''), original.replace(SUFFIX,SUFFIX+SUFFIX),
                        original.replace(SUFFIX,'')+SUFFIX, original.replace('    if: ','    if: false && ',1)):
            try: digest=selector.protected_job_digest('baseline',changed)
            except ValueError: continue
            self.assertNotEqual(digest,selector.DATA['unchanged_job_blocks']['baseline'])

    def test_failed_compile_keeps_product_and_raw_before_rejecting_plan(self):
        with tempfile.TemporaryDirectory() as temp:
            runner=object.__new__(Runner); runner.out=Path(temp)
            runner.frozen={}; runner.barrier=lambda: None
            output=runner.out/'products/a';output.mkdir(parents=True)
            (output/'partial.dll').write_bytes(b'failed-PE')
            def command(name,argv,timeout,env,limit,cwd):
                for suffix in ('.argv.json','.json','.stdout','.stderr'):
                    (runner.out/(name+suffix)).write_bytes(b'raw')
                return dict(exit=1,failure=None,raw_streams_complete=True)
            runner.command=command
            result=runner.target('a-compile',['fake'],30,{},1024)
            self.assertEqual(result['exit'],1)
            self.assertEqual((output/'partial.dll').read_bytes(),b'failed-PE')
            self.assertIn('partial.dll',json.loads((runner.out/'a-compile-products.json').read_text()))

    def test_complete_streams_and_command_binding(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp); prefix=root/'call'; argv=['fake']; env={'LC_ALL':'C.UTF-8'}
            record=dict(argv=argv,cwd=str(root),timeout_seconds=30,exit=0,failure=None,raw_streams_complete=True,streams={})
            for stream in ('stdout','stderr'):
                path=prefix.with_suffix('.'+stream);path.write_bytes(b'good' if stream=='stdout' else b'')
                record['streams'][stream]={'bytes':path.stat().st_size,'sha256':sha(path)}
            save(prefix.with_suffix('.argv.json'),record);save(prefix.with_suffix('.json'),record)
            save(Path(str(prefix)+'-environment.json'),env)
            verify_record(record,prefix,argv,root,30,env)
            for altered in [dict(record,exit=1),dict(record,failure='timeout'),dict(record,raw_streams_complete=False)]:
                self.assertFalse(successful(altered))
            with self.assertRaises(ValueError): verify_record(record,prefix,['other'],root,30,env)
            save(Path(str(prefix)+'-environment.json'),{})
            with self.assertRaises(ValueError): verify_record(record,prefix,argv,root,30,env)
            save(Path(str(prefix)+'-environment.json'),env)
            prefix.with_suffix('.stderr').unlink()
            with self.assertRaises(ValueError): verify_record(record,prefix,argv,root,30,env)
            prefix.with_suffix('.stderr').write_bytes(b'')
            prefix.with_suffix('.stdout').write_bytes(b'tampered')
            with self.assertRaises(ValueError): verify_record(record,prefix,argv,root,30,env)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--self-test', action='store_true')
    if parser.parse_args().self_test: unittest.main(argv=[sys.argv[0]], verbosity=2)
    else: raise SystemExit(Runner().execute())
