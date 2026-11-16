#!/usr/bin/env python3
"""dedent有限同HEAD验收：两阶段共享一次构建，真实CP/JIT/Native分别留证，旧项目分子不变。"""
import argparse
import json
import os
from pathlib import Path
import re
import shutil
import sys
import traceback
import textwrap_dedent_contract as c

ROOT, old, metadata = c.ROOT, c.old, c.metadata
require, sha, save = c.require, c.sha, c.save


def native_groups(data):
    result = {}
    for case in data['cases']:
        if case['native']:
            key = (case['source'], tuple(case['include']), tuple(sorted(case['modules'].items())))
            result.setdefault(key, []).append(case)
    return list(result.values())


def stages(data):
    result = ['identity', 'csharp-build', 'csharp-tests', 'tools', 'sourceprep', 'textwrapper-apps']
    result += ['fresh-' + name for name in data['fresh_scenarios']]
    for case in data['cases']:
        result += [case['id'] + '-cp'] if case['mode'] in ('cp', 'pending') else []
        result += [case['id'] + '-jit'] if case['mode'] != 'pending' else []
    result += [group[0]['id'] + '-native' for group in native_groups(data)]
    return result


class Runner(metadata.Runner):
    def __init__(self, resume=False):
        self.data = c.parse(c.MANIFEST.read_bytes())
        self.out = ROOT / 'target/textwrap-dedent-evidence'
        self.out.mkdir(parents=True, exist_ok=resume)
        self.inputs_dir = self.out / 'inputs'
        self.labels, self.frozen, self.input_inventories, self.isolation = set(), {}, {}, {}
        self.before, self.identity_failed = None, False
        self.python = self.dotnet = self.cli = self.runtime = None
        self.result = {'passed': False, 'default_project_delta': 0, 'retries': 0,
            'scope': '有限dedent；NI负门不是完整CP支持，pending正合同不晋升',
            'old_gates': '同HEAD其它独立jobs，不依赖本job结果',
            'source_isolation_scope': '仅完整编译输入inputs目录撤离原路径；repo原fixture与candidate归档保留，非全机器源码撤除',
            'stages': {name: {'passed': False, 'status': 'not-run'} for name in stages(self.data)}}

    def persist(self):
        save(self.out / 'state.json', {'schema_version': 1, 'manifest_sha256': sha(c.MANIFEST),
            'before': self.before, 'frozen': self.frozen, 'input_inventories': self.input_inventories,
            'isolation': self.isolation, 'identity_failed': self.identity_failed, 'result': self.result,
            'labels': sorted(self.labels), 'environment': getattr(self, 'env', None),
            'tools': {key: str(getattr(self, key)) if getattr(self, key) else None for key in ('python', 'dotnet', 'cli', 'runtime')}})
        save(self.out / 'result.json', self.result)

    def identities(self):
        c.check_context(os.environ, c.parse(Path(os.environ['GITHUB_EVENT_PATH']).read_bytes()))
        c.load_manifest()
        self.before = self.identity('before')
        require(self.before['head'] == os.environ['GITHUB_SHA'], '当前HEAD不同于CI事件')
        save(self.out / 'source-identity.json', self.before)
        archive = self.out / 'candidate-source.tar'
        self.checked('source-archive', ['git', 'archive', '--format=tar', '--output=' + str(archive), 'HEAD'], 60)
        self.frozen[str(archive)] = sha(archive)
        self.frozen[str(c.MANIFEST)] = sha(c.MANIFEST)
        self.python, self.dotnet = Path(os.environ['PYTHON']).resolve(), Path(os.environ['DOTNET']).resolve()
        require(sys.version_info[:3] == (3, 12, 13) and Path(sys.executable).resolve() == self.python
                and sha(self.python) == old.shared.PYTHON_SHA, 'CP版本/实际解释器/固定SHA不符')
        versions = {'dotnet': '10.0.403', 'rustc': 'rustc 1.99.1 (58cf8f9c3 2026-10-13)',
                    'cargo': 'cargo 1.99.1 (5f94df478 2026-08-27)'}
        tools = {'python': self.python}
        for name, expected in versions.items():
            path = self.dotnet if name == 'dotnet' else Path(shutil.which(name) or '').resolve()
            self.checked(name + '-version', [path, '--version'], 30)
            require((self.out / (name + '-version.stdout')).read_text().strip() == expected, '工具版本不符')
            tools[name] = path
        for path, expected in self.data['old_protected'].items():
            require(sha(ROOT / path) == expected, '原保护文件改变：' + path)
        self.frozen.update({str(path): sha(path) for path in tools.values()})
        save(self.out / 'tool-identity.json', {name: {'path': str(path), 'sha256': sha(path)} for name, path in tools.items()})
        self.environment()

    def load_state(self):
        try:
            raw = (self.out / 'state.json').read_bytes()
            (self.out / 'resume-state.raw.json').write_bytes(raw)
            state = c.parse(raw)
            require(type(state) is dict and set(state) == {'schema_version', 'manifest_sha256', 'before', 'frozen',
                    'input_inventories', 'isolation', 'identity_failed', 'result', 'labels', 'environment', 'tools'}, 'state根字段缺失/越界')
            require(state['schema_version'] == 1 and state['manifest_sha256'] == sha(c.MANIFEST), 'state/manifest版本身份不符')
            # 先核局部候选，坏JSON结构不得破坏本次可persist的失败报告或留下旧passed。
            candidate = state['result']
            rows = candidate.get('stages') if type(candidate) is dict else None
            require(type(rows) is dict and set(rows) == set(stages(self.data)) and all(type(row) is dict
                    and type(row.get('passed')) is bool and row.get('status') in
                    ('not-run', 'started', 'blocked', 'passed', 'failed', 'invalidated-identity')
                    and row['passed'] == (row['status'] == 'passed') for row in rows.values()), 'state阶段缺失/越界/矛盾')
            require(type(candidate.get('passed')) is bool and candidate.get('default_project_delta') == 0
                    and candidate.get('retries') == 0 and (not candidate['passed'] or all(row['passed'] for row in rows.values())),
                    'state总状态/分子/重试不符')
            self.result = candidate
            self.before, self.frozen = state['before'], state['frozen']
            self.input_inventories, self.isolation = state['input_inventories'], state['isolation']
            self.labels, self.identity_failed = set(state['labels']), state['identity_failed']
            require(not self.identity_failed and not self.isolation, '先前身份无效/隔离未恢复')
            expected = {'python': Path(os.environ['PYTHON']).resolve(), 'dotnet': Path(os.environ['DOTNET']).resolve(),
                        'cli': self.out / 'tools/ryupy', 'runtime': self.out / 'tools/RyuPy.Runtime.dll'}
            require(state['tools'] == {name: str(path) for name, path in expected.items()}, 'state产物/工具路径被替换')
            for name, path in expected.items(): setattr(self, name, path)
            self.environment()
            require(self.env == state['environment'], '阶段间运行环境改变')
            require(set(self.input_inventories) == {str(self.inputs_dir)}, '完整输入清单缺失/换根')
            c.load_manifest()
            c.check_context(os.environ, c.parse(Path(os.environ['GITHUB_EVENT_PATH']).read_bytes()))
            self.barrier('native-resume')
        except Exception as error:
            self.fail_identity(error)

    def prepare_sources(self):
        for source in self.data['sources']:
            dest = self.inputs_dir / source
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(ROOT / source, dest)
            require(sha(dest) == self.data['sources'][source], '编译输入复制身份不符')
        self.freeze_inputs(self.inputs_dir)
        save(self.out / 'source-isolation-scope.json', {'removed_at_execution': str(self.inputs_dir),
            'original_fixture_paths_retained': sorted(self.data['sources']), 'candidate_archive_retained': True,
            'full_machine_source_removal_claimed': False})

    def managed_tests(self):
        target = self.out / 'trx'
        self.checked('managed-tests', [self.dotnet, 'test', ROOT / 'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj',
            '-c', 'Release', '--no-build', '--no-restore', '--filter',
            'FullyQualifiedName~PyTextDedentTests|FullyQualifiedName~PyTextWrap', '--logger',
            'trx;LogFileName=result.trx', '--results-directory', target], 300)
        self.preserve(target, 'trx')
        return c.verify_trx(target / 'result.trx', self.data['facts'], legacy=True)

    def fresh(self, scenario):
        target = self.out / ('trx-fresh-' + scenario)
        env = dict(self.env, RYUPY_TEXTWRAP_INIT_SCENARIO=scenario)
        name = 'fresh-' + scenario + '-process'
        record = self.command(name, [self.dotnet, 'test', ROOT / 'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj',
            '-c', 'Release', '--no-build', '--no-restore', '--filter', self.data['fresh_filter'],
            '--logger', 'trx;LogFileName=result.trx', '--results-directory', target], 60, env)
        self.preserve(target, name + '-trx')
        require(c.complete(record), '独立C#进程失败/超时/信号')
        detail = c.verify_trx(target / 'result.trx', [self.data['fresh_fact']])
        raw = (target / 'result.trx').read_text()
        pids = re.findall(r'FRESH_OK ' + re.escape(scenario) + r' pid=(\d+)', raw)
        require(len(pids) == 1, '专用Fact未输出唯一真实执行/PID标记')
        detail['testhost_pid'] = int(pids[0])
        return detail

    def legacy_apps(self):
        # 原driver的四应用已经完整核CP原流/用户异常帧；临时目录由本job保存，失败同样上传。
        temp = self.out / 'textwrapper-processes'; temp.mkdir()
        env = dict(self.env, TMPDIR=str(temp), PYTHON=str(self.python))
        record = self.command('textwrapper-apps', ['cargo', 'test', '--locked', '-p', 'ryupy-driver', '--features',
            old.shared.FEATURE, '--test', 'textwrapper', '--', '--nocapture', '--test-threads=1'], 600, env)
        require(c.complete(record), '原TextWrapper四应用失败')
        stdout = (self.out / 'textwrapper-apps.stdout').read_text()
        require('test result: ok. 4 passed; 0 failed; 0 ignored;' in stdout, '四应用漏跑/忽略')
        for name in self.data['textwrapper_tests']:
            require(re.search(r'test ' + re.escape(name) + r' \.\.\. ok', stdout), '旧应用缺少实际通过行')
        for path in temp.rglob('RyuPy.Runtime.dll'):
            require(sha(path) == sha(self.runtime), '旧应用使用不同Runtime')
        require(len(list(temp.glob('ryupy-textwrapper-*'))) == 4, '四应用原流目录缺失')

    def oracle(self, case):
        label = case['id'] + '-cp-run'
        entry = self.inputs_dir / case['source']
        self.run_positive(label, [self.python, '-S', '-B', entry, *case['argv']], case['stdout'].encode(),
            dict(PATH='/usr/bin:/bin', LC_ALL='C.UTF-8', TZ='UTC', PYTHONNOUSERSITE='1',
                 PYTHONHASHSEED='0', PYTHONDONTWRITEBYTECODE='1'))
        return {'cp_equivalent': case['mode'] == 'cp', 'product_support': case['mode'] != 'pending'}

    def jit(self, case):
        label = case['id']; target = self.out / 'products' / label
        self.barrier(label + '-before-compile')
        record = self.command(label + '-compile', c.build_argv(self, case, target), 120, self.env)
        self.barrier(label + '-after-compile')
        require(c.complete(record), '实际编译失败；本域JIT未执行')
        pe = target / (Path(case['source']).stem + '.dll')
        self.audit(lambda: require(pe.is_file() and sha(target / 'RyuPy.Runtime.dll') == sha(self.runtime), 'JIT PE/runtime不符'))
        self.audit(lambda: c.verify_plan(target / 'module-environment.json', case))
        self.preserve(target, label + '-pe')
        self.run_positive(label + '-jit-run', [self.dotnet, pe, *case['argv']], case['stdout'].encode())
        return {'cp_equivalent': case['mode'] == 'cp', 'expected_ni': case['mode'] == 'ni'}

    def native(self, cases):
        from textwrap_dedent_native import native
        return native(self, cases)

    def execute(self, phase):
        try:
            if phase == 'jit':
                if not self.phase('identity', lambda: self.audit(self.identities)): return 1
                if not self.phase('csharp-build', lambda: self.managed(build_only=True)): return 1
                self.phase('csharp-tests', self.managed_tests)
                for scenario in self.data['fresh_scenarios']:
                    self.phase('fresh-' + scenario, lambda scenario=scenario: self.fresh(scenario))
                if not self.phase('tools', self.build_tools): return 1
                self.phase('textwrapper-apps', self.legacy_apps)
                if not self.phase('sourceprep', self.prepare_sources): return 1
                for case in self.data['cases']:
                    if case['mode'] in ('cp', 'pending'):
                        self.phase(case['id'] + '-cp', lambda case=case: self.oracle(case))
                    if case['mode'] != 'pending':
                        if case['mode'] == 'cp' and not self.result['stages'][case['id'] + '-cp']['passed']:
                            self.block(case['id'] + '-jit', '同源CP失败')
                        else: self.phase(case['id'] + '-jit', lambda case=case: self.jit(case))
            else:
                self.load_state()
                require(self.result['stages']['tools']['passed'] and self.result['stages']['sourceprep']['passed'], '同源工具/输入未就绪')
                for group in native_groups(self.data):
                    name = group[0]['id'] + '-native'
                    if any(case['mode'] == 'cp' and not self.result['stages'][case['id'] + '-cp']['passed'] for case in group):
                        self.block(name, '同源CP失败')
                    else: self.phase(name, lambda group=group: self.native(group))
            self.result['passed'] = all(row['passed'] for row in self.result['stages'].values())
        except c.IdentityError as error:
            self.invalidate(error)
        except Exception as error:
            self.result.update(passed=False, error=str(error), traceback=traceback.format_exc())
        finally:
            try: self.barrier(phase + '-final')
            except Exception as error: self.invalidate(error)
            self.persist()
        considered = [row for name, row in self.result['stages'].items() if phase == 'native' or not name.endswith('-native')]
        return 0 if not self.identity_failed and 'error' not in self.result and all(row['passed'] for row in considered) else 1


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--phase', required=True, choices=['jit', 'native'])
    args = parser.parse_args()
    raise SystemExit(Runner(args.phase == 'native').execute(args.phase))
