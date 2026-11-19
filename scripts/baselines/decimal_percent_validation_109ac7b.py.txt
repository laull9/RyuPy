#!/usr/bin/env python3
"""两图百分号同源采集：薄metadata Runner，逐行分类；C#语义红不挡独立Native。"""
import argparse
import hashlib
import os
from pathlib import Path
import shutil
import sys
import tarfile
import traceback
import decimal_percent_contract as c
from textwrap_dedent_native import native

base = c.shared.shared.metadata
helper = c.shared.shared
ROOT, require, sha, save = c.ROOT, c.require, c.sha, helper.save


def stages(data):
    names = ['identity', 'csharp-build', 'csharp-tests', 'tools', 'sourceprep', 'termcolor-source']
    for graph in data['graphs']:
        names += [graph['id'] + '-compile', graph['id'] + '-native']
        for row in graph['cases']:
            names += [row['id'] + '-jit'] + ([row['id'] + '-cp'] if row['cp'] else [])
    for row in data['old_cases']: names += [row['id'] + '-' + phase for phase in ('compile', 'cp', 'jit')]
    return names


class Runner(base.Runner):
    def __init__(self, resume=False):
        self.data = c.parse(c.MANIFEST.read_bytes())
        self.out = self.root_out = ROOT / 'target/decimal-percent-evidence'
        self.out.mkdir(parents=True, exist_ok=resume)
        self.inputs_dir = self.out / 'inputs'
        self.before, self.identity_failed = None, False
        self.python = self.dotnet = self.cli = self.runtime = None
        self.labels, self.frozen, self.input_inventories, self.isolation = set(), {}, {}, {}
        self.run_records = {}
        self.ready = False
        self.result = dict(passed=False, default_project_delta=0, retries=0,
            scope='两新图；NI/宿主偏差/真实trace逐row分账；整体NOT_GO不因本切自动升级',
            oracle_roles=self.data['oracle_roles'], old_gates='同HEAD独立原jobs，不冒领通过',
            stages={name: dict(passed=False, status='not-run') for name in stages(self.data)})

    def fresh_identity(self):
        return dict(head=self.before['head'], tree=self.before['tree'], run_id=os.environ['GITHUB_RUN_ID'],
            run_attempt=os.environ['GITHUB_RUN_ATTEMPT'], manifest_sha256=sha(c.MANIFEST), sources=self.data['sources'],
            tools={name: sha(path) for name, path in self.tool_paths().items()})

    def tool_paths(self):
        return dict(python=self.python, dotnet=self.dotnet, cli=self.cli, runtime=self.runtime,
                    cargo=Path(shutil.which('cargo')).resolve(), rustc=Path(shutil.which('rustc')).resolve())

    def persist(self):
        checkpoint = None
        if self.ready:
            # 只绑定已采集原件，不把可变摘要/state本身当证据；Native不另制恢复包。
            # 未验证提取树只保诊断，不纳入authority；普通archive原件仍保留。
            unverified = self.root_out / 'termcolor-unverified'
            files = {str(p.relative_to(self.root_out)): sha(p) for p in self.root_out.rglob('*')
                     if not p.is_relative_to(unverified) and p.is_file() and p.name not in ('state.json', 'result.json', 'resume-state.raw.json')
                     and 'native-domains' not in p.parts}
            # 已核原件沿用原authority；不能因persist重算而接受变流或悄悄移除已消失原件。
            for name, digest in self.frozen.items():
                path = Path(name)
                if path.is_relative_to(self.root_out) and not path.is_relative_to(unverified) and path.name not in ('state.json', 'result.json', 'resume-state.raw.json') and 'native-domains' not in path.parts:
                    files[str(path.relative_to(self.root_out))] = digest
            checkpoint = dict(schema_version=1, identity=self.fresh_identity(), runs=self.run_records, files=files)
        save(self.root_out / 'state.json', dict(schema_version=1, before=self.before,
            frozen=self.frozen, inventories=self.input_inventories, isolation=self.isolation,
            environment=getattr(self, 'env', None), labels=sorted(self.labels), checkpoint=checkpoint))
        save(self.root_out / 'result.json', self.result)

    def identities(self):
        c.check_context(os.environ, c.parse(Path(os.environ['GITHUB_EVENT_PATH']).read_bytes()))
        c.load_manifest()
        self.before = self.identity('before')
        require(self.before['head'] == os.environ['GITHUB_SHA'], 'HEAD不同于事件')
        self.python, self.dotnet = Path(os.environ['PYTHON']).resolve(), Path(os.environ['DOTNET']).resolve()
        require(Path(sys.executable).resolve() == self.python and sys.version_info[:3] == (3, 12, 13)
                and sha(self.python) == self.data['cp_sha256'], '固定CP实际解释器不符')
        for name, version in [('dotnet', '10.0.403'), ('rustc', 'rustc 1.99.1 (58cf8f9c3 2026-10-13)'),
                              ('cargo', 'cargo 1.99.1 (5f94df478 2026-08-27)')]:
            path = self.dotnet if name == 'dotnet' else Path(shutil.which(name)).resolve()
            self.checked(name + '-version', [path, '--version'], 30)
            require((self.out / (name + '-version.stdout')).read_text().strip() == version, '固定工具版本不符')
            self.frozen[str(path)] = sha(path)
        self.frozen[str(self.python)] = sha(self.python)
        self.frozen[str(c.MANIFEST)] = sha(c.MANIFEST)
        self.environment()

    def test_command(self):
        return [self.dotnet, 'test', ROOT / 'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj',
                '-c', 'Release', '--no-build', '--no-restore', '--filter', self.data['csharp']['filter'],
                '--logger', 'trx;LogFileName=result.trx', '--results-directory', self.root_out / 'trx']

    def test_assemblies(self):
        for name in ('RyuPy.Runtime.dll', 'RyuPy.Runtime.Tests.dll'):
            actual = ROOT / 'runtime/RyuPy.Runtime.Tests/bin/Release/net10.0' / name
            saved = self.root_out / 'managed' / name
            require(actual.is_file() and saved.is_file() and sha(actual) == sha(saved), 'C#实际测试程序集不同于保存副本')
            self.frozen[str(actual)] = sha(actual)

    def managed_tests(self):
        self.audit(self.test_assemblies)
        self.barrier('csharp-before')
        self.command('percent-csharp-tests', self.test_command(), 300)
        self.barrier('csharp-after')
        self.audit(self.test_assemblies)
        if (self.out / 'trx').is_dir(): self.preserve(self.out / 'trx', 'trx')
        c.verify_command(self.out / 'percent-csharp-tests', self.test_command())
        return c.verify_trx(self.out / 'trx/result.trx', self.data['csharp']['facts'])

    def prepare_sources(self):
        for source in set(self.data['sources']) | {row['source'] for row in self.data['old_cases']}:
            target = self.inputs_dir / source
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(ROOT / source, target)
        # termcolor只恢复一个已有锁定官方sdist，失败与新两图无依赖。
        self.phase('termcolor-source', self.prepare_termcolor)
        self.freeze_inputs(self.inputs_dir)
        self.preserve(self.inputs_dir, 'compiled-inputs')

    def prepare_termcolor(self):
        recipe = c.parse((ROOT / 'tests/aot/wcwidth_ci/project-source-recovery.json').read_bytes())
        items = [row for row in recipe['packages'] if row['package'] == 'termcolor' and row['version'] == '2.4.0']
        require(len(items) == 1, '锁定termcolor来源不唯一')
        item = items[0]
        require(item == self.data['termcolor'], 'termcolor URL/SHA/版本漂移')
        archive = self.root_out / item['filename']
        self.checked('termcolor-download', ['curl', '--fail', '--location', '--show-error', '--max-time', '180',
            '--proto', '=https', '--proto-redir', '=https', item['url'], '-o', archive], 200)
        require(sha(archive) == item['sha256'], 'termcolor归档hash不符')
        destination = self.root_out / 'termcolor-unverified'
        destination.mkdir()
        with tarfile.open(archive) as handle: handle.extractall(destination, filter='data')
        # 尚未安装的外部暂存不是全局authority；坏包仅拒绝本消费者，绝不发布到inputs。
        try:
            inventory = base.inventory(destination)
        except helper.IdentityError as error:
            raise ValueError('termcolor未安装暂存验证拒绝：' + str(error)) from error
        require((destination / 'termcolor-2.4.0/src').is_dir(), 'termcolor实际src来源缺失')
        destination.rename(self.inputs_dir / 'termcolor-package')
        save(self.root_out / 'termcolor-source.json', dict(recipe=item, archive_sha256=sha(archive),
             inventory=inventory))
        self.frozen[str(archive)] = sha(archive)
        return dict(package='termcolor', version='2.4.0', archives=1)

    def termcolor_root(self):
        return self.inputs_dir / 'termcolor-package/termcolor-2.4.0/src'

    def graph_target(self, graph):
        return self.root_out / 'products' / graph['id']

    def compile_graph(self, graph):
        case, target = c.cases(graph)[0], self.graph_target(graph)
        argv = helper.build_argv(self, case, target)
        self.command(graph['id'] + '-compile-command', argv, 180, self.env)
        c.verify_command(self.root_out / (graph['id'] + '-compile-command'), argv)
        self.audit(lambda: helper.verify_plan(target / 'module-environment.json', case))
        self.audit(lambda: require(sha(target / 'RyuPy.Runtime.dll') == sha(self.runtime), '图runtime换源'))
        self.preserve(target, graph['id'] + '-pe')

    def record_row(self, name, argv, row, source, env=None):
        record = self.run_product(name, argv, env)
        self.run_records[name] = dict(record=record, prefix=name, argv=list(map(str, argv)),
                                     source_sha256=sha(source))
        return c.verify_row(self.out / name, argv, row, source)

    def run_positive(self, name, argv, golden, env=None):
        # Native helper按固定ID回调；未知行不能获得宽松stderr或exit1权利。
        row = next((r for g in self.data['graphs'] for r in c.cases(g)
                    if name in (r['id'] + '-original', r['id'] + '-relocated')), None)
        require(row is not None and golden == row['stdout'].encode(), 'Native行ID或完整golden不符')
        self.run_product(name, argv, env)
        return c.verify_row(self.out / name, argv, row, ROOT / row['source'])

    def native_graph(self, graph):
        detail = native(self, c.cases(graph))
        detail.pop('cp_equivalent', None); detail.pop('expected_ni', None)
        detail['case_kinds'] = {row['id']: row['kind'] for row in graph['cases']}
        return detail

    def old_compile_argv(self, row):
        argv = [self.cli, 'build', self.inputs_dir / row['source'], '--diagnostic-stages',
                '--out-dir', self.root_out / 'products' / row['id']]
        if row['id'] == 'old-termcolor': argv += ['-I', self.termcolor_root()]
        return argv

    def old_env(self, row):
        return dict(self.env, PYTHONPATH=str(self.termcolor_root())) if row['id'] == 'old-termcolor' else self.env

    def old_checks(self, row):
        name, source = row['id'], self.inputs_dir / row['source']
        if name == 'old-termcolor' and not (self.root_out / 'termcolor-source.json').is_file():
            for phase in ('cp', 'compile', 'jit'): self.block(name + '-' + phase, '锁定termcolor来源未就绪')
            return
        cp_argv = [self.python, '-B', source, *row['argv']]
        def cp():
            record = self.run_product(name + '-cp-run', cp_argv, self.old_env(row))
            self.run_records[name + '-cp-run'] = dict(record=record, prefix=name + '-cp-run',
                argv=list(map(str, cp_argv)), source_sha256=sha(source))
            actual = dict(row, stdout=(self.out / (name + '-cp-run.stdout')).read_text() if row['kind'] != 'trace' else row['stdout'])
            return c.verify_row(self.out / (name + '-cp-run'), cp_argv, actual, source)
        cp_ok = self.phase(name + '-cp', cp)
        argv = self.old_compile_argv(row)
        def compile_old():
            self.command(name + '-compile-command', argv, 180, self.env)
            c.verify_command(self.out / (name + '-compile-command'), argv)
            self.preserve(self.root_out / 'products' / name, name + '-pe')
        compiled = self.phase(name + '-compile', compile_old)
        if cp_ok and compiled:
            actual = dict(row, stdout=(self.out / (name + '-cp-run.stdout')).read_text() if row['kind'] != 'trace' else row['stdout'])
            pe = self.root_out / 'products' / name / (source.stem + '.dll')
            self.phase(name + '-jit', lambda: self.record_row(name + '-jit-run', [self.dotnet, pe, *row['argv']], actual, source))
        else: self.block(name + '-jit', '本旧源CP或compile未通过，不影响新图')

    def resume_expectations(self):
        """固定两图从真实compile原件推导应有run集合，不信state中的通过/阻断标签。"""
        expected, semantic_rows, required = {}, {}, set()
        def files(name):
            required.update(name + '.' + suffix for suffix in ('stdout', 'stderr', 'json', 'argv.json'))
        def command(name, argv, success=False):
            files(name)
            record, _ = c.verify_process(self.root_out / name, argv, exits=tuple(range(256)))
            if success: c.verify_command(self.root_out / name, argv)
            return record['exit'] == 0
        def run(name, argv, row, source):
            files(name)
            expected[name] = dict(argv=list(map(str, argv)), source_sha256=sha(source), stdout=row['stdout'])
            semantic_rows[name] = (row, source)
        tests = ROOT / 'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj'
        command('csharp-build', [self.dotnet, 'build', tests, '-c', 'Release', '-nologo'], True)
        command('compiler-build', ['cargo', 'build', '--locked', '-p', 'ryupy-cli', '--features', helper.old.shared.FEATURE], True)
        command('percent-csharp-tests', self.test_command())
        required.update(('tools/ryupy', 'tools/RyuPy.Runtime.dll', 'managed/RyuPy.Runtime.dll', 'managed/RyuPy.Runtime.Tests.dll'))
        for graph in self.data['graphs']:
            target = self.graph_target(graph)
            compiled = command(graph['id'] + '-compile-command', helper.build_argv(self, c.cases(graph)[0], target))
            self.result['stages'][graph['id'] + '-compile'] = dict(passed=compiled, status='passed' if compiled else 'failed')
            if compiled:
                required.update(str((target / filename).relative_to(self.root_out)) for filename in
                    (Path(graph['source']).stem + '.dll', 'RyuPy.Runtime.dll', 'module-environment.json'))
                helper.verify_plan(target / 'module-environment.json', c.cases(graph)[0])
                require(sha(target / 'RyuPy.Runtime.dll') == sha(self.runtime), '已编译runtime换源')
            source = self.inputs_dir / graph['source']
            for row in graph['cases']:
                if row['cp']: run(row['id'] + '-cp-run', [self.python, '-I', '-B', source, *row['argv']], row, source)
                if compiled:
                    run(row['id'] + '-jit-run', [self.dotnet, target / (source.stem + '.dll'), *row['argv']], row, source)
                else: self.block(row['id'] + '-jit', '本图实际compile失败；Native可独立采集')
        termcolor = (self.root_out / 'termcolor-source.json').is_file()
        require(termcolor == self.termcolor_root().is_dir(), 'termcolor成功标记/实际输入矛盾')
        if termcolor:
            source = c.parse((self.root_out / 'termcolor-source.json').read_bytes())
            require(source['recipe'] == self.data['termcolor'] and source['archive_sha256'] == self.data['termcolor']['sha256']
                    and sha(self.root_out / self.data['termcolor']['filename']) == source['archive_sha256']
                    and source['inventory'] == base.inventory(self.inputs_dir / 'termcolor-package'), 'termcolor来源身份不符')
            required.add('termcolor-source.json')
        for row in self.data['old_cases']:
            name, source = row['id'], self.inputs_dir / row['source']
            if name == 'old-termcolor' and not termcolor:
                for phase in ('cp', 'compile', 'jit'): self.block(name + '-' + phase, 'termcolor来源未就绪')
                continue
            argv = [self.python, '-B', source, *row['argv']]
            prefix = self.root_out / (name + '-cp-run')
            record, streams = c.verify_process(prefix, argv)
            actual = dict(row, stdout=streams['stdout'].decode() if row['kind'] != 'trace' else row['stdout'])
            run(name + '-cp-run', argv, actual, source)
            cp_ok = self.phase(name + '-cp', lambda: c.verify_row(prefix, argv, actual, source))
            compiled = command(name + '-compile-command', self.old_compile_argv(row))
            self.result['stages'][name + '-compile'] = dict(passed=compiled, status='passed' if compiled else 'failed')
            if compiled:
                target = self.root_out / 'products' / name
                required.update(str((target / filename).relative_to(self.root_out)) for filename in
                    (source.stem + '.dll', 'RyuPy.Runtime.dll'))
                require(sha(target / 'RyuPy.Runtime.dll') == sha(self.runtime), '旧保护runtime换源')
            if cp_ok and compiled:
                run(name + '-jit-run', [self.dotnet, self.root_out / 'products' / name / (source.stem + '.dll'), *row['argv']], actual, source)
            else: self.block(name + '-jit', '原CP或compile失败')
        return expected, semantic_rows, required

    def load_state(self):
        raw = c.safe_file(self.root_out, 'state.json').read_bytes()
        with (self.root_out / 'resume-state.raw.json').open('xb') as handle: handle.write(raw)
        state = c.parse(raw)
        require(set(state) == {'schema_version', 'before', 'frozen', 'inventories', 'isolation', 'environment', 'labels', 'checkpoint'}
                and state['schema_version'] == 1 and not state['isolation'], '恢复结构/隔离无效')
        c.check_context(os.environ, c.parse(Path(os.environ['GITHUB_EVENT_PATH']).read_bytes())); c.load_manifest()
        self.python, self.dotnet = Path(os.environ['PYTHON']).resolve(), Path(os.environ['DOTNET']).resolve()
        self.cli, self.runtime = self.root_out / 'tools/ryupy', self.root_out / 'tools/RyuPy.Runtime.dll'
        require(Path(sys.executable).resolve() == self.python and sys.version_info[:3] == (3, 12, 13)
                and sha(self.python) == self.data['cp_sha256'], '恢复固定CP实际解释器不符')
        self.before = self.identity('fresh-resume')
        require(self.before == state['before'], '恢复HEAD/tree/完整源码身份改变')
        self.environment(); require(self.env == state['environment'], '恢复环境改变')
        self.frozen, self.input_inventories = state['frozen'], state['inventories']
        require(set(self.input_inventories) == {str(self.inputs_dir)}, '输入inventory根改变')
        for path in self.frozen:
            require(Path(path).is_absolute(), '冻结路径非法')
            c.safe_file(Path(path).parent, Path(path).name)
        checkpoint = state['checkpoint']
        require(type(checkpoint) is dict, '未完整采集到可核checkpoint')
        # 先核全部原件与身份，再读真实编译结果推导run集合；返回摘要不被采纳。
        require(checkpoint['identity'] == self.fresh_identity(), '跨run/attempt/工具恢复')
        for name, digest in checkpoint['files'].items(): require(sha(c.safe_file(self.root_out, name)) == digest, '原件改变')
        self.test_assemblies()
        self.barrier('resume-inputs')
        expected, rows, required = self.resume_expectations()
        c.verify_checkpoint(checkpoint, self.fresh_identity(), expected, self.root_out, required)
        # 完整恢复核验后登记原digest；不覆盖旧authority，并持续保护到Native/final。
        authority = {str(c.safe_file(self.root_out, name)): digest for name, digest in checkpoint['files'].items()}
        authority[str(c.safe_file(self.root_out, 'resume-state.raw.json'))] = hashlib.sha256(raw).hexdigest()
        for name, digest in authority.items():
            if name in self.frozen and self.frozen[name] != digest:
                raise helper.IdentityError('恢复原件冻结authority冲突：' + name)
        self.frozen.update(authority)
        self.barrier('resume-checkpoint-frozen')
        self.run_records = checkpoint['runs']
        # 不信state摘要；包括异常行在内全部从已验身份的真实原流重算。
        for name, (row, source) in rows.items():
            self.phase(name.removesuffix('-run'), lambda name=name, row=row, source=source:
                       c.verify_row(self.root_out / name, expected[name]['argv'], row, source))
        self.phase('csharp-tests', lambda: self.check_saved_tests())
        for name in ('identity', 'csharp-build', 'tools', 'sourceprep'):
            self.result['stages'][name] = dict(passed=True, status='passed', recomputed=True)
        self.result['stages']['termcolor-source'] = dict(passed=(self.root_out / 'termcolor-source.json').is_file(),
            status='passed' if (self.root_out / 'termcolor-source.json').is_file() else 'failed')
        require(type(state['labels']) is list and len(state['labels']) == len(set(state['labels'])), '重复label')
        self.labels.update(state['labels'])
        self.ready = True

    def check_saved_tests(self):
        c.verify_command(self.root_out / 'percent-csharp-tests', self.test_command())
        return c.verify_trx(self.root_out / 'trx/result.trx', self.data['csharp']['facts'])

    def invalidate(self, error):
        self.ready = False
        super().invalidate(error)

    def execute(self, phase):
        try:
            if phase == 'jit':
                if not self.phase('identity', lambda: self.audit(self.identities)): return 1
                if not self.phase('csharp-build', lambda: self.managed(build_only=True)): return 1
                self.phase('csharp-tests', self.managed_tests)
                if not self.phase('tools', self.build_tools): return 1
                if not self.phase('sourceprep', self.prepare_sources): return 1
                for graph in self.data['graphs']:
                    source = self.inputs_dir / graph['source']
                    for row in graph['cases']:
                        if row['cp']:
                            argv = [self.python, '-I', '-B', source, *row['argv']]
                            self.phase(row['id'] + '-cp', lambda row=row, argv=argv, source=source:
                                self.record_row(row['id'] + '-cp-run', argv, row, source))
                    compiled = self.phase(graph['id'] + '-compile', lambda graph=graph: self.compile_graph(graph))
                    for row in graph['cases']:
                        if compiled:
                            argv = [self.dotnet, self.graph_target(graph) / (source.stem + '.dll'), *row['argv']]
                            self.phase(row['id'] + '-jit', lambda row=row, argv=argv, source=source:
                                self.record_row(row['id'] + '-jit-run', argv, row, source))
                        else: self.block(row['id'] + '-jit', '本图compile失败；其它图继续')
                for row in self.data['old_cases']: self.old_checks(row)
                self.ready = True
            else:
                self.audit(self.load_state)
                for graph in self.data['graphs']:
                    self.phase(graph['id'] + '-native', lambda graph=graph: self.native_graph(graph))
            self.result['passed'] = all(row['passed'] for row in self.result['stages'].values())
        except helper.IdentityError as error:
            self.invalidate(error)
        except Exception as error:
            self.result.update(passed=False, error=str(error), traceback=traceback.format_exc())
        finally:
            try:
                if self.before is not None: self.barrier(phase + '-final')
            except Exception as error: self.invalidate(error)
            self.persist()
        rows = [row for name, row in self.result['stages'].items() if phase == 'native' or not name.endswith('-native')]
        return 0 if not self.identity_failed and all(row['passed'] for row in rows) else 1


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--phase', choices=['jit', 'native'], required=True)
    args = parser.parse_args()
    raise SystemExit(Runner(resume=args.phase == 'native').execute(args.phase))
