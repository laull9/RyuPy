#!/usr/bin/env python3
"""单候选metadata验收：两阶段共享一次工具，所有真实失败保红，不改变默认20口径。"""
import argparse
import collections
import json
import os
from pathlib import Path
import shutil
import sys
import traceback
import xml.etree.ElementTree as ET
import typing_noreturn as old
import distribution_metadata_products as product
from distribution_metadata_identity import IdentityError, inventory, mapped, validate_mapping

ROOT = old.ROOT
require, sha, save = old.require, old.sha, old.save
DATA = json.loads(Path(__file__).with_suffix('.json').read_text())


def check_context(env, event):
    require(env.get('RUNNER_ENVIRONMENT') == 'github-hosted' and
            env.get('RYUPY_VALIDATION_MODE') == 'distribution-metadata', '只允许新hosted有限模式')
    require(event.get('head_commit', {}).get('message') in (DATA['title'], 'validation: textwrap-dedent-001', 'validation: re-scanner-001', 'validation: decimal-percent-001', 'validation: keyerror-construction-001'), '精确标题不符')
    adjusted = dict(event, head_commit=dict(event['head_commit'], message=old.shared.TITLE))
    old.shared.check_context(env, adjusted)


def verify_trx(path):
    rows = ET.parse(path).findall('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}UnitTestResult')
    actual = collections.Counter(x.attrib['testName'].removeprefix('RyuPy.Runtime.Tests.') for x in rows)
    require(actual == collections.Counter(DATA['facts']) and all(x.get('outcome') == 'Passed' for x in rows),
            'metadata C#测试缺失/重复/失败/跳过')
    return {'total': len(rows), 'all_passed': True}


def stage_names():
    names = ['identity', 'csharp-build', 'csharp-tests', 'host-build', 'host-smoke', 'tools', 'sourceprep',
             'unconfigured-jit', 'collection-rejections', 'synthetic-native', 'markdown-aot']
    names += [name + '-' + mode for name in ['synthetic', 'binding', 'parameters', 'missing-include', 'empty',
              'real-discovery', 'markdown-plain', 'markdown-full', *('search-' + n for n in DATA['search'])]
              for mode in ['cp', 'jit']]
    names += ['host-' + n for n in DATA['host_modes']]
    return names


class Runner(old.Runner):
    def __init__(self, resume=False):
        self.out = ROOT / 'target/distribution-metadata-evidence'
        self.data, self.labels, self.frozen = DATA, set(), {}
        self.before = None
        self.resume = resume
        self.identity_failed = False
        self.input_inventories, self.isolation, self.snapshot_reports = {}, {}, {}
        self.python = self.dotnet = self.cli = self.runtime = self.payload = None
        self.fixture, self.inputs_dir = self.out / 'fixtures', self.out / 'inputs'
        self.host = self.out / 'host/bin/MetadataHost.dll'
        self.result = {'passed': False, 'default_project_delta': 0, 'retries': 0,
            'scope': '显式closed snapshot；不是CP动态finder/默认项目晋升/完整Markdown支持',
            'historical': DATA['historical'], 'old_gates': '同HEAD独立jobs；本报告不冒领其结果',
            'stages': {n: {'passed': False, 'status': 'not-run'} for n in stage_names()}}
        self.out.mkdir(parents=True, exist_ok=resume)

    def load_state(self):
        """恢复也处在execute错误出口内；坏state/环境不得保留旧pass。"""
        try:
            raw = (self.out / 'state.json').read_bytes()
            (self.out / 'resume-state.raw.json').write_bytes(raw)
            state = product.contract.parse(raw)
            candidate = state.get('result')
            if (isinstance(candidate, dict) and isinstance(candidate.get('stages'), dict)
                    and all(isinstance(row, dict) for row in candidate['stages'].values())): self.result = candidate
            require(candidate is self.result and state['schema_version'] == 2
                    and set(self.result['stages']) == set(stage_names())
                    and all(type(row.get('passed')) is bool and isinstance(row.get('status'), str)
                            for row in self.result['stages'].values()), 'state版本/阶段集合不符')
            self.before, self.frozen = state['before'], state['frozen']
            self.input_inventories, self.isolation = state['input_inventories'], state['isolation']
            self.snapshot_reports = state['snapshot_reports']
            self.identity_failed = state['identity_failed']
            self.labels = set(state['labels'])
            for key in ('python', 'dotnet', 'cli', 'runtime', 'payload'):
                setattr(self, key, Path(state[key]) if state.get(key) else None)
            require(not self.identity_failed and not self.isolation, '先前身份失效或隔离未恢复')
            self.environment()
            require(self.env == state['environment'], '两阶段进程环境改变')
            if self.result['stages']['sourceprep']['passed']:
                require(set(self.input_inventories) == {str(self.fixture), str(self.inputs_dir)}, '完整输入冻结域缺失/改变')
        except Exception as error:
            # JSON未解析时仍尽力保留旧result的阶段，再统一全部失效；不伪造新before。
            if not self.result.get('stages') or all(x.get('status') == 'not-run' for x in self.result['stages'].values()):
                try:
                    previous = product.contract.parse((self.out / 'result.json').read_bytes())
                    if (isinstance(previous.get('stages'), dict)
                            and all(isinstance(row, dict) for row in previous['stages'].values())): self.result = previous
                except Exception: pass
            self.fail_identity(error)

    def environment(self):
        self.env = {k: os.environ[k] for k in ('PATH', 'HOME', 'RUNNER_TEMP') if k in os.environ}
        self.env.update(RYUPY_RUNTIME_DIR=str(self.out / 'tools'), DOTNET_ROOT=str(self.dotnet.parent),
            DOTNET=str(self.dotnet), DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1',
            PYTHONDONTWRITEBYTECODE='1', PYTHONNOUSERSITE='1', PYTHONHASHSEED='0', LC_ALL='C.UTF-8', TZ='UTC')

    def block(self, name, reason):
        self.result['stages'][name] = {'passed': False, 'status': 'blocked', 'reason': reason}
        save(self.out / 'result.json', self.result)

    def identities(self):
        check_context(os.environ, json.loads(Path(os.environ['GITHUB_EVENT_PATH']).read_text()))
        self.before = self.identity('before')
        require(self.before['head'] == os.environ['GITHUB_SHA'], '实际HEAD不符')
        # 全部当前source属于同一提交；不冒用旧text-search父/祖父白名单。
        save(self.out / 'source-identity.json', self.before)
        archive = self.out / 'candidate-source.tar'
        self.checked('source-archive', ['git', 'archive', '--format=tar', '--output=' + str(archive), 'HEAD'], 60)
        self.frozen[str(archive)] = sha(archive)
        self.python, self.dotnet = Path(os.environ['PYTHON']).resolve(), Path(os.environ['DOTNET']).resolve()
        require(sys.version_info[:3] == (3, 12, 13) and sha(self.python) == old.shared.PYTHON_SHA, '固定CP身份不符')
        versions = {'dotnet': '10.0.403', 'rustc': 'rustc 1.99.1 (58cf8f9c3 2026-10-13)',
                    'cargo': 'cargo 1.99.1 (5f94df478 2026-08-27)'}
        tools = {'python': self.python}
        for name, expected in versions.items():
            path = self.dotnet if name == 'dotnet' else Path(shutil.which(name)).resolve()
            self.checked(name + '-version', [path, '--version'], 30)
            require((self.out / (name + '-version.stdout')).read_text().strip() == expected, '工具版本不符')
            tools[name] = path
        for name, expected in DATA['old_protected'].items():
            require(sha(ROOT / name) == expected, '旧输入/oracle改变：' + name)
        self.frozen.update({str(p): sha(p) for p in tools.values()})
        save(self.out / 'tool-identity.json', {n: {'path': str(p), 'sha256': sha(p)} for n, p in tools.items()})
        self.environment()
        save(self.out / 'runtime-environment.json', self.env)

    def managed_tests(self):
        target = self.out / 'trx'
        self.checked('managed-tests', [self.dotnet, 'test', ROOT / 'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj',
            '-c', 'Release', '--no-build', '--no-restore', '--filter', 'FullyQualifiedName~PyDistributionMetadataTests',
            '--logger', 'trx;LogFileName=result.trx', '--results-directory', target], 300)
        # 原始TRX保存到checkpoint，不只摘录计数。
        self.preserve(target, 'trx')
        return verify_trx(target / 'result.trx')

    def host_build(self):
        root = self.out / 'host'
        shutil.copytree(product.FIXTURES / 'host', root)
        runtime = self.out / 'managed/RyuPy.Runtime.dll'
        self.checked('host-build-command', [self.dotnet, 'build', root / 'MetadataHost.csproj', '-c', 'Release',
            '-o', root / 'bin', '-p:RuntimePath=' + str(runtime), '-nologo'], 120)
        self.audit(lambda: require(self.host.is_file() and sha(self.host.parent / 'RyuPy.Runtime.dll') == sha(runtime), 'host runtime身份不符'))
        self.preserve(self.host.parent, 'host-before-execution')

    def build_tools(self):
        # C#测试项目已经构建唯一runtime，这里只构建一次CLI，避免make runtime重复。
        self.checked('compiler-build', ['cargo', 'build', '--locked', '-p', 'ryupy-cli', '--features', old.shared.FEATURE], 1200)
        target = self.out / 'tools'; target.mkdir()
        self.cli, self.runtime = target / 'ryupy', target / 'RyuPy.Runtime.dll'
        shutil.copy2(ROOT / 'target/debug/ryupy', self.cli)
        shutil.copy2(self.out / 'managed/RyuPy.Runtime.dll', self.runtime)
        self.preserve(target, 'compiled-tools')
        self.environment()

    def hosts(self):
        if not self.result['stages']['host-smoke']['passed']:
            for mode in DATA['host_modes']: self.block('host-' + mode, 'host基础设施未通过')
            return
        if self.payload is None:
            for mode in DATA['host_modes']: self.block('host-' + mode, '无经PE核验的真实快照payload')
            return
        for mode in DATA['host_modes']:
            self.phase('host-' + mode, lambda: self.run_positive('host-' + mode + '-run',
                [self.dotnet, self.host, mode, self.payload], (mode + ' ok\n').encode()))

    def freeze_inputs(self, root):
        root = str(root)
        require(root not in self.input_inventories, '禁止覆盖输入authority')
        self.input_inventories[root] = inventory(root)
        save(self.out / (Path(root).name + '-inventory.json'), self.input_inventories[root])

    def audit(self, operation):
        try: return operation()
        except Exception as error: self.fail_identity(error)

    def fail_identity(self, error):
        self.invalidate(error)
        self.persist()
        raise IdentityError(str(error)) from error

    def barrier(self, label, cleanup=False):
        if cleanup: self.result['restoration_full_check'] = 'started'
        if self.identity_failed and not cleanup:
            raise IdentityError(self.result.get('identity_error', '身份已失效'))
        try:
            require(self.before is not None and self.identity('barrier-' + str(len(self.labels)) + '-' + label) == self.before,
                    'source HEAD/tree/完整工作树身份改变')
            validate_mapping(self.isolation)
            require(all(mapped(p, self.isolation).is_file() and not mapped(p, self.isolation).is_symlink()
                        and sha(mapped(p, self.isolation)) == value for p, value in self.frozen.items()),
                    '工具/input/payload/PE/runtime身份改变')
            for root, expected in self.input_inventories.items():
                require(inventory(root, self.isolation) == expected, '完整输入目录成员/类型/bytes改变：' + root)
            if cleanup: self.result['restoration_full_check'] = 'passed'
            require(not self.identity_failed, '此前身份失效不可恢复为通过')
            self.result['identity_preserved'] = True
        except Exception as error:
            if cleanup and self.result['restoration_full_check'] == 'started': self.result['restoration_full_check'] = 'failed'
            self.fail_identity(error)

    def phase(self, name, operation):
        if self.identity_failed: raise IdentityError('身份失效，停止后继阶段')
        self.result['stages'][name] = {'passed': False, 'status': 'started'}
        try:
            detail = operation()
            self.result['stages'][name].update(passed=True, status='passed')
            if isinstance(detail, dict): self.result['stages'][name].update(detail)
        except IdentityError as error:
            self.result['stages'][name].update(passed=False, status='invalidated-identity', error=str(error))
            self.fail_identity(error)
        except Exception as error:
            self.result['stages'][name].update(status='failed', error=str(error), traceback=traceback.format_exc())
        save(self.out / 'result.json', self.result)
        return self.result['stages'][name]['passed']

    def run_product(self, name, argv, env=None):
        self.barrier(name + '-before-execute')
        try: return super().run_product(name, argv, env)
        finally: self.barrier(name + '-after-execute')

    def invalidate(self, error):
        self.identity_failed = True
        self.result.update(passed=False, identity_preserved=False)
        self.result.setdefault('identity_error', str(error))
        for row in self.result['stages'].values():
            if row.get('passed') or row.get('status') == 'started': row.update(passed=False, status='invalidated-identity')

    def persist(self):
        state = {'schema_version': 2, 'before': self.before, 'frozen': self.frozen,
                 'input_inventories': self.input_inventories, 'isolation': self.isolation, 'identity_failed': self.identity_failed,
                 'snapshot_reports': self.snapshot_reports,
                 'result': self.result, 'labels': sorted(self.labels), 'environment': getattr(self, 'env', None)}
        for key in ('python', 'dotnet', 'cli', 'runtime', 'payload'):
            state[key] = str(getattr(self, key)) if getattr(self, key) is not None else None
        save(self.out / 'state.json', state)
        save(self.out / 'result.json', self.result)

    def execute(self, phase):
        try:
            if phase == 'jit':
                if not self.phase('identity', lambda: self.audit(self.identities)): return 1
                if not self.phase('csharp-build', lambda: self.managed(build_only=True)): return 1
                self.phase('csharp-tests', self.managed_tests)
                if self.phase('host-build', self.host_build):
                    self.phase('host-smoke', lambda: self.run_positive('host-smoke-run', [self.dotnet, self.host, 'smoke'], b'smoke ok\n'))
                if not self.phase('tools', self.build_tools): return 1
                product.products(self)
                self.hosts()
            else:
                self.load_state()
                self.audit(lambda: check_context(os.environ, json.loads(Path(os.environ['GITHUB_EVENT_PATH']).read_text())))
                self.barrier('resume-before')
                require(self.result['stages']['tools']['passed'] and self.cli is not None, '无已验证工具')
                if self.result['stages']['sourceprep']['passed']:
                    if self.result['stages']['synthetic-cp']['passed']:
                        self.phase('synthetic-native', lambda: product.native(self))
                    else: self.block('synthetic-native', '同源合成CP失败')
                    self.barrier('between-native-domains')
                    # 独立原AOT诊断不依赖full JIT是否通过。
                    self.phase('markdown-aot', lambda: product.native(self, markdown=True))
                else:
                    self.block('synthetic-native', '输入准备失败')
                    self.block('markdown-aot', '输入准备失败')
            self.result['passed'] = all(row['passed'] for row in self.result['stages'].values())
        except IdentityError as error:
            self.invalidate(error)
        except Exception as error:
            self.result.update(passed=False, error=str(error), traceback=traceback.format_exc())
        finally:
            try: self.barrier(phase + '-after')
            except Exception as error: self.invalidate(error)
            self.persist()
        considered = [v for k, v in self.result['stages'].items() if phase == 'native' or k not in ('synthetic-native', 'markdown-aot')]
        return 0 if not self.identity_failed and 'error' not in self.result and self.result.get('identity_preserved') and all(x['passed'] for x in considered) else 1


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--phase', choices=['jit', 'native'], required=True)
    args = parser.parse_args()
    raise SystemExit(Runner(resume=args.phase == 'native').execute(args.phase))
