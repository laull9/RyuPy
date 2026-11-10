#!/usr/bin/env python3
"""Scanner 同候选两阶段验收：复用既有采集/身份/Native机制，每图一次PE并多argv执行。"""
import argparse
import os
from pathlib import Path
import shutil
import sys
import traceback
import re_scanner_contract as c

ROOT, old, base = c.ROOT, c.shared.old, c.shared.metadata
require, sha, save = c.require, c.sha, c.shared.save


def stage_names(data):
    names = ['identity', 'csharp-build', 'csharp-tests', 'tools', 'sourceprep', 'attr-list-jit', 'attr-list-native']
    for graph in data['graphs']:
        if graph['native']:
            names += [graph['id'] + '-compile', graph['id'] + '-native']
            names += [row['id'] + '-jit' for row in c.native_cases(graph)]
    return names


class Runner(base.Runner):
    def __init__(self, resume=False):
        self.data = c.parse(c.MANIFEST.read_bytes())
        self.out = ROOT / 'target/re-scanner-evidence'
        self.root_out = self.out
        self.out.mkdir(parents=True, exist_ok=resume)
        self.inputs_dir = self.out / 'inputs'
        self.before, self.identity_failed = None, False
        self.labels, self.frozen, self.input_inventories, self.isolation = set(), {}, {}, {}
        self.root_labels = self.labels
        self.python = self.dotnet = self.cli = self.runtime = None
        self.run_records, self.snapshot_reports = {}, {}
        self.result = {'passed': False, 'collection_ready': False, 'default_project_delta': 0, 'retries': 0,
            'scope': '四候选图及NI分账；pending非产品支持；attr_list不替代完整extra/默认20',
            'oracle_execution': '只核先前固定CP原流；本专用job不重复采CP fixture',
            'stages': {name: dict(passed=False, status='not-run') for name in stage_names(self.data)}}

    def fresh_identity(self):
        tools = {name: sha(getattr(self, name)) for name in ('python', 'dotnet', 'cli', 'runtime')}
        tools.update({name: sha(Path(shutil.which(name) or '').resolve()) for name in ('cargo', 'rustc')})
        return dict(head=self.before['head'], tree=self.before['tree'], run_id=os.environ['GITHUB_RUN_ID'],
            run_attempt=os.environ['GITHUB_RUN_ATTEMPT'], manifest_sha256=sha(c.MANIFEST),
            sources=self.data['sources'], tools=tools)

    def persist(self):
        # 旧Native helper暂换out作分域采集；总恢复状态始终属于原job根，不挪JIT相对原流。
        destination = self.root_out
        checkpoint = None
        if self.cli and self.runtime and self.before:
            files = {str(Path(name).relative_to(destination)): digest for name, digest in self.frozen.items()
                     if Path(name).is_relative_to(destination)}
            for row in self.run_records.values():
                for suffix in ('stdout', 'stderr', 'json', 'argv.json'):
                    path = (destination / row['prefix']).with_suffix('.' + suffix)
                    if path.is_file(): files[str(path.relative_to(destination))] = sha(path)
            for name in self.successful_commands():
                for suffix in ('stdout', 'stderr', 'json', 'argv.json'):
                    path = (destination / name).with_suffix('.' + suffix)
                    if path.is_file(): files[str(path.relative_to(destination))] = sha(path)
            checkpoint = dict(schema_version=1, identity=self.fresh_identity(), runs=self.run_records, files=files)
        save(destination / 'state.json', dict(schema_version=1, before=self.before, frozen=self.frozen,
            inventories=self.input_inventories, isolation=self.isolation, labels=sorted(self.root_labels),
            result=self.result, environment=getattr(self, 'env', None), checkpoint=checkpoint,
            snapshots=self.snapshot_reports))
        save(destination / 'result.json', self.result)

    def identities(self):
        c.check_context(os.environ, c.parse(Path(os.environ['GITHUB_EVENT_PATH']).read_bytes()))
        c.load_manifest(); c.qualified_oracles(self.data)
        self.before = self.identity('before')
        require(self.before['head'] == os.environ['GITHUB_SHA'], 'HEAD不同于事件')
        self.python, self.dotnet = Path(os.environ['PYTHON']).resolve(), Path(os.environ['DOTNET']).resolve()
        require(Path(sys.executable).resolve() == self.python and sys.version_info[:3] == (3, 12, 13)
                and sha(self.python) == self.data['cp_sha256'], '实际固定CP工具不符')
        for name, expected in [('dotnet', '10.0.403'), ('rustc', 'rustc 1.99.1 (58cf8f9c3 2026-10-13)'),
                               ('cargo', 'cargo 1.99.1 (5f94df478 2026-08-27)')]:
            path = self.dotnet if name == 'dotnet' else Path(shutil.which(name) or '').resolve()
            self.checked(name + '-version', [path, '--version'], 30)
            require((self.out / (name + '-version.stdout')).read_text().strip() == expected, '工具版本不符')
            self.frozen[str(path)] = sha(path)
        self.frozen[str(self.python)] = sha(self.python)
        self.frozen[str(c.MANIFEST)] = sha(c.MANIFEST)
        archive = self.out / 'candidate-source.tar'
        self.checked('source-archive', ['git', 'archive', '--format=tar', '--output=' + str(archive), 'HEAD'], 60)
        self.frozen[str(archive)] = sha(archive)
        self.environment()
        for path, digest in self.data['old_protected'].items():
            require(sha(ROOT / path) == digest, '旧输入/旧已验源改变：' + path)

    def managed_tests(self):
        facts = self.data['csharp']['facts']
        target = self.out / 'trx'
        self.test_assemblies()
        self.barrier('csharp-tests-before')
        record = self.command('scanner-csharp-tests', [self.dotnet, 'test', ROOT / 'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj',
            '-c', 'Release', '--no-build', '--no-restore', '--filter', self.data['csharp']['filter'],
            '--logger', 'trx;LogFileName=result.trx', '--results-directory', target], 300)
        self.test_assemblies()
        self.barrier('csharp-tests-after')
        self.preserve(target, 'trx')
        require(c.shared.complete(record), '真实C#进程失败/信号/超时')
        return c.verify_trx(target / 'result.trx', facts)

    def successful_commands(self):
        from re_scanner_workload import argv as workload_argv
        tests = ROOT / 'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj'
        commands = {'csharp-build': [self.dotnet, 'build', tests, '-c', 'Release', '-nologo'],
            'scanner-csharp-tests': [self.dotnet, 'test', tests, '-c', 'Release', '--no-build', '--no-restore',
                '--filter', self.data['csharp']['filter'], '--logger', 'trx;LogFileName=result.trx',
                '--results-directory', self.root_out / 'trx'],
            'compiler-build': ['cargo', 'build', '--locked', '-p', 'ryupy-cli', '--features', old.shared.FEATURE]}
        for graph in self.data['graphs']:
            if graph['native']:
                commands[graph['id'] + '-compile-command'] = c.shared.build_argv(
                    self, c.native_cases(graph)[0], self.root_out / 'products' / graph['id'])
        commands['attr-list-compile'] = workload_argv(self, self.root_out / 'products/attr-list')
        return commands

    def invalidate(self, error):
        self.result['collection_ready'] = False
        super().invalidate(error)

    def preserve_resume(self):
        raw = c.safe_file(self.out, 'state.json').read_bytes()
        # 独占写入，不覆盖上次失败恢复原件，也不允许第二次Native补采。
        with (self.out / 'resume-state.raw.json').open('xb') as handle:
            handle.write(raw)
        return c.parse(raw)

    def test_assemblies(self):
        # --no-build实际从repo/bin加载；这两份真实程序集必须等于工具来源的managed副本。
        binary = ROOT / 'runtime/RyuPy.Runtime.Tests/bin/Release/net10.0'
        for name in ('RyuPy.Runtime.dll', 'RyuPy.Runtime.Tests.dll'):
            actual, saved = binary / name, self.root_out / 'managed' / name
            require(actual.is_file() and saved.is_file() and sha(actual) == sha(saved), 'C#实际程序集不同于保存副本')
            self.frozen.setdefault(str(actual), sha(actual))
            require(self.frozen[str(actual)] == sha(actual), 'C#实际程序集在测试前后改变')

    def prepare_sources(self):
        # 原metadata helper只在hosted执行；获取锁定Markdown及四份固定CP标准库源。
        self.inputs_dir = self.inputs(self.python)
        for source, digest in self.data['sources'].items():
            target = self.inputs_dir / source
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(ROOT / source, target)
            require(sha(target) == digest, '实际编译输入换源')
        save(self.inputs_dir / 'metadata-request.json', dict(schema_version=1, mode='closed_snapshot',
             roots=[dict(id='markdown', path='markdown')]))
        self.freeze_inputs(self.inputs_dir)

    def record_run(self, name, argv, stdout, source_sha256, env=None):
        record = self.run_product(name, argv, env)
        self.run_records[name] = dict(record=record, prefix=name, argv=list(map(str, argv)), source_sha256=source_sha256)
        if name == 'negative-timeout-jit-run':
            return self.timeout_observation(name, record)
        c.verify_output(record, self.out / name, stdout)

    def timeout_observation(self, name, record):
        detail = c.verify_timeout_output(record, self.out / name)
        path = self.out / (name + '-observation.json')
        save(path, detail)
        self.frozen[str(path)] = sha(path)
        return detail

    def run_positive(self, name, argv, golden, env=None):
        if name in ('negative-timeout-original', 'negative-timeout-relocated'):
            require(golden == b'resource-timeout RESOURCE_COMPLETED\n', 'timeout入口基准改变')
            return self.timeout_observation(name, self.run_product(name, argv, env))
        return super().run_positive(name, argv, golden, env)

    def compile_graph(self, graph):
        case = c.native_cases(graph)[0]
        target = self.out / 'products' / graph['id']
        self.barrier(graph['id'] + '-before-compile')
        record = self.command(graph['id'] + '-compile-command', c.shared.build_argv(self, case, target), 180, self.env)
        self.barrier(graph['id'] + '-after-compile')
        require(c.shared.complete(record), 'CLI编译失败；本图JIT未运行')
        pe = target / 'entry.dll' if graph['mode'] != 'ni' else target / 'negative.dll'
        self.audit(lambda: require(pe.is_file() and sha(target / 'RyuPy.Runtime.dll') == sha(self.runtime), '实际PE/runtime身份不符'))
        self.audit(lambda: c.shared.verify_plan(target / 'module-environment.json', case))
        self.preserve(target, graph['id'] + '-pe')
        return pe

    def expected_runs(self):
        expected = {}
        for graph in self.data['graphs']:
            if not graph['native']: continue
            pe = self.out / 'products' / graph['id'] / (Path(graph['source']).stem + '.dll')
            for row in c.native_cases(graph):
                expected[row['id'] + '-jit-run'] = dict(argv=list(map(str, [self.dotnet, pe, *row['argv']])),
                    source_sha256=graph['source_sha256'], stdout=row['stdout'], observation=row['observation'])
        real = self.data['real_workload']
        expected['attr-list-jit-run'] = dict(argv=list(map(str, [self.dotnet, self.out / 'products/attr-list/attr_list_entry.dll'])),
            source_sha256=real['source_sha256'], stdout=(ROOT / real['golden']).read_text())
        return expected

    def load_state(self):
        self.result['collection_ready'] = False
        state = self.preserve_resume()
        require(state['schema_version'] == 1 and not state['isolation'], '恢复版本/隔离状态无效')
        self.labels = set(state['labels'])
        self.root_labels = self.labels
        require(len(self.labels) == len(state['labels']), '恢复命令重复')
        self.python, self.dotnet = Path(os.environ['PYTHON']).resolve(), Path(os.environ['DOTNET']).resolve()
        self.cli, self.runtime = self.out / 'tools/ryupy', self.out / 'tools/RyuPy.Runtime.dll'
        c.check_context(os.environ, c.parse(Path(os.environ['GITHUB_EVENT_PATH']).read_bytes()))
        c.load_manifest(); c.qualified_oracles(self.data)
        self.before = self.identity('fresh-resume')
        require(self.before == state['before'], '恢复HEAD/tree/全部源码身份不符')
        self.environment()
        require(self.env == state['environment'], '恢复环境改变')
        self.frozen, self.input_inventories = state['frozen'], state['inventories']
        require(type(self.frozen) is dict, '恢复冻结表结构无效')
        for name in self.frozen:
            path = Path(name)
            require(path.is_absolute() and str(path) == name, '恢复冻结路径不规范')
            c.safe_file(path.parent, path.name)
        require(set(self.input_inventories) == {str(self.inputs_dir)}, '完整编译输入域缺失')
        checkpoint = state['checkpoint']
        all_expected = self.expected_runs()
        require(set(checkpoint['runs']) == set(all_expected) and len(all_expected) == 20,
                'Native前必须完整19个图argv及attr_list共20条JIT原流')
        # 语义红也须完整身份；构建、C#和六次图编译仍必须真实成功。
        mandatory = {'tools/ryupy', 'tools/RyuPy.Runtime.dll', 'managed/RyuPy.Runtime.dll',
                     'managed/RyuPy.Runtime.Tests.dll', 'trx/result.trx'}
        mandatory.update(name + '.' + suffix for name in self.successful_commands()
                         for suffix in ('stdout', 'stderr', 'json', 'argv.json'))
        for expected in all_expected.values():
            pe = Path(expected['argv'][1])
            mandatory.update(str((pe.parent / name).relative_to(self.out)) for name in
                             (pe.name, 'RyuPy.Runtime.dll', 'module-environment.json'))
        verified = c.verify_checkpoint(checkpoint, self.fresh_identity(), all_expected, self.out, mandatory)
        commands = self.successful_commands()
        require(set(commands) | set(all_expected) <= self.labels, '命令label缺少同轮执行记录')
        for name, expected in commands.items(): c.verify_command(self.root_out / name, expected)
        trx = c.verify_trx(self.out / 'trx/result.trx', self.data['csharp']['facts'])
        require(trx['total'] == trx['new_facts'] == 43, '必须同轮恰43个Scanner Fact')
        require(sha(self.runtime) == sha(self.out / 'managed/RyuPy.Runtime.dll'), '工具与C#测试runtime不一致')
        for expected in all_expected.values():
            require(sha(Path(expected['argv'][1]).parent / 'RyuPy.Runtime.dll') == sha(self.runtime), 'PE旁Runtime不是同轮工具')
        # 全部原record/argv和双流也进入之后每道barrier，不仅校验时瞬时存在。
        for name, digest in checkpoint['files'].items():
            path = str(c.safe_file(self.out, name))
            require(path not in self.frozen or self.frozen[path] == digest, '两份冻结身份矛盾')
            self.frozen[path] = digest
        self.frozen[str(self.out / 'resume-state.raw.json')] = sha(self.out / 'resume-state.raw.json')
        self.test_assemblies()
        self.barrier('resume-full-identity')
        self.snapshot_reports = state['snapshots']
        self.run_records = checkpoint['runs']
        self.restore_results(verified, trx, commands)
        # 此处是唯一准入置真点，任何先前硬门异常均不会继承旧result宣称通过。
        self.result['collection_ready'] = True

    def restore_results(self, verified, trx, commands):
        """仅用重核原件重建43/20事实，不读取state.result中的passed、数量或摘要。"""
        for name in ('identity', 'csharp-build', 'csharp-tests', 'tools', 'sourceprep'):
            self.result['stages'][name] = dict(passed=True, status='passed', reverified=True)
        self.result['stages']['csharp-tests'].update(trx)
        for name in verified:
            stage = name.removesuffix('-run')
            self.result['stages'][stage] = dict(verified[name], reverified=True)
        for graph in self.data['graphs']:
            if graph['native']:
                self.result['stages'][graph['id'] + '-compile'] = dict(passed=True, status='passed', reverified=True)
        self.result['compile_commands'] = {name: dict(passed=True, status='passed', reverified=True)
            for name in commands if name.endswith(('-compile-command', '-compile'))}
        self.result['jit_observed'] = len(verified)
        self.result['jit_semantic_failed'] = [name for name, row in verified.items() if not row['passed']]

    def native_graph(self, graph):
        from textwrap_dedent_native import native
        cases = c.native_cases(graph)
        detail = native(self, cases)
        if graph['mode'] == 'ni':
            detail.pop('expected_ni', None)
            detail.update(ni_only_scenarios=4, mixed_resource_scenarios=1, timeout_observation_scenarios=1)
            folder = self.root_out / 'native-domains' / cases[0]['id']
            detail['timeout_observations'] = {mode: c.parse((folder / ('negative-timeout-' + mode + '-observation.json')).read_bytes())
                                              for mode in ('original', 'relocated')}
        return detail

    def execute(self, phase):
        from re_scanner_workload import jit as real_jit, native as real_native
        try:
            if phase == 'jit':
                if not self.phase('identity', lambda: self.audit(self.identities)): return 1
                if not self.phase('csharp-build', lambda: self.managed(build_only=True)): return 1
                self.phase('csharp-tests', self.managed_tests)
                if not self.phase('tools', self.build_tools): return 1
                if not self.phase('sourceprep', self.prepare_sources): return 1
                for graph in self.data['graphs']:
                    if not graph['native']: continue
                    pe = self.out / 'products' / graph['id'] / (Path(graph['source']).stem + '.dll')
                    if self.phase(graph['id'] + '-compile', lambda graph=graph: self.compile_graph(graph)):
                        for row in c.native_cases(graph):
                            self.phase(row['id'] + '-jit', lambda row=row, graph=graph, pe=pe: self.record_run(
                                row['id'] + '-jit-run', [self.dotnet, pe, *row['argv']], row['stdout'], graph['source_sha256']))
                    else:
                        for row in c.native_cases(graph): self.block(row['id'] + '-jit', '同图PE未生成')
                self.phase('attr-list-jit', lambda: real_jit(self))
            else:
                self.audit(self.load_state)
                for graph in self.data['graphs']:
                    if graph['native']:
                        self.phase(graph['id'] + '-native', lambda graph=graph: self.native_graph(graph))
                self.phase('attr-list-native', lambda: real_native(self))
            self.result['passed'] = all(row['passed'] for row in self.result['stages'].values())
        except c.shared.IdentityError as error:
            self.invalidate(error)
        except Exception as error:
            self.result.update(passed=False, error=str(error), traceback=traceback.format_exc())
        finally:
            try: self.barrier(phase + '-final')
            except Exception as error: self.invalidate(error)
            self.persist()
        rows = [value for name, value in self.result['stages'].items() if phase == 'native' or not name.endswith('-native')]
        return 0 if not self.identity_failed and all(row['passed'] for row in rows) else 1


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--phase', choices=['jit', 'native'], required=True)
    args = parser.parse_args()
    raise SystemExit(Runner(resume=args.phase == 'native').execute(args.phase))
