#!/usr/bin/env python3
"""新KeyError仅替换合同/普通CLI；同源恢复、失败分账、Native撤输入/搬移均复用既有实现。"""
import argparse
from pathlib import Path
import re
import shutil
import decimal_percent_validation as previous
import keyerror_construction_contract as c
from textwrap_dedent_native import native

ROOT, require, sha, save = c.ROOT, c.require, c.sha, previous.save


class Runner(previous.Runner):
    contract, evidence_name, test_prefix = c, 'keyerror-construction-evidence', 'keyerror'

    def __init__(self, resume=False):
        super().__init__(resume)
        for name in ('rust-mir', 'rust-codegen', 'pe-reader-build', *(g['id'] + '-structure' for g in self.data['graphs'])):
            self.result['stages'][name] = dict(passed=False, status='not-run')
        self.reader = self.root_out / 'pe-reader/PeAudit.dll'
        self.result['expected_product_processes'] = self.data['expected_product_processes']
        self.result['cli_feature'] = 'default；无pilot，与同HEAD原percent的pilot CLI分开构建和绑定'

    def compiler_argv(self):
        return ['cargo', 'build', '--locked', '-p', 'ryupy-cli']

    def build_tools(self):
        self.checked('compiler-build', self.compiler_argv(), 1200)
        target = self.out / 'tools'; target.mkdir()
        self.cli, self.runtime = target / 'ryupy', target / 'RyuPy.Runtime.dll'
        shutil.copy2(ROOT / 'target/debug/ryupy', self.cli)
        shutil.copy2(self.out / 'managed/RyuPy.Runtime.dll', self.runtime)
        self.preserve(target, 'compiled-tools'); self.environment()

    def cp_argv(self, row, source):
        # 同目录模块是产品源码的一部分；不以-I删除脚本搜索根导致伪CP失败。
        return [self.python, '-B', source, *row['argv']]

    def build_argv(self, case, target, aot=False): return c.build_argv(self, case, target, aot)
    def verify_plan(self, path, case, aot=False): return c.verify_plan(path, case, aot)

    def rust_argv(self, group):
        crate = 'ryupy-mir' if group == 'mir' else 'ryupy-codegen-cil'
        return ['cargo', 'test', '--locked', '-p', crate, 'builtin_lookup_tests', '--', '--nocapture']

    def rust_result(self, group):
        name = 'rust-' + group
        c.verify_command(self.out / name, self.rust_argv(group))
        lines = (self.out / (name + '.stdout')).read_text().splitlines()
        tests = re.findall(r'^test (builtin_lookup_tests::\w+) \.\.\. ok$', '\n'.join(lines), re.M)
        require(sorted(tests) == sorted(self.data['rust'][group]), 'Rust具名结果缺失/重复/失败')
        require(any(line.startswith('test result: ok. ' + str(len(tests)) + ' passed; 0 failed; 0 ignored;') for line in lines), 'Rust汇总不是精确执行')
        return dict(passed=True, status='passed', tests=tests)

    def reader_argv(self):
        return [self.dotnet, 'build', ROOT / 'tests/validation/foreach_pe_audit/PeAudit.csproj',
                '-c', 'Release', '-o', self.reader.parent, '-nologo']

    def reader_result(self):
        c.verify_command(self.out / 'pe-reader-build', self.reader_argv())
        require(self.reader.is_file(), 'PE reader未生成')
        self.preserve(self.reader.parent, 'pe-reader-tools')
        return dict(passed=True, status='passed', sha256=sha(self.reader))

    def prepare_sources(self):
        super().prepare_sources()
        for group in ('mir', 'codegen'):
            def run(group=group):
                self.command('rust-' + group, self.rust_argv(group), 600, self.env)
                return self.rust_result(group)
            self.phase('rust-' + group, run)
        def reader():
            self.command('pe-reader-build', self.reader_argv(), 180, self.env)
            return self.reader_result()
        self.phase('pe-reader-build', reader)

    def structure(self, case, pe, plan, label, saved=False):
        argv = [self.dotnet, self.reader, pe]
        if not saved: self.command(label, argv, 60, self.env)
        c.verify_command(self.out / label, argv)
        data = c.parse((self.out / (label + '.stdout')).read_bytes())
        detail = c.pe_calls(data, pe, plan, case)
        save(self.out / (label + '-calls.json'), detail)
        return detail

    def compile_graph(self, graph):
        super().compile_graph(graph)
        case, target = c.cases(graph)[0], self.graph_target(graph)
        if self.result['stages']['pe-reader-build']['passed']:
            self.phase(graph['id'] + '-structure', lambda: self.structure(case, target / (Path(case['source']).stem + '.dll'),
                self.verify_plan(target / 'module-environment.json', case), graph['id'] + '-pe'))
        else: self.block(graph['id'] + '-structure', 'PE reader未就绪，产品JIT仍独立执行')

    def native_graph(self, graph):
        require(self.result['stages']['pe-reader-build']['passed'], '真实PE审计工具缺失，不能放宽Native结构门')
        detail = native(self, c.cases(graph), contract=c)
        detail.pop('expected_ni', None)
        return detail

    def resume_expectations(self):
        expected, rows, required = super().resume_expectations()
        def files(label): required.update(label + '.' + suffix for suffix in ('stdout', 'stderr', 'json', 'argv.json'))
        for group in ('mir', 'codegen'):
            files('rust-' + group)
            self.phase('rust-' + group, lambda group=group: self.rust_result(group))
        files('pe-reader-build')
        reader = self.phase('pe-reader-build', self.reader_result)
        if reader:
            required.add(str(self.reader.relative_to(self.root_out)))
            for graph in self.data['graphs']:
                name = graph['id']; case = c.cases(graph)[0]; target = self.graph_target(graph)
                if self.result['stages'][name + '-compile']['passed']:
                    files(name + '-pe')
                    self.phase(name + '-structure', lambda case=case, target=target, name=name: self.structure(case,
                        target / (Path(case['source']).stem + '.dll'), self.verify_plan(target / 'module-environment.json', case), name + '-pe', saved=True))
                else: self.block(name + '-structure', '本图原compile未通过')
        else:
            for graph in self.data['graphs']: self.block(graph['id'] + '-structure', '原PE reader构建未通过')
        return expected, rows, required


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--phase', choices=['jit', 'native'], required=True)
    args = parser.parse_args()
    raise SystemExit(Runner(resume=args.phase == 'native').execute(args.phase))
