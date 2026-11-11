#!/usr/bin/env python3
"""class工厂同HEAD两阶段验收；未冻结硬停、JIT红灯不被Native采集改绿。"""
import argparse
import json
import os
from pathlib import Path
import shutil
import sys
import traceback
import lexical_class_factory_contract as c
from textwrap_dedent_native import preserve_before_validation

ROOT, require, sha, save = c.ROOT, c.require, c.sha, c.save


def stages(data):
    return ['identity', 'csharp-build', 'csharp-tests', 'rust-tests', 'tools', 'sourceprep'] + [
        case['id'] + '-' + phase for case in data['cases'] for phase in ('cp', 'jit')] + [
        case['id'] + '-native' for case in data['cases'] if case['native'] != 'none']


def native_ready(result, case):
    return all(result['stages'][name]['passed'] for name in ('identity', 'tools', 'sourceprep', case['id'] + '-cp'))


class Runner(c.metadata.Runner):
    def __init__(self, resume=False):
        # 必须先核冻结，不能先创建目录、下载、构建或启动业务进程。
        self.data = c.load_manifest()
        self.out = ROOT / 'target/lexical-class-factory-evidence'
        self.out.mkdir(parents=True, exist_ok=resume)
        self.inputs_dir = self.out / 'inputs'
        self.labels, self.frozen, self.input_inventories, self.isolation = set(), {}, {}, {}
        self.before, self.identity_failed = None, False
        self.python = self.dotnet = self.cli = self.runtime = None
        self.result = {'passed': False, 'default_project_delta': 0, 'retries': 0,
            'scope': '单元1有限普通class；私名改写/MI/native族未支持，toml增量0',
            'pending_only': [{'source': x['source'], 'reason': x['reason'], 'status': 'not-run', 'passed': False}
                             for x in self.data['pending_only']],
            'case_counts': {kind: sum(x['kind'] == kind for x in self.data['cases'])
                            for kind in ('positive', 'cp_error', 'ni')},
            'stages': {name: {'passed': False, 'status': 'not-run'} for name in stages(self.data)}}

    def persist(self):
        save(self.out / 'state.json', {'schema_version': 1, 'manifest_sha256': sha(c.MANIFEST),
            'before': self.before, 'frozen': self.frozen, 'input_inventories': self.input_inventories,
            'isolation': self.isolation, 'identity_failed': self.identity_failed, 'result': self.result,
            'labels': sorted(self.labels), 'environment': getattr(self, 'env', None),
            'tools': {k: str(getattr(self, k)) if getattr(self, k) else None for k in ('python', 'dotnet', 'cli', 'runtime')}})
        save(self.out / 'result.json', self.result)

    def identities(self):
        c.check_context(os.environ, c.parse(Path(os.environ['GITHUB_EVENT_PATH']).read_bytes()))
        c.load_manifest()
        self.before = self.identity('before')
        require(self.before['head'] == os.environ['GITHUB_SHA'], '候选HEAD不同于事件')
        save(self.out / 'source-identity.json', self.before)
        archive = self.out / 'candidate-source.tar'
        self.checked('source-archive', ['git', 'archive', '--format=tar', '--output=' + str(archive), 'HEAD'], 60)
        self.frozen.update({str(archive): sha(archive), str(c.MANIFEST): sha(c.MANIFEST)})
        self.python, self.dotnet = Path(os.environ['PYTHON']).resolve(), Path(os.environ['DOTNET']).resolve()
        require(sys.version_info[:3] == (3, 12, 13) and Path(sys.executable).resolve() == self.python
                and sha(self.python) == c.old.shared.PYTHON_SHA, '固定CP身份不符')
        tools = {'python': self.python}
        for name, expected in {'dotnet': '10.0.403', 'rustc': 'rustc 1.99.1 (58cf8f9c3 2026-10-13)',
                               'cargo': 'cargo 1.99.1 (5f94df478 2026-08-27)'}.items():
            path = self.dotnet if name == 'dotnet' else Path(shutil.which(name) or '').resolve()
            self.checked(name + '-version', [path, '--version'], 30)
            require((self.out / (name + '-version.stdout')).read_text().strip() == expected, '工具版本不符')
            tools[name] = path
        self.frozen.update({str(p): sha(p) for p in tools.values()})
        save(self.out / 'tool-identity.json', {n: {'path': str(p), 'sha256': sha(p)} for n, p in tools.items()})
        self.environment()

    def load_state(self):
        try:
            raw = (self.out / 'state.json').read_bytes()
            (self.out / 'resume-state.raw.json').write_bytes(raw)
            state = c.parse(raw)
            require(set(state) == {'schema_version', 'manifest_sha256', 'before', 'frozen', 'input_inventories',
                'isolation', 'identity_failed', 'result', 'labels', 'environment', 'tools'}, 'state字段不完整')
            require(state['schema_version'] == 1 and state['manifest_sha256'] == sha(c.MANIFEST), 'state身份改变')
            candidate = state['result']; rows = candidate['stages']
            require(set(rows) == set(stages(self.data)) and all(type(r.get('passed')) is bool
                and r.get('status') in ('not-run', 'started', 'blocked', 'passed', 'failed', 'invalidated-identity', 'unsupported', 'unaligned')
                and r['passed'] == (r['status'] == 'passed') for r in rows.values()), '阶段集合/状态矛盾')
            require(candidate['default_project_delta'] == candidate['retries'] == 0 and type(candidate['passed']) is bool
                    and (not candidate['passed'] or all(r['passed'] for r in rows.values())), '旧总结果矛盾')
            require(candidate.get('pending_only') == self.result['pending_only']
                    and candidate.get('case_counts') == self.result['case_counts'], 'pending/分类记账改变')
            self.result = candidate
            self.before, self.frozen = state['before'], state['frozen']
            self.input_inventories, self.isolation = state['input_inventories'], state['isolation']
            self.labels, self.identity_failed = set(state['labels']), state['identity_failed']
            require(not self.identity_failed and not self.isolation, '旧身份失效/隔离未恢复')
            expected = {'python': Path(os.environ['PYTHON']).resolve(), 'dotnet': Path(os.environ['DOTNET']).resolve(),
                        'cli': self.out / 'tools/ryupy', 'runtime': self.out / 'tools/RyuPy.Runtime.dll'}
            require(state['tools'] == {n: str(p) for n, p in expected.items()}, '工具路径替换')
            for n, p in expected.items(): setattr(self, n, p)
            self.environment(); require(self.env == state['environment'], '环境改变')
            require(set(self.input_inventories) == {str(self.inputs_dir)}, '完整输入清单缺失')
            c.load_manifest()
            c.check_context(os.environ, c.parse(Path(os.environ['GITHUB_EVENT_PATH']).read_bytes()))
            self.barrier('native-resume')
        except Exception as error:
            self.fail_identity(error)

    def prepare_sources(self):
        for source in self.data['sources']:
            target = self.inputs_dir / source
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(ROOT / source, target)
            require(sha(target) == self.data['sources'][source], '复制输入身份改变')
        self.freeze_inputs(self.inputs_dir)
        save(self.out / 'isolation-scope.json', {'removed_at_execution': str(self.inputs_dir),
            'original_repo_sources_retained': True, 'sdk_uninstalled': False,
            'claim': '仅编译输入根隔离，最小PATH；不声称机器/容器无SDK'})

    def managed_tests(self):
        target = self.out / 'trx'
        self.checked('managed-tests', [self.dotnet, 'test', ROOT / 'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj',
            '-c', 'Release', '--no-build', '--no-restore', '--filter',
            '|'.join('FullyQualifiedName=' + n for n in self.data['facts']), '--logger',
            'trx;LogFileName=result.trx', '--results-directory', target], 240)
        self.preserve(target, 'trx')
        return c.verify_trx(target / 'result.trx', self.data['facts'])

    def rust_tests(self):
        for index, row in enumerate(self.data['rust_tests']):
            name = 'rust-' + str(index)
            self.checked(name, ['cargo', 'test', '--locked', '-p', row['package'], '--lib', row['name'], '--', '--exact', '--nocapture'], 300)
            raw = (self.out / (name + '.stdout')).read_text()
            require('test ' + row['name'] + ' ... ok' in raw and '1 passed; 0 failed; 0 ignored;' in raw,
                    'Rust精确测试零执行/跳过/结果不符')

    def oracle(self, case):
        label = case['id'] + '-cp-run'
        record = self.command(label, [self.python, '-S', '-B', self.inputs_dir / case['source']], 30,
            dict(PATH='/usr/bin:/bin', LC_ALL='C.UTF-8', TZ='UTC', PYTHONNOUSERSITE='1',
                 PYTHONHASHSEED='0', PYTHONDONTWRITEBYTECODE='1'))
        return c.verify_output(record, self.out / label, case['cp'],
            {n: self.inputs_dir / n for n in case['modules'].values()})

    def jit(self, case):
        label = case['id']; target = self.out / 'products' / label
        self.barrier(label + '-before-compile')
        record = self.command(label + '-compile', c.build_argv(self, case, target), 90, self.env)
        # 已生成的PE/诊断先留证，随后才判成功；失败不删partial产品。
        if target.exists() and any(target.iterdir()): self.preserve(target, label + '-partial-product')
        self.barrier(label + '-after-compile')
        if case['product_stage'] == 'compile':
            require(not (target / (Path(case['source']).stem + '.dll')).exists(), '静态拒绝不得生成成功入口PE')
            return c.verify_output(record, self.out / (label + '-compile'), case['product'], refusal=True)
        require(c.complete(record), '编译失败，JIT未执行')
        pe = target / (Path(case['source']).stem + '.dll')
        self.audit(lambda: require(pe.is_file() and sha(target / 'RyuPy.Runtime.dll') == sha(self.runtime), 'PE/runtime身份不符'))
        c.verify_plan(target / 'module-environment.json', case)
        record = self.run_product(label + '-jit-run', [self.dotnet, pe], self.env)
        prefix = self.out / (label + '-jit-run')
        paths = {n: self.inputs_dir / n for n in case['modules'].values()}
        if case['kind'] == 'cp_error':
            comparison = c.compare_cp_error(record, prefix, case['cp'], paths)
            save(self.out / (label + '-trace-comparison.json'), comparison)
            return {'unaligned': not comparison['aligned'], 'cp_equivalent': comparison['aligned'],
                    'scope': '完整trace比较，不删类帧或行号'}
        return c.verify_output(record, prefix, case['product'], paths, refusal=case['kind'] == 'ni')

    def native(self, case):
        from textwrap_dedent_native import native
        if case['native'] == 'positive':
            # 现有helper只服务exit0正门；不假装支持错误退出合同。
            adapted = dict(case, argv=[], stdout=case['cp']['stdout'], mode='cp')
            try:
                return native(self, [adapted], contract=c)
            except c.IdentityError:
                raise
            except Exception:
                prefix = self.out / 'native-domains' / case['id'] / 'native-publish'
                if case['native_refusal'] is None or not prefix.with_suffix('.json').exists(): raise
                record = c.parse(prefix.with_suffix('.json').read_bytes())
                c.verify_output(record, prefix, case['native_refusal'], refusal=True)
                return {'unsupported': True, 'reason': case['native_refusal']['diagnostic']}
        previous = self.out
        self.out = previous / 'native-domains' / case['id']; self.out.mkdir(parents=True)
        try:
            require(shutil.disk_usage(self.out).free >= 2 * 1024**3, 'Native不足2GiB，不清理')
            target = self.out / 'native-build'
            self.barrier('negative-before-publish')
            record = self.command('negative-publish', c.build_argv(self, case, target, True), 300, self.env)
            preserve_before_validation(self, target, Path(case['source']).stem)
            self.barrier('negative-after-publish')
            c.reject_native_success_products(target, Path(case['source']).stem)
            c.verify_output(record, self.out / 'negative-publish', case['native_refusal'], refusal=True)
            return {'expected_capability_refusal': True, 'cp_equivalent': False}
        finally:
            self.out = previous

    def execute(self, phase):
        try:
            if phase == 'jit':
                if not self.phase('identity', lambda: self.audit(self.identities)): return 1
                if not self.phase('csharp-build', lambda: self.managed(build_only=True)): return 1
                self.phase('csharp-tests', self.managed_tests)
                self.phase('rust-tests', self.rust_tests)
                if not self.phase('tools', self.build_tools): return 1
                if not self.phase('sourceprep', self.prepare_sources): return 1
                for case in self.data['cases']:
                    self.phase(case['id'] + '-cp', lambda case=case: self.oracle(case))
                    if self.result['stages'][case['id'] + '-cp']['passed']:
                        self.phase(case['id'] + '-jit', lambda case=case: self.jit(case))
                        row = self.result['stages'][case['id'] + '-jit']
                        if row.get('unaligned'): row.update(passed=False, status='unaligned')
                    else: self.block(case['id'] + '-jit', '同源CP合同失败')
            else:
                self.load_state()
                for case in self.data['cases']:
                    if case['native'] == 'none': continue
                    name = case['id'] + '-native'
                    if not native_ready(self.result, case): self.block(name, '同源身份/工具/输入/CP不足'); continue
                    self.phase(name, lambda case=case: self.native(case))
                    row = self.result['stages'][name]
                    if row.get('unsupported'): row.update(passed=False, status='unsupported')
            self.result['passed'] = all(r['passed'] for r in self.result['stages'].values())
        except c.IdentityError as error:
            self.invalidate(error)
        except Exception as error:
            self.result.update(passed=False, error=str(error), traceback=traceback.format_exc())
        finally:
            try: self.barrier(phase + '-final')
            except Exception as error: self.invalidate(error)
            self.persist()
        considered = [r for n, r in self.result['stages'].items() if phase == 'native' or not n.endswith('-native')]
        return 0 if not self.identity_failed and 'error' not in self.result and all(r['passed'] for r in considered) else 1


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--phase', choices=['jit', 'native'], required=True)
    args = parser.parse_args()
    raise SystemExit(Runner(args.phase == 'native').execute(args.phase))
