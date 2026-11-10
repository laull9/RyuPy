#!/usr/bin/env python3
"""NoReturn同候选hosted验收：C#早门、真实JIT/Native及Markdown分阶段；不改变默认项目计数。"""
import collections
import json
import os
from pathlib import Path
import shutil
import sys
import traceback
import xml.etree.ElementTree as ET

import markdown_preflight as shared
sys.path.insert(0, str(shared.REPO / 'tests/aot'))
from aot_layout import reported_aot
from manifest_verify import verify_manifest, relocate_manifest, read_sdk_version
from native_evidence import preserve_dist

ROOT = shared.REPO
TITLE = 'validation: typing-noreturn-001'
BASE_SOURCE_HEAD = 'fb9c1214a7ce0f00f9793c9904c2a6fa412bc31e'
REMOTE_BASE_HEAD = '7c9c895178181664d06a113ba369185e19650688'
BASE_TREE = 'eb1d3432a0a00917e6a320ee47c2f7eac7d2c37e'
FROZEN = {
    'docs/adr/0147-typing-noreturn-consumers.md':
        '8230e2ff5385f1a63c7a024d41c5c7c5962e8eae9c1e4af764a5fc7466b95624',
    'runtime/RyuPy.Runtime.Tests/PyTypingCallableCacheTests.cs':
        '711bbdaf21289d160e8c4c97f831696f3d3cc3337b7bff78128559ba9b4358f7',
    'runtime/RyuPy.Runtime.Tests/PyTypingNamedTupleAnnotationsTests.cs':
        '994669189a8bf27c3534be43a4faf6f6dd22920cb79cc3908be5f97e2fb214c3',
    'runtime/RyuPy.Runtime.Tests/PyTypingNoReturnTests.cs':
        '4e924cfeea312edac018cf303a79d0e3776c310075430bafa11b6c86842f8b36',
    'runtime/RyuPy.Runtime/PyBuiltinTypes.cs':
        '9b609fd2607a1f1757a95f3f117e2cd0a6ec7d470e24f25143ab30d3dd236a87',
    'runtime/RyuPy.Runtime/PyRuntimeBases.cs':
        '5c32684ca7d4949cb1c8e231e8024aa00505d38405e4c6f2aa1625180d319699',
    'runtime/RyuPy.Runtime/PyTypedDict.cs':
        '087e4e279c167cd5e1718deebe401501c0b10a46003c152acafd8d42913a9501',
    'runtime/RyuPy.Runtime/PyTyping.cs':
        '3ac25f43218c9993d23cba57d2208c118605e62dc1954818c0a4268c92a25f4c',
    'runtime/RyuPy.Runtime/PyTypingLiteral.cs':
        '2c5d00b284f98af1fcff4f7e3dd2aecbede3223b510ebbb55bce1b498309dafe',
    'runtime/RyuPy.Runtime/PyTypingNamedTupleAnnotations.cs':
        'fd4e890488e693995c6b4deb649d507af651a342af330a1ea640c91bacf9c2f7',
    'runtime/RyuPy.Runtime/PyTypingNoReturn.cs':
        '9b4b6c46cb917c13fcd3841527717e4962808cf2cdf0c2ec23d26571dcd65be1',
    'runtime/RyuPy.Runtime/PyTypingSpecialForms.cs':
        '343389d887df8c1d68bb461e10e99fd76b56821455f25c5bfdc21bb29e7e3420',
    'runtime/RyuPy.Runtime/PyTypingTypeVarFactory.cs':
        '83a1285cf0588474092f96b4464a7886019df561a7483d8925fad2ff954e8a34',
    'tests/e2e/phase3/m3_7_typing_noreturn.py':
        '542f65d9108f343605b0c1df41b21e53c42618f088a900eee0196e1e108f99cb',
}
CI_PATHS = {'.github/workflows/wcwidth-legacy-aot.yml', '.github/workflows/typing-noreturn.yml',
    'tests/validation/typing_noreturn.py', 'tests/validation/test_typing_noreturn.py',
    'tests/validation/markdown_preflight.py',
    'tests/validation/generator_product/test_diagnostic_once.py'}
FACTS = {
    'RootUsesCanonicalSpecialFormAndAccuratePublicProtocols',
    'RuntimeClassChecksKeepTupleShortCircuitAndRootError',
    'EveryTypeCheckingConsumerAcceptsOriginalNoReturn',
    'MissingExportBreaksColdConsumersButNotWarmOrUncheckedPositions',
    'RebindingChangesMembershipAndSavedRootStillHasOwnIdentity',
    'UnknownComparisonOnlyRejectsAtActualMembershipAfterAliasChecks',
    'NoneReplacementPreservesOldOrdinaryBoundNamedAndCallableBehavior',
    'LaterHelperIsConsumedBeforeAnyEarlyReturnAndUnknownHashRemainsExplicit',
    'LiteralOnlyAddsImmutableFormIdentityAndPreservesOldValueDomain',
}
CONTRACT = 'tests/e2e/phase3/m3_7_typing_noreturn.py'
ORACLE_SHA = '4ca42299e1eb9a42407935a54db7321401ba3b729edeac827fb4b53d0ceffdf1'
require, save, sha, snapshot = shared.require, shared.save, shared.sha, shared.snapshot


def check_context(env, event):
    require(env.get('RUNNER_ENVIRONMENT') == 'github-hosted', '只允许hosted')
    # 仅复用旧严格上下文检查，不修改旧模块标题/树，也不调用旧Runner.execute。
    adjusted = dict(event, head_commit=dict(event.get('head_commit') or {}))
    require(adjusted['head_commit'].get('message') == TITLE, '新验收标题不符')
    adjusted['head_commit']['message'] = shared.TITLE
    shared.check_context(env, adjusted)


def verify_trx(path):
    rows = ET.parse(path).findall('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}UnitTestResult')
    require(rows and all(x.get('outcome') == 'Passed' for x in rows), 'C#失败、跳过或零测试')
    names = [x.attrib['testName'] for x in rows]
    actual = collections.Counter(x.rsplit('.', 1)[-1] for x in names if '.PyTypingNoReturnTests.' in x)
    require(actual == collections.Counter(FACTS), '必须实际执行全部9个不同Fact')
    require(all('.PyTyping' in x for x in names), '过滤器包含意外测试')
    require(any('.PyTypingCallableCacheTests.' in x for x in names) and
            any('.PyTypingNamedTupleAnnotationsTests.' in x for x in names), '两旧迁移类漏跑')
    return {'total': len(rows), 'new_facts': 9, 'all_passed': True}


def positive(record, prefix, golden):
    return (record['exit'] == 0 and record['failure'] is None and record['raw_streams_complete']
            and prefix.with_suffix('.stdout').read_bytes() == golden
            and prefix.with_suffix('.stderr').read_bytes() == b'')


def plain_probe(entry):
    # 保原入口首个调用逐字，不修改完整入口；只能删已锁定的第二个print。
    lines = entry.read_bytes().splitlines(keepends=True)
    require(sha(entry) == shared.ENTRY_SHA and len(lines) == 5, '原双调用入口改变')
    return b''.join(lines[:4])


class Runner(shared.Runner):
    def __init__(self):
        self.out = ROOT / 'target/typing-noreturn-evidence'
        self.out.mkdir(parents=True, exist_ok=False)
        self.labels = set()
        self.result = {'passed': False, 'default_project_delta': 0, 'stages': {
            x: {'passed': False, 'status': 'not-run'} for x in
            ['csharp', 'tools', 'contract-jit', 'contract-native', 'markdown-plain', 'markdown-full']},
            'scope': 'NoReturn支持合同与显式Markdown阶段；非全typing、非默认Markdown晋升',
            'old_gates': '独立原jobs运行，本runner不代替或跳过', 'retries': 0}
        self.frozen = {}

    def preserve(self, root, label):
        require(root.is_dir(), '缺产物目录：' + str(root))
        files = snapshot(root)
        require(files, '产物目录为空')
        save(self.out / (label + '-files.json'), files)
        for name, row in files.items():
            self.frozen[str(root / name)] = row['sha256']
        return files

    def phase(self, name, operation):
        self.result['stages'][name] = {'passed': False, 'status': 'started'}
        try:
            operation()
            self.result['stages'][name].update(passed=True, status='passed')
        except Exception as error:
            self.result['stages'][name].update(status='failed', error=str(error), traceback=traceback.format_exc())
        save(self.out / 'result.json', self.result)
        return self.result['stages'][name]['passed']

    def managed(self):
        tests = ROOT / 'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj'
        self.checked('csharp-build', [self.dotnet, 'build', tests, '-c', 'Release', '-nologo'], 600)
        managed = tests.parent / 'bin/Release/net10.0'
        target = self.out / 'managed'; target.mkdir()
        for name in ['RyuPy.Runtime.dll', 'RyuPy.Runtime.Tests.dll']:
            shutil.copy2(managed / name, target / name)
        self.preserve(target, 'csharp-inputs')
        self.checked('csharp-tests', [self.dotnet, 'test', tests, '-c', 'Release', '--no-build',
            '--no-restore', '--filter', 'FullyQualifiedName~PyTyping', '--logger',
            'trx;LogFileName=typing.trx', '--results-directory', self.out / 'trx'], 300)
        save(self.out / 'csharp-results.json', verify_trx(self.out / 'trx/typing.trx'))

    def build_tools(self):
        shared.tool_build(self.checked)
        target = self.out / 'tools'; target.mkdir()
        for name, src in [('ryupy', ROOT / 'target/debug/ryupy'), ('RyuPy.Runtime.dll',
                ROOT / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll')]:
            shutil.copy2(src, target / name)
        require(sha(target / 'RyuPy.Runtime.dll') == sha(self.out / 'managed/RyuPy.Runtime.dll'),
                'C#早门和产品Runtime不一致')
        self.preserve(target, 'compiled-tools')
        self.cli, self.runtime = target / 'ryupy', target / 'RyuPy.Runtime.dll'
        self.env = {k: os.environ[k] for k in ('PATH', 'HOME', 'RUNNER_TEMP') if k in os.environ}
        self.env.update(RYUPY_RUNTIME_DIR=str(target), DOTNET_ROOT=str(self.dotnet.parent),
            DOTNET=str(self.dotnet), DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1', PYTHONDONTWRITEBYTECODE='1',
            PYTHONNOUSERSITE='1', PYTHONHASHSEED='0', LC_ALL='C.UTF-8', TZ='UTC')

    def run_positive(self, name, argv, golden, env=None):
        require(name not in self.labels, '禁止重复执行：' + name)
        self.labels.add(name)
        cwd = self.out / 'cwd' / name; cwd.mkdir(parents=True)
        actual = shared.capture(self.out / name, argv, cwd, 30, env or self.env)
        require(positive(actual, self.out / name, golden), name + '实际exit/stdout/stderr不符；见原流')

    def jit(self, name, entry, golden, inputs=None):
        target = self.out / 'products' / name
        args = (shared.build_command(self.cli, entry, inputs, target) if inputs else
                [self.cli, 'build', entry, '--diagnostic-stages', '--out-dir', target])
        env = dict(self.env)
        if inputs:
            env['PYTHONPATH'] = os.pathsep.join(map(str, [inputs / 'stdlib', inputs / 'markdown']))
        if name != 'contract':
            self.run_positive(name + '-cp', [self.python, '-B', entry], golden, env)
        actual = self.command(name + '-compile', args, 120, env)
        require(actual['exit'] == 0 and not actual['failure'], name + '编译失败，未执行JIT')
        dll = target / (entry.stem + '.dll')
        require(dll.is_file() and sha(target / 'RyuPy.Runtime.dll') == sha(self.runtime), 'PE/Runtime身份不符')
        preserved = self.out / 'preserved-pe' / name
        shutil.copytree(target, preserved)
        require(snapshot(target) == snapshot(preserved), '执行前PE保存不一致')
        self.preserve(target, name + '-pe')
        self.preserve(preserved, name + '-preserved-pe')
        self.run_positive(name + '-jit', [self.dotnet, dll], golden, env)

    def preserve_publish_evidence(self, target, stem):
        # publish失败也保存同次SDK原流/生成项目/PE，绝不复制SDK、cache、obj或bin。
        saved = self.out / 'sdk-evidence'; saved.mkdir()
        names = {'publish.stdout.log', 'publish.stderr.log', 'sdk.version', 'Program.cs',
                 stem + '.dll', stem + '.runtimeconfig.json', 'RyuPy.Runtime.dll',
                 'module-environment.json', 'codegen-report.json'}
        total = 0
        for directory, dirs, files in os.walk(target):
            dirs[:] = [x for x in dirs if x not in {'obj', 'bin', 'publish'} and not x.endswith('.dist')]
            for name in files:
                if name not in names and not name.endswith('.csproj'):
                    continue
                src = Path(directory) / name
                require(not src.is_symlink(), '发布证据不接受符号链接')
                total += src.stat().st_size
                require(total <= 32 * 1024 * 1024, 'SDK小证据超32MiB')
                dest = saved / src.relative_to(target); dest.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(src, dest)
        save(self.out / 'sdk-evidence-files.json', snapshot(saved))

    def native(self, entry, golden):
        target = self.out / 'native-build'
        built = self.command('native-publish', [self.cli, 'build', entry, '--diagnostic-stages',
            '--out-dir', target, '--aot'], 1200, self.env)
        self.preserve_publish_evidence(target, entry.stem)
        require(built['exit'] == 0 and not built['failure'], 'Native publish失败，未执行Native')
        layout = reported_aot((self.out / 'native-publish.stderr').read_bytes(), target, entry.stem, 'linux-x64')
        preserve_dist(layout.dist, self.out)
        self.preserve(layout.dist, 'native-original')
        sdk = read_sdk_version(layout.host / 'sdk.version')
        require(sdk == '10.0.403', '实际发布SDK错误')
        saved = self.out / 'native-managed'; saved.mkdir()
        for path in layout.managed.parent.iterdir():
            if path.is_file():
                shutil.copy2(path, saved / path.name)
        shutil.copy2(layout.host / 'sdk.version', saved / 'sdk.version')
        self.preserve(saved, 'native-managed')
        manifest = verify_manifest(layout.dist, layout.native, 'linux-x64', sdk)
        save(self.out / 'native-manifest.json', manifest)
        bindings = {x['role']: x for x in manifest['build_inputs']}
        require(bindings['runtime_managed_input']['sha256'] == sha(self.runtime) and
                bindings['compiled_managed_input']['sha256'] == sha(layout.managed), 'AOT输入绑定错误')
        require(sha(layout.managed.parent / 'RyuPy.Runtime.dll') == sha(self.runtime), 'AOT Runtime错误')
        sources = manifest['sources']
        require(len(sources) == 1 and sources[0]['module_name'] == '__main__' and
                sources[0]['compiled_text_sha256'] == sha(entry), 'Native源码绑定不符')
        native_env = {'PATH': '/usr/bin:/bin', 'LC_ALL': 'C.UTF-8', 'TZ': 'UTC'}
        self.run_positive('native-original', [layout.native], golden, native_env)
        moved = self.out / 'relocated'
        binary, _ = relocate_manifest(layout.dist, layout.native, moved, 'linux-x64', sdk)
        require(snapshot(moved) == snapshot(layout.dist), '搬移文件身份改变')
        self.preserve(moved, 'native-relocated')
        self.run_positive('native-relocated', [binary], golden, native_env)

    def execute(self):
        before = None
        try:
            check_context(os.environ, json.loads(Path(os.environ['GITHUB_EVENT_PATH']).read_text()))
            before = self.identity('before')
            require(before['head'] == os.environ['GITHUB_SHA'], '候选HEAD不符')
            self.checked('base-files', ['git', 'ls-tree', '-r', '-z', BASE_TREE], 20)
            base = shared.tree_records((self.out / 'base-files.stdout').read_bytes())
            changed = {p for p in base.keys() | before['files'].keys() if base.get(p) != before['files'].get(p)}
            require(changed <= set(FROZEN) | CI_PATHS, '出现候选外差异')
            require(all(sha(ROOT / p) == h for p, h in FROZEN.items()), '冻结14路径hash不符')
            save(self.out / 'source-identity.json', dict(before, frozen_paths=FROZEN,
                base_source_head=BASE_SOURCE_HEAD, remote_base_head=REMOTE_BASE_HEAD, base_tree=BASE_TREE))
            self.python, self.dotnet = Path(os.environ['PYTHON']).resolve(), Path(os.environ['DOTNET']).resolve()
            require(sys.version_info[:3] == (3, 12, 13) and sha(self.python) == shared.PYTHON_SHA, 'CP身份不符')
            tools = {'python': self.python, 'dotnet': self.dotnet}
            versions = {'dotnet': '10.0.403', 'rustc': 'rustc 1.99.1 (58cf8f9c3 2026-10-13)',
                        'cargo': 'cargo 1.99.1 (5f94df478 2026-08-27)'}
            for name, expected in versions.items():
                path = self.dotnet if name == 'dotnet' else Path(shutil.which(name)).resolve()
                self.checked(name + '-version', [path, '--version'], 30)
                require((self.out / (name + '-version.stdout')).read_text().strip() == expected, '工具版本不符')
                tools[name] = path
            self.frozen.update({str(p): sha(p) for p in tools.values()})
            save(self.out / 'tool-identity.json', {'head': before['head'], 'versions': versions,
                'paths': {k: str(v) for k, v in tools.items()}, 'sha256': self.frozen.copy(), 'feature': shared.FEATURE,
                'cli_configuration': 'debug', 'runtime_configuration': 'Release',
                'csharp_configuration': 'Release'})
            if not self.phase('csharp', self.managed):
                return 1
            if not self.phase('tools', self.build_tools):
                return 1
            # 固定原positive源完整执行；不删构造边界，不把CP-only未知协议纳入支持。
            contract = ROOT / CONTRACT
            cp = self.command('contract-oracle', [self.python, '-B', contract], 30, self.env)
            golden = (self.out / 'contract-oracle.stdout').read_bytes()
            require(len(golden) == 1482 and sha(self.out / 'contract-oracle.stdout') == ORACLE_SHA
                    and positive(cp, self.out / 'contract-oracle', golden), '固定合同oracle不符')
            self.phase('contract-jit', lambda: self.jit('contract', contract, golden))
            self.phase('contract-native', lambda: self.native(contract, golden))
            inputs = self.inputs(self.python)
            full = inputs / 'markdown_sample.py'
            plain = inputs / 'markdown_plain_probe.py'; plain.write_bytes(plain_probe(full))
            expected = (inputs / 'stdout.txt').read_bytes()
            require(len(expected) == 77, '原完整golden不符')
            self.preserve(inputs, 'inputs')
            self.phase('markdown-plain', lambda: self.jit('markdown-plain', plain, expected[:48], inputs))
            self.phase('markdown-full', lambda: self.jit('markdown-full', full, expected, inputs))
            self.result['passed'] = all(x['passed'] for x in self.result['stages'].values())
        except Exception as error:
            self.result.update(status='failed', error=str(error), traceback=traceback.format_exc())
        finally:
            try:
                require(before is not None and self.identity('after') == before, '源码身份改变或未核验')
                require(all(sha(Path(p)) == h for p, h in self.frozen.items()), '工具/输入/产物身份改变')
                self.result['identity_preserved'] = True
            except Exception as error:
                self.result.update(passed=False, identity_preserved=False, identity_error=str(error))
            save(self.out / 'result.json', self.result)
        return 0 if self.result['passed'] else 1


if __name__ == '__main__':
    raise SystemExit(Runner().execute())
