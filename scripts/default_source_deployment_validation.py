#!/usr/bin/env python3
"""默认来源部署同源门：复用固定工具及原流采集，不注入catalog搜索根，不晋升原A。"""
import json
import os
from pathlib import Path
import shutil
import sys
import traceback
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'tests/validation'))
import markdown_closure_probe as shared
import default_source_deployment_validation_probes as probes
import default_source_deployment_validation_plans as plan_guard

ROOT, old = shared.REPO, shared.old
require, sha, save, snapshot = shared.require, shared.sha, shared.save, shared.snapshot
TITLE = 'validation: default-source-deployment-001'
PARSER_SHA = '0e0f4f71a4949982d30a29e3a3181b06a8c38a3601b68cc9c88b06a85c268544'
PARSER_OUTPUT = b"parser_source_isolation_v1 ['probe>'] ['probe']\n"
PARSER_OUTPUT_SHA = '5911218708498540658da693765bf9a339daaa411b599da072ac9217ead6b812'
PARSER_SOURCE = r"""# ryupy: pending
# 独立合同 parser_source_isolation_v1；不替代完整原 A。
import html.parser as original
import importlib.util as util
import re

original_class = original.HTMLParser
original_pi = original_class.parse_pi
original_goahead = original_class.goahead
original_globals = original.__dict__
original_close = original.piclose
assert original_pi.__globals__ is original_globals
assert original_goahead.__globals__ is original_globals

spec = util.find_spec('html.parser')
clone = util.module_from_spec(spec)
spec.loader.exec_module(clone)
assert clone is not original
assert clone.__dict__ is not original_globals
assert clone.HTMLParser is not original_class
assert clone.HTMLParser.parse_pi is not original_pi
assert clone.HTMLParser.goahead is not original_goahead
assert clone.HTMLParser.parse_pi.__globals__ is clone.__dict__
assert clone.HTMLParser.goahead.__globals__ is clone.__dict__
clone.piclose = re.compile(r'\?>')
assert clone.piclose is not original_close

class CopyParser(clone.HTMLParser):
    def __init__(self):
        super().__init__(convert_charrefs=False)
        self.events = []
    def handle_pi(self, text):
        self.events.append(text)

class OriginalParser(original.HTMLParser):
    def __init__(self):
        super().__init__(convert_charrefs=False)
        self.events = []
    def handle_pi(self, text):
        self.events.append(text)

copy_parser = CopyParser()
original_parser = OriginalParser()
copy_parser.feed('<?probe>')
original_parser.feed('<?probe>')
assert copy_parser.events == []
assert original_parser.events == ['probe']
copy_parser.feed('?>')
original_parser.feed('?>')
assert copy_parser.events == ['probe>']
assert original_parser.events == ['probe']
assert original.HTMLParser is original_class
assert original.HTMLParser.parse_pi is original_pi
assert original.HTMLParser.goahead is original_goahead
assert original.__dict__ is original_globals
assert original_pi.__globals__ is original_globals
assert original_goahead.__globals__ is original_globals
assert clone.HTMLParser.parse_pi.__globals__ is clone.__dict__
assert clone.HTMLParser.goahead.__globals__ is clone.__dict__
assert original.piclose is original_close
assert original.piclose.pattern == '>'
assert clone.piclose.pattern == r'\?>'
print('parser_source_isolation_v1', copy_parser.events, original_parser.events)
"""


def check_context(env, event):
    replacement = dict(event, head_commit=dict(event.get('head_commit') or {}, message=shared.TITLE))
    shared.check_context(env, replacement)
    require(event.get('head_commit', {}).get('message') == TITLE, '默认部署标题不符')


def command_plan(cli, dotnet, inputs, products):
    """默认业务策略仅开启既有诊断观察；唯一项目根为Markdown，不注入catalog。"""
    result = {}
    for name, stem in [('parser', 'parser_isolation'), ('markdown', 'markdown_sample')]:
        target = products / name
        args = [cli, 'build', inputs / (stem + '.py'), '--out-dir', target, '--diagnostic-stages']
        if name == 'markdown': args += ['-I', inputs / 'markdown']
        result[name] = {'compile': list(map(str, args)),
                        'jit': list(map(str, [dotnet, target / (stem + '.dll')]))}
    return result


def run_independent(names, action, result, persist):
    """业务失败不中断其它新保护；共享身份损坏仍硬停，绝不当作业务NI。"""
    for name in names:
        row = result['stages'][name]
        row.update(status='started', passed=False); persist()
        try:
            action(name)
            row.update(status='passed', passed=True)
        except shared.IdentityError:
            row.update(status='invalidated-identity', passed=False); persist()
            raise
        except Exception as error:
            row.update(status='failed', passed=False, error=str(error))
        persist()
    return all(row['passed'] for row in result['stages'].values())


def verify_audit(output, inputs, name):
    """核实际编译策略、最终部署路径字节与完整metadata；不冒称运行期冷身份证明。"""
    plan = json.loads((output / 'module-environment.json').read_text())
    require(plan['compiled_feature'] is True and plan['session_policy'] == 'ordinary-dynamic-module-pilot',
            '实际session_policy/编译feature不同于固定20矩阵')
    plan_guard.audit_product_plan(output, plan, inputs, name)
    deployed = {}
    for relative, digest in old.STDLIB.items():
        matches = list((output / 'compiler-sources').glob('*/' + relative))
        require(len(matches) == 1 and not matches[0].is_symlink() and sha(matches[0]) == digest,
                '最终真实catalog路径/字节缺失或重复：' + relative)
        deployed[relative] = {'path': str(matches[0].resolve()), 'sha256': digest}
    detail = {'module_plan': plan, 'deployed_sources': deployed,
              'runtime_source_identity': '需生产冷激活/CodeSource测试独立验证；此处不以落盘代替'}
    if name == 'markdown':
        report = json.loads((output / 'distribution-metadata.json').read_text())
        require(report['mode'] == 'closed_snapshot' and len(report['distributions']) == 1,
                '自动完整metadata报告不符')
        distribution = report['distributions'][0]
        expected = snapshot(inputs / 'markdown/Markdown.egg-info')
        require(distribution['path'] == 'Markdown.egg-info' and not distribution['shadowed']
                and distribution['directories'] == []
                and {row['path']: {'bytes': row['size'], 'sha256': row['sha256']}
                     for row in distribution['files']} == expected, '自动metadata丢文件/改变原字节')
        detail['distribution_metadata'] = report
    return detail


class Runner(shared.Runner):
    def __init__(self):
        self.out = ROOT / 'target/default-source-deployment'
        self.out.mkdir(parents=True, exist_ok=False)
        self.root = self.out / 'frozen'
        self.before = self.input_identity = None
        self.frozen, self.labels, self.sequence = {}, set(), 0
        self.result = {'passed': False, 'default_project_delta': 0, 'retries': 0,
            'scope': '默认来源部署；独立parser不是原A builtin修复',
            'old_gates': '同HEAD独立jobs；本job失败不跳过旧门，整个workflow失败',
            'original_a': {'source_sha256': shared.A_SHA, 'status': 'retained-pending-not-run'},
            'default_markdown_aot': {'status': 'not-run', 'support': 'unsupported-not-promoted',
                'reason': '无同配置冻结oracle；不新增裸默认AOT窗口，旧AOT保护独立运行'},
            'stages': {name: {'passed': False, 'status': 'not-run'}
                       for name in ('parser', 'markdown')},
            'production_probes': {name: {'passed': False, 'status': 'not-run'} for name in probes.STAGES}}
        self.persist()

    def persist(self):
        save(self.out / 'result.json', self.result)

    def prepare_inputs(self):
        manifest = shared.unpack(shared.BUNDLE, self.root)
        inputs = self.root / 'inputs'
        require(sha(ROOT / shared.A_PATH) == shared.A_SHA, '原A改变')
        entry = ROOT / 'tests/projects/entries/markdown_sample.py'
        require(sha(entry) == old.ENTRY_SHA, '原Markdown入口改变')
        shutil.copyfile(entry, inputs / 'markdown_sample.py')
        (inputs / 'parser_isolation.py').write_text(PARSER_SOURCE)
        require(sha(inputs / 'parser_isolation.py') == PARSER_SHA, '独立parser原文改变')
        import hashlib
        require(len(PARSER_OUTPUT) == 48 and hashlib.sha256(PARSER_OUTPUT).hexdigest() == PARSER_OUTPUT_SHA,
                '独立parser原CP48B改变')
        oracle = self.root / 'reference/markdown-full-cp-run'
        record = json.loads(oracle.with_suffix('.json').read_text())
        require(shared.successful(record) and sha(oracle.with_suffix('.stdout')) == old.GOLDEN_SHA
                and oracle.with_suffix('.stdout').stat().st_size == 77
                and oracle.with_suffix('.stderr').stat().st_size == 0, '原Markdown CP证据不符')
        for stream in ('stdout', 'stderr'):
            path = oracle.with_suffix('.' + stream)
            require(record['streams'][stream] == {'bytes': path.stat().st_size, 'sha256': sha(path)},
                    '原Markdown CP双流不符')
        require(manifest['execution_identity_excerpt']['python_sha256'] == old.PYTHON_SHA, '原CP身份不符')
        shared.targets(inputs)  # 保完整两组19条原字节，不删除console_scripts。
        self.input_identity = snapshot(self.root)
        save(self.out / 'source-inputs.json', self.input_identity)
        save(self.out / 'input-provenance.json', manifest)
        return inputs

    def product(self, name):
        self.barrier()
        argv = self.plans[name]['compile']
        record = self.command(name + '-compile', argv, 240, self.env)
        output = self.out / 'products' / name
        if output.exists():
            files = probes.execution_snapshot(output) if shared.successful(record) else snapshot(output)
            save(self.out / (name + '-products.json'), files)
        self.barrier()
        require(shared.successful(record), name + '默认构建失败，JIT未运行')
        pe = Path(self.plans[name]['jit'][1])
        probes.execution_identity(pe, b'MZ')
        runtime = output / 'RyuPy.Runtime.dll'
        actual_id, expected_id = probes.execution_identity(runtime), probes.execution_identity(self.runtime)
        if actual_id['sha256'] != expected_id['sha256']: raise shared.IdentityError('实际产品Runtime异源')
        save(self.out / (name + '-runtime.json'), {**actual_id,
             'expected_path': expected_id['path'], 'expected_sha256': expected_id['sha256']})
        save(self.out / (name + '-audit.json'), verify_audit(output, self.inputs, name))
        self.frozen.update({str(output / n): r['sha256'] for n, r in probes.execution_snapshot(output).items()})
        self.barrier()
        record = self.command(name + '-jit', self.plans[name]['jit'], 90, self.env)
        self.barrier()
        golden = PARSER_OUTPUT if name == 'parser' else (self.root / 'reference/markdown-full-cp-run.stdout').read_bytes()
        require(shared.successful(record) and (self.out / (name + '-jit.stdout')).read_bytes() == golden
                and (self.out / (name + '-jit.stderr')).read_bytes() == b'', '默认JIT完整原流不符')

    def execute(self):
        code = 2
        try:
            check_context(os.environ, json.loads(Path(os.environ['GITHUB_EVENT_PATH']).read_text()))
            self.before = self.identity()
            require(self.before['head'] == os.environ['GITHUB_SHA'], '源码HEAD与事件不符')
            save(self.out / 'source-identity.json', self.before)
            manifest = probes.source_manifest(ROOT)
            save(self.out / 'production-probe-contract.json', manifest)
            self.frozen.update({str(ROOT / name): digest for name, digest in manifest['files'].items()})
            self.inputs = self.prepare_inputs()
            self.prepare_tools()
            self.env = dict(PATH='/usr/bin:/bin', LANG='C.UTF-8', LC_ALL='C.UTF-8', TZ='UTC',
                PYTHONDONTWRITEBYTECODE='1', PYTHONNOUSERSITE='1', PYTHONHASHSEED='0',
                RYUPY_RUNTIME_DIR=str(self.runtime.parent), DOTNET_ROOT=str(self.dotnet.parent),
                DOTNET_MULTILEVEL_LOOKUP='0', DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1')
            self.plans = command_plan(self.cli, self.dotnet, self.inputs, self.out / 'products')
            save(self.out / 'target-plan.json', self.plans)
            unit_result = {'stages': self.result['production_probes']}
            probes_passed = run_independent(probes.STAGES, lambda name: probes.execute(self, name),
                                           unit_result, self.persist)
            jit_passed = run_independent(self.result['stages'], self.product, self.result, self.persist)
            code = 0 if probes_passed and jit_passed else 1
        except Exception as error:
            self.result.update(error=str(error), traceback=traceback.format_exc())
        finally:
            try:
                if self.before is not None: self.barrier()
                self.result['identity_preserved'] = self.before is not None
            except Exception as error:
                self.result.update(identity_preserved=False, identity_error=str(error)); code = 2
            self.result.update(passed=code == 0, runner_exit=code)
            self.persist()
        return code


if __name__ == '__main__':
    raise SystemExit(Runner().execute())
