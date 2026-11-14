#!/usr/bin/env python3
"""同轮有界TreeBuilder竖切：真实JIT、独立核心Native、原Markdown及资源完备AOT边界。"""
import os
from pathlib import Path
import shutil
import sys
import traceback
import treebuilder_contract as c
from textwrap_dedent_native import native, preserve_before_validation

base = c.shared.metadata
old = c.shared.old
raw = old.shared
require, sha, save, ROOT = c.require, c.sha, c.shared.save, c.ROOT


class Runner(base.Runner):
    def __init__(self):
        self.data = c.load_manifest()
        self.out = self.root_out = ROOT / 'target/treebuilder-evidence'
        self.out.mkdir(parents=True, exist_ok=False)
        self.inputs_dir = self.out / 'inputs'
        self.before, self.identity_failed = None, False
        self.labels, self.frozen, self.input_inventories, self.isolation = set(), {}, {}, {}
        self.python = self.dotnet = self.cli = self.runtime = None
        names = ['identity', 'tools', 'sourceprep', 'original-types', 'original-cp', 'original-compile',
                 'original-jit', 'core-compile', 'boundaries-compile', 'boundaries-jit',
                 'subclass-syntax', 'subclass-parse', 'subclass-imported', 'core-native', 'markdown-aot',
                 'ordinary-bases-cp', 'ordinary-bases-compile', 'ordinary-bases-jit',
                 'runtime-bases-compile', 'runtime-bases-jit',
                 'mutable-parent-cp', 'mutable-parent-compile', 'mutable-parent-jit']
        names += [g + '-' + mode for g in c.GROUPS for mode in ('cp', 'jit')]
        names += [g + '-' + mode for g in ('markdown-full', 'markdown-html') for mode in ('cp', 'compile', 'jit')]
        self.result = dict(passed=False, default_project_delta=0, retries=0,
            scope='六组有限正域；NI/E0100独立；原Markdown显式配置；完整AOT独立实测、结论以原流为准；不晋升默认11/8/1',
            old_gates='同HEAD原独立jobs，保原所有失败；本job不冒领其结果',
            process_plan=self.data['process_plan'],
            tool_preservation='CLI不随artifact保存，仅保SHA/版本/源码重建信息，不保证同字节工具恢复；实际PE及其runtime另保',
            stages={n: dict(passed=False, status='not-run') for n in names})

    def persist(self):
        save(self.root_out / 'result.json', self.result)
        save(self.root_out / 'execution-identity.json', dict(before=self.before, files=self.frozen,
            input_inventories=self.input_inventories, isolation=self.isolation,
            manifest_sha256=sha(c.MANIFEST), identity_failed=self.identity_failed))

    def command(self, name, argv, timeout, env=None):
        record = super().command(name, argv, timeout, env)
        self.freeze_record(name)
        return record

    def freeze_record(self, name):
        for suffix in ('argv.json', 'json', 'stdout', 'stderr'):
            path = self.out / (name + '.' + suffix)
            require(str(path) not in self.frozen, '禁止覆盖原流identity')
            self.frozen[str(path)] = sha(path)

    def identity(self, label):
        # HEAD/tree不可变且工作树逐次干净即可证明同一源；完整12k文件表只保存一次。
        # 不在每个事件屏障重复归档1.5MiB ls-tree，旧runner行为不变。
        if self.before is None: return super().identity(label)
        self.checked(label + '-head', ['git', 'rev-parse', 'HEAD', 'HEAD^{tree}'], 20)
        actual = (self.out / (label + '-head.stdout')).read_text().splitlines()
        require(actual == [self.before['head'], self.before['tree']], '执行期间HEAD/tree改变')
        self.checked(label + '-status', ['git', 'status', '--porcelain=v1', '--untracked-files=all'], 20)
        require((self.out / (label + '-status.stdout')).read_bytes() == b'', '执行期间工作树改变')
        return self.before

    def identities(self):
        c.check_context(os.environ, c.parse(Path(os.environ['GITHUB_EVENT_PATH']).read_bytes()))
        c.qualified_oracles(self.data)
        self.before = self.identity('before')
        require(self.before['head'] == os.environ['GITHUB_SHA'], '实际HEAD不是本次事件')
        self.python, self.dotnet = Path(os.environ['PYTHON']).resolve(), Path(os.environ['DOTNET']).resolve()
        require(Path(sys.executable).resolve() == self.python and sys.version_info[:3] == (3, 12, 13)
                and sha(self.python) == self.data['cp_sha256'], '实际CP版本/解释器身份不符')
        self.frozen[str(c.MANIFEST)] = sha(c.MANIFEST)
        for name in self.data['sources'].keys() | self.data['protected'].keys():
            self.frozen[str(ROOT / name)] = sha(ROOT / name)
        tools = {'python': self.python, 'dotnet': self.dotnet,
                 'cargo': Path(shutil.which('cargo')).resolve(), 'rustc': Path(shutil.which('rustc')).resolve()}
        expected = {'dotnet': '10.0.403', 'rustc': 'rustc 1.99.1 (58cf8f9c3 2026-10-13)',
                    'cargo': 'cargo 1.99.1 (5f94df478 2026-08-27)'}
        for name, version in expected.items():
            self.checked(name + '-version', [tools[name], '--version'], 30)
            require((self.out / (name + '-version.stdout')).read_text().strip() == version, '工具版本不符')
        self.frozen.update({str(p): sha(p) for p in tools.values()})
        save(self.out / 'tools-before.json', {n: dict(path=str(p), sha256=sha(p)) for n, p in tools.items()})
        self.environment()

    def build_tools(self):
        # 与原Markdown相同pilot CLI；不新增default开关，也不借用其它job编译产物。
        self.checked('runtime-build', [self.dotnet, 'build', ROOT / 'runtime/RyuPy.Runtime/RyuPy.Runtime.csproj',
                     '-c', 'Release', '-nologo'], 600)
        self.checked('compiler-build', ['cargo', 'build', '--locked', '-p', 'ryupy-cli', '--features', raw.FEATURE], 1200)
        tools = self.out / 'tools'; tools.mkdir()
        self.cli, self.runtime = tools / 'ryupy', tools / 'RyuPy.Runtime.dll'
        shutil.copy2(ROOT / 'target/debug/ryupy', self.cli)
        shutil.copy2(ROOT / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll', self.runtime)
        self.preserve(tools, 'compiled-tools')
        self.environment()
        save(self.out / 'runtime-selection.json', dict(environment=self.env,
            runtime_path=str(self.runtime), runtime_sha256=sha(self.runtime), cli_sha256=sha(self.cli),
            cli_has_runtime_option=False, project_runner_runtime='原独立project_gate.py显式--runtime及run.json核验'))

    def prepare_sources(self):
        self.inputs_dir = self.inputs(self.python)
        for name in self.data['sources']:
            target = self.inputs_dir / name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(ROOT / name, target)
        save(self.inputs_dir / 'metadata-request.json', self.data['metadata_request'])
        save(self.inputs_dir / 'markdown-resources.json', c.resource_request(self.data))
        for name, digest in self.data['business_sources'].items():
            require(sha(self.inputs_dir / name) == digest, '实际Markdown/stdlib/egg-info源不符：' + name)
        rows = []
        for module, relative in self.data['markdown_modules'].items():
            path = self.inputs_dir / relative
            require(path.is_file(), '27模块源缺失：' + module)
            package = module if path.name == '__init__.py' else module.rpartition('.')[0]
            rows.append(dict(module=module, source=relative, sha256=sha(path), package=package,
                             location='entry' if module == '__main__' else 'ExecutableRelative',
                             package_parent=path.name == '__init__.py'))
        save(self.out / 'markdown-source-plan.json', rows)
        self.freeze_inputs(self.inputs_dir)
        self.preserve(self.inputs_dir, 'inputs')

    def run_product(self, name, argv, env=None, cp=False):
        self.barrier(name + '-before')
        require(name not in self.labels, '禁止重复目标执行')
        self.labels.add(name)
        cwd = self.out / 'cwd' / name; cwd.mkdir(parents=True)
        save(self.out / (name + '-environment.json'), env or self.env)
        record = raw.capture(self.out / name, argv, cwd, 30, env or self.env,
                             limit=128 * 1024 if cp else 4 * 1024**2,
                             memory_limit=512 * 1024**2 if cp else None)
        self.freeze_record(name)
        self.barrier(name + '-after')
        return record

    def golden(self, name):
        return (ROOT / self.data['oracles'][name]['stdout_file']).read_bytes()

    def cp(self, name, source, args=(), business=False):
        argv = [self.python, '-S', '-B', source, *args]
        env = dict(PATH='/usr/bin:/bin', LANG='C.UTF-8', LC_ALL='C.UTF-8', TZ='UTC',
                   PYTHONNOUSERSITE='1', PYTHONDONTWRITEBYTECODE='1', PYTHONHASHSEED='0')
        if business: env['PYTHONPATH'] = os.pathsep.join(map(str, [self.inputs_dir / 'stdlib', self.inputs_dir / 'markdown']))
        else: argv.insert(1, '-I')
        self.run_product(name + '-cp-run', argv, env, cp=True)
        return c.positive(self.out / (name + '-cp-run'), argv, self.golden(name))

    def compile(self, name, case):
        target = self.root_out / 'products' / name
        argv = c.build_argv(self, case, target)
        self.barrier(name + '-before-compile')
        self.command(name + '-compile-command', argv, 180, self.env)
        self.barrier(name + '-after-compile')
        c.verify_command(self.out / (name + '-compile-command'), argv)
        self.audit(lambda: c.verify_plan(target / 'module-environment.json', case))
        self.audit(lambda: require(sha(target / 'RyuPy.Runtime.dll') == sha(self.runtime), '实际JIT runtime换源'))
        if case.get('business'):
            base.product.contract.audit_pe(self, name, target / (Path(case['source']).stem + '.dll'),
                target / 'distribution-metadata.json', self.inputs_dir / 'metadata-request.json')
        self.preserve(target, name + '-pe')

    def jit(self, name, graph, source, args=(), golden=None):
        argv = [self.dotnet, self.root_out / 'products' / graph / (Path(source).stem + '.dll'), *args]
        self.run_product(name + '-jit-run', argv)
        return c.positive(self.out / (name + '-jit-run'), argv, self.golden(name) if golden is None else golden)

    def run_positive(self, name, argv, golden, env=None):
        # Native helper只获唯一all合同，不接受任意行或空expected。
        require(name in ('core-all-original', 'core-all-relocated') and golden == self.golden('all'), 'Native合同换源')
        self.run_product(name, argv, env)
        return c.positive(self.out / name, argv, golden)

    def types(self, source):
        argv = [self.cli, 'dump', source, '--emit', 'types']
        self.barrier('original-before-types')
        self.command('original-types-command', argv, 120, self.env)
        self.barrier('original-after-types')
        c.verify_command(self.out / 'original-types-command', argv)
        require((self.out / 'original-types-command.stdout').stat().st_size > 0, '真实typed输出为空')

    def rejected_class(self, kind):
        source = self.inputs_dir / (c.FIXTURE + 'subclass_' + kind + '.py')
        target = self.out / 'rejected' / kind
        argv = [self.cli, 'build', source, '--diagnostic-stages', '--out-dir', target]
        self.barrier(kind + '-before-reject')
        self.command('reject-' + kind, argv, 120, self.env)
        self.barrier(kind + '-after-reject')
        detail = c.subclass_rejection(self.out / ('reject-' + kind), argv, source, self.data['subclass_messages'][kind])
        require(not list(target.rglob('*.dll')), '新族派生拒绝却生成PE')
        return detail

    def base_controls(self):
        # 普通类按冻结CP对齐；原生对象拒绝只核明确NI，绝不运行NI的CP。
        ordinary = dict(source=c.FIXTURE + 'ordinary_bases.py')
        self.phase('ordinary-bases-cp', lambda: self.cp('ordinary-bases', self.inputs_dir / ordinary['source']))
        if self.phase('ordinary-bases-compile', lambda: self.compile('ordinary-bases', ordinary)):
            self.phase('ordinary-bases-jit', lambda: self.jit('ordinary-bases', 'ordinary-bases', ordinary['source']))
        mutable = dict(source=c.FIXTURE + 'mutable_parent.py')
        self.phase('mutable-parent-cp', lambda: self.cp('mutable-parent', self.inputs_dir / mutable['source']))
        if self.phase('mutable-parent-compile', lambda: self.compile('mutable-parent', mutable)):
            self.phase('mutable-parent-jit', lambda: self.jit('mutable-parent', 'mutable-parent', mutable['source']))
        runtime = dict(source=c.FIXTURE + 'runtime_bases.py')
        if self.phase('runtime-bases-compile', lambda: self.compile('runtime-bases', runtime)):
            self.phase('runtime-bases-jit', lambda: self.jit('runtime-bases', 'runtime-bases', runtime['source'],
                golden=self.data['runtime_bases_stdout'].encode()))

    def business(self):
        # 首个新业务失败即停本JIT链；仍保独立core Native与一次完整AOT边界。
        for name, source in [('markdown-full', 'markdown_sample.py'), ('markdown-html', c.FIXTURE + 'markdown_html.py')]:
            case = dict(source=source, business=True, plan=self.data['markdown_plan'])
            if not self.phase(name + '-cp', lambda: self.cp(name, self.inputs_dir / source, business=True)): return
            if not self.phase(name + '-compile', lambda: self.compile(name, case)): return
            if not self.phase(name + '-jit', lambda: self.jit(name, name, source)): return

    def markdown_aot(self):
        case = dict(source='markdown_sample.py', business=True)
        target = self.out / 'markdown-aot-build'
        argv = c.build_argv(self, case, target, True)
        self.barrier('markdown-aot-before')
        self.command('markdown-aot-command', argv, 1200, self.env)
        # 与core共用有界原dist归档；非零/超时、布局或后置身份失败也保已生成原物。
        candidates = preserve_before_validation(self, target, 'markdown_sample')
        self.barrier('markdown-aot-after')
        # 旧missing-resource命令禁止再用；新失败也不得改成通过。
        record, streams = c.verify_process(self.out / 'markdown-aot-command', argv, exits=(0, 1))
        save(self.out / 'markdown-aot-boundary.json', dict(exit=record['exit'],
            resource_anchors=26, request_sha256=sha(self.inputs_dir / 'markdown-resources.json'),
            known_E0100=b'[E0100]' in streams['stderr'], native_execution='not-run',
            status='failed' if record['exit'] else 'unexpected-build-success-unvalidated'))
        require(record['exit'] == 0, '完整Markdown AOT真实失败保红，见原诊断；不当作预期通过')
        layout = old.reported_aot(streams['stderr'], target, 'markdown_sample', 'linux-x64')
        self.audit(lambda: require(len(candidates) == 1 and candidates[0].samefile(layout.dist),
                                   '真实reported dist不等于已归档唯一产物'))
        self.preserve(layout.dist, 'markdown-native-dist')
        base.product.contract.audit_pe(self, 'markdown-native', layout.managed,
            layout.managed.parent / 'distribution-metadata.json', self.inputs_dir / 'metadata-request.json')
        manifest = c.verify_markdown_native(self, layout)
        save(self.out / 'markdown-native-manifest.json', manifest)
        saved = self.out / 'markdown-native-managed'; saved.mkdir()
        for path in layout.managed.parent.iterdir():
            if path.is_file(): shutil.copy2(path, saved / path.name)
        self.preserve(saved, 'markdown-native-managed')
        moved = self.out / 'relocated' / 'markdown'; moved.parent.mkdir(exist_ok=True)
        binary = base.product.contract.relocate(layout.dist, layout.native, moved)
        require(c.verify_markdown_native(self, layout, moved, binary) == manifest, '搬移manifest改变')
        self.preserve(moved, 'markdown-native-relocated')
        runs = {}
        env = {'PATH': '/usr/bin:/bin', 'LC_ALL': 'C.UTF-8', 'TZ': 'UTC'}
        with c.isolated(self, [self.inputs_dir], 'markdown-all-inputs'):
            for label, program in [('original', layout.native), ('relocated', binary)]:
                name = 'markdown-native-' + label
                try:
                    self.run_product(name, [program], env)
                    c.positive(self.out / name, [program], self.golden('markdown-full'))
                    runs[label] = 'passed'
                except c.IdentityError: raise
                except Exception as error: runs[label] = 'failed: ' + str(error)
                save(self.out / 'markdown-native-runs.json', runs)
        require(runs == {'original': 'passed', 'relocated': 'passed'}, '完整Markdown Native部分运行失败')
        save(self.out / 'markdown-aot-boundary.json', dict(exit=0, resource_anchors=26,
            request_sha256=sha(self.inputs_dir / 'markdown-resources.json'),
            status='passed', native_execution='original-and-relocated', runs=runs))
        return dict(native_runs=runs, manifest_validated=True, cp_equivalent=True)

    def execute(self):
        try:
            if not self.phase('identity', lambda: self.audit(self.identities)): return 1
            if not self.phase('tools', self.build_tools): return 1
            if not self.phase('sourceprep', lambda: self.audit(self.prepare_sources)): return 1
            original = dict(source=c.ORIGINAL)
            self.phase('original-types', lambda: self.types(self.inputs_dir / c.ORIGINAL))
            self.phase('original-cp', lambda: self.cp('original', self.inputs_dir / c.ORIGINAL))
            if self.phase('original-compile', lambda: self.compile('original', original)):
                self.phase('original-jit', lambda: self.jit('original', 'original', c.ORIGINAL))
            core = dict(id='core-all', source=c.FIXTURE + 'core.py', include=[],
                        modules={'__main__': c.FIXTURE + 'core.py'}, argv=['all'], mode='cp', stdout=self.golden('all').decode())
            compiled = self.phase('core-compile', lambda: self.compile('core', core))
            for group in c.GROUPS:
                self.phase(group + '-cp', lambda group=group: self.cp(group, self.inputs_dir / core['source'], [group]))
                if compiled: self.phase(group + '-jit', lambda group=group: self.jit(group, 'core', core['source'], [group]))
            boundary = dict(source=c.FIXTURE + 'boundaries.py')
            if self.phase('boundaries-compile', lambda: self.compile('boundaries', boundary)):
                self.phase('boundaries-jit', lambda: self.jit('boundaries', 'boundaries', boundary['source'],
                    golden=self.data['boundary_stdout'].encode()))
            for kind in self.data['subclass_messages']:
                self.phase('subclass-' + kind, lambda kind=kind: self.rejected_class(kind))
            self.base_controls()
            self.business()
            self.persist()
            if c.native_eligible(self.result['stages']): self.phase('core-native', lambda: native(self, [core], contract=c))
            else: self.block('core-native', '同源inputs/compile/all CP前置缺失，非JIT语义红')
            self.phase('markdown-aot', self.markdown_aot)
        except Exception as error:
            self.result.update(passed=False, error=str(error), traceback=traceback.format_exc())
        finally:
            try: self.barrier('final')
            except Exception as error: self.invalidate(error)
            self.result['passed'] = not self.identity_failed and all(x['passed'] for x in self.result['stages'].values())
            self.persist()
        return 0 if self.result['passed'] else 1


if __name__ == '__main__':
    raise SystemExit(Runner().execute())
