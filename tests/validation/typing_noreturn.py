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
BASE_SOURCE_HEAD = 'b0aee23f15ed1662039756bd60eef1d32e9e00c8'
REMOTE_BASE_HEAD = '1e263ff824ef8f50d2559e1562ff3d71b17db228'
BASE_TREE = 'e5a1e790d0bbf8a1c0ce3dff27a2771a8fae0944'
FROZEN = {
    'docs/adr/0147-typing-noreturn-consumers.md':
        '8230e2ff5385f1a63c7a024d41c5c7c5962e8eae9c1e4af764a5fc7466b95624',
    'runtime/RyuPy.Runtime.Tests/PyTypingCallableCacheTests.cs':
        '711bbdaf21289d160e8c4c97f831696f3d3cc3337b7bff78128559ba9b4358f7',
    'runtime/RyuPy.Runtime.Tests/PyTypingNamedTupleAnnotationsTests.cs':
        '994669189a8bf27c3534be43a4faf6f6dd22920cb79cc3908be5f97e2fb214c3',
    'runtime/RyuPy.Runtime.Tests/PyTypingNoReturnTests.cs':
        'a21bb09e631e2370e7de04b3f0f6c915780b9c288e364a2f7a457ad3c1f9fe10',
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
    'tests/e2e/phase3/m3_7_typing_noreturn_supported.py':
        '52e6eda8542f09371e0c222a64983ab4bff2fd759b3207879bbe829e0e3ca4f1',
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
SUPPORTED = 'tests/e2e/phase3/m3_7_typing_noreturn_supported.py'
SUPPORTED_ORACLE_SHA = '3d3141a80cd27e59aa3e162f9f37cd6cd2553ff0a581902285ac9bfc20c678ff'
# run42754725069的原完整源JIT删除边界，严格原流；不是CP1482B支持。
BOUNDARY_STREAMS = {
    'stdout': (851, 'ccf5907ccaa9a88efc3232deea54c37ffbbfcf4b7f4d61d5ce5b66ce4485e5c7'),
    'stderr': (174, 'a20780ff889db88ed94e1f86206017a1b120e7e40be0cbadeca7c267b2bed79e'),
}
ORACLE_SHA = '4ca42299e1eb9a42407935a54db7321401ba3b729edeac827fb4b53d0ceffdf1'
# 新模式仅认本次精确标题；旧标题、来源与oracle常量保持不变。
EXPAND_TITLE = 'validation: text-expandtabs-001'
EXPAND_PARENT = 'f306cae57860d4d8e46ed288984b8b7eafd39830'
EXPAND_TREE = '989b15a877c7fdbda6dcc6383ede93d1f66caa60'
EXPAND_CONTRACTS = {'core': ('70b9048711a29479be013369c7ef90c25cbb229f9d44016cfa7407cc6e17fc3b', 47,
    '301913567a9655c475190b8036f2a3d294e937ad7e8265d46dfaff18c0a0ca1e',
    '657870616e647461627320636f72650a657870616e6474616273206361707475726520616e642062696e64696e670a'),
    'slots': ('d8ac96a484fc4256a7f170b48ea97406e56039d2e38191dc294cfc271c8d6831', 17,
    '3b6ce87f9f833603ee74a287d9411bfd74f25fa683846cbc63fdbee7147fcb5b',
    '657870616e647461627320736c6f74730a'),
    'warning_contract': ('b01539ed6ac3ab126fd90044858bed2d307979a3025bb4b122aceea099970ad9', 20,
    '47fecd9627816664d4b1c0d6a2ed4cb29c09b184036b1ebc77b08a3e1da83f23',
    '657870616e6474616273207761726e696e67730a')}
EXPAND_FACTS = {
    'PyTextExpandTabsProtocolTests.TypeSlotIgnoresInstanceAndIntFallbackButObservesClassMutation',
    'PyTextExpandTabsProtocolTests.OrdinaryStaticClassPropertyAndCallableSlotsHaveDistinctReceivers',
    'PyTextExpandTabsProtocolTests.DescriptorLookupIgnoresItsInstanceShadowAndPassesActualIndexClass',
    'PyTextExpandTabsProtocolTests.RawGetIsNotBoundAgainAndRejectsClassMethodAndProperty',
    'PyTextExpandTabsProtocolTests.GetterResultsRespectStrictCallableGateAndMissingGetterMessage',
    'PyTextExpandTabsProtocolTests.ExistingPythonExceptionIndexCarrierIsNotLost',
    'PyTextExpandTabsProtocolTests.UserBodiesKeepOriginalExceptionFramesCauseAndContext',
    'PyTextExpandTabsProtocolTests.NoneSlotAndWideIndexReturnKeepPreciseFailuresAndSingleInvocation',
    'PyTextExpandTabsProtocolTests.ExistingNativeSpecialIndexBoundaryRemainsAvailable',
    'PyTextExpandTabsProtocolTests.OrdinaryRawGetKeepsLegacySuperContextAndRestoresOuterContextOnBothExits',
    'PyTextExpandTabsTests.FourRoutesShareDefaultsKeywordsAndBindingPriority',
    'PyTextExpandTabsTests.ConversionRunsOnceEvenWithoutTabsAndRejectsBothCIntOverflows',
    'PyTextExpandTabsTests.CodePointColumnsCopyPairsAndLoneSurrogatesAndOnlyCrLfReset',
    'PyTextExpandTabsTests.CapacityChecksNeverAllocateTheirHypotheticalOutput',
    'PyTextExpandTabsTests.SubtypeInheritanceSavedOverrideShadowAndSuperKeepTheirRoutes',
    'PyTextExpandTabsTests.SharedPicArgumentArrayIsFrozenBeforeIndexReentry',
    'PyTextExpandTabsWarningTests.BoolReturnUsesActualFiltersAndDirectBoolNeverWarns',
    'PyTextExpandTabsWarningTests.RealStderrReentryKeepsOuterTextAndTabsizeAndWriteErrorIdentity',
}
EXPAND_FROZEN = {'docs/adr/0148-string-expandtabs.md': '0f0f1b77581967181dadd1d329f2964743c227e46e19b6de4015626cd3cdeef2',
    'runtime/RyuPy.Runtime.Tests/PyTextExpandTabsProtocolTests.cs': '9f256511e8ced8843e2b7273558ee31b0119d4f7ee59929e3d5d2a2934a2068d',
    'runtime/RyuPy.Runtime.Tests/PyTextExpandTabsTests.cs': 'fb11b8b96e6a87aa0d0f63aa0dd62fbd0135736a282f78857f3542f5573e2eb4',
    'runtime/RyuPy.Runtime.Tests/PyTextExpandTabsWarningTests.cs': '46fe3a957ba34b152d7092495d7e8fabafe9e0f86078b5347a438ed43a466d92',
    'runtime/RyuPy.Runtime/PyTextExpandTabs.cs': 'f181e00af6638eade4224ce60a453f1dc9a8b5d7099ca2a80ddebd168f15c43d',
    'tests/fixtures/text_expandtabs/boundaries.py': 'aca77c5cb797ec384dd73d8a050f78b37ea74bfd5ee770d81113b4cb5af33208',
    'tests/fixtures/text_expandtabs/core.py': '70b9048711a29479be013369c7ef90c25cbb229f9d44016cfa7407cc6e17fc3b',
    'tests/fixtures/text_expandtabs/slots.py': 'd8ac96a484fc4256a7f170b48ea97406e56039d2e38191dc294cfc271c8d6831',
    'tests/fixtures/text_expandtabs/warning_contract.py': 'b01539ed6ac3ab126fd90044858bed2d307979a3025bb4b122aceea099970ad9',
    'docs/semantics.md': 'b3cd20a54db54564f46c2c62fff2e7c5852cf5a43d5539af25e2d378267854e7',
    'runtime/RyuPy.Runtime/PyCollectionMethodCall.cs': 'a810d7c354cc3e09f9e3727c6673d41ffa7d093a8f2bcc3316fc730b700270fb',
    'runtime/RyuPy.Runtime/PyPreparedMethods.cs': '131596486ee0444f02f4f730f67f4da40356f5d9d73cd9860a2893bf209c2555',
    'runtime/RyuPy.Runtime/PyStringDescriptors.cs': 'e0479e6c288eb4e84e894455047bb894d2fb9a198f7bc4c7335851bfe0a81a6c',
    'runtime/RyuPy.Runtime/PyStringMethods.cs': '68125a7e2ed7f3da522e581758dc2a57b8dcbbbe2763e6a7b62d7c6da4e6d243',
    'runtime/RyuPy.Runtime/PyWarnings.cs': 'a215e18d18c30a7c1cdc6c155fadc7c26bc1e3954381cec9b36bb14fa59542b8'}
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


def expected_refusal(record, prefix):
    # 非零/超时/信号或任意错误都不够；原stdout前缀与完整Traceback逐流锁定。
    return (record['exit'] == 1 and record['failure'] is None and record['raw_streams_complete']
            and all(prefix.with_suffix('.' + key).stat().st_size == size and
                    sha(prefix.with_suffix('.' + key)) == expected
                    for key, (size, expected) in BOUNDARY_STREAMS.items()))


def plain_probe(entry):
    # 保原入口首个调用逐字，不修改完整入口；只能删已锁定的第二个print。
    lines = entry.read_bytes().splitlines(keepends=True)
    require(sha(entry) == shared.ENTRY_SHA and len(lines) == 5, '原双调用入口改变')
    return b''.join(lines[:4])


class Runner(shared.Runner):
    def __init__(self, expandtabs=False):
        self.expandtabs = expandtabs
        self.out = ROOT / 'target/typing-noreturn-evidence'
        self.out.mkdir(parents=True, exist_ok=False)
        self.labels = set()
        self.result = {'passed': False, 'supported_contract_passed': False, 'full_contract_supported': False, 'default_project_delta': 0, 'stages': {
            x: {'passed': False, 'status': 'not-run'} for x in
            ['csharp', 'tools', 'supported-cp', 'supported-jit', 'supported-native',
             'full-source-deletion-boundary', 'markdown-plain', 'markdown-full']},
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
            detail = operation()
            self.result['stages'][name].update(passed=True, status='passed')
            if isinstance(detail, dict):
                self.result['stages'][name].update(detail)
        except Exception as error:
            self.result['stages'][name].update(status='failed', error=str(error), traceback=traceback.format_exc())
        save(self.out / 'result.json', self.result)
        return self.result['stages'][name]['passed']

    def managed(self, *, build_only=False):
        tests = ROOT / 'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj'
        self.checked('csharp-build', [self.dotnet, 'build', tests, '-c', 'Release', '-nologo'], 600)
        managed = tests.parent / 'bin/Release/net10.0'
        target = self.out / 'managed'; target.mkdir()
        for name in ['RyuPy.Runtime.dll', 'RyuPy.Runtime.Tests.dll']:
            shutil.copy2(managed / name, target / name)
        self.preserve(target, 'csharp-inputs')
        if build_only:
            return
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

    def run_product(self, name, argv, env=None):
        require(name not in self.labels, '禁止重复执行：' + name)
        self.labels.add(name)
        cwd = self.out / 'cwd' / name; cwd.mkdir(parents=True)
        return shared.capture(self.out / name, argv, cwd, getattr(self, 'product_timeout', 30),
                              env or self.env)

    def run_positive(self, name, argv, golden, env=None):
        actual = self.run_product(name, argv, env)
        require(positive(actual, self.out / name, golden), name + '实际exit/stdout/stderr不符；见原流')

    def jit(self, name, entry, golden, inputs=None):
        target = self.out / 'products' / name
        args = (shared.build_command(self.cli, entry, inputs, target) if inputs else
                [self.cli, 'build', entry, '--diagnostic-stages', '--out-dir', target])
        env = dict(self.env)
        if inputs:
            env['PYTHONPATH'] = os.pathsep.join(map(str, [inputs / 'stdlib', inputs / 'markdown']))
        if name not in ('supported', 'full-source-deletion-boundary') and not name.startswith('expandtabs-'):
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
        if name == 'full-source-deletion-boundary':
            record = self.run_product(name + '-jit', [self.dotnet, dll], env)
            require(expected_refusal(record, self.out / (name + '-jit')),
                    '原完整源删除负界不是精确exit1/851B/174B原流')
            return {'status': 'expected_refusal', 'cp_equivalent': False, 'supported': False}
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

    def native(self, entry, golden, *, name=None, timeout=1200):
        # 串行作用域：旧调用目录/label/1200秒完全不变，新入口各自独立归档。
        previous, run_timeout = self.out, getattr(self, 'product_timeout', 30)
        if name is not None:
            self.out = previous / 'native-domains' / name
            self.out.mkdir(parents=True, exist_ok=False)
            self.product_timeout = 15
        labels = getattr(self, 'labels', set())
        if name is not None:
            self.labels = set()
        try:
            if name is not None:
                require(shutil.disk_usage(self.out).free >= 600 * 1024 * 1024, 'Native余量不足600MiB；停止该域，不清理资料')
            return self._native(entry, golden, timeout)
        finally:
            self.out, self.labels, self.product_timeout = previous, labels, run_timeout

    def _native(self, entry, golden, timeout):
        target = self.out / 'native-build'
        built = self.command('native-publish', [self.cli, 'build', entry, '--diagnostic-stages',
            '--out-dir', target, '--aot'], timeout, self.env)
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

    def supported_oracle(self):
        cp = self.command('supported-oracle', [self.python, '-B', ROOT / SUPPORTED], 30, self.env)
        prefix = self.out / 'supported-oracle'
        golden = prefix.with_suffix('.stdout').read_bytes()
        require(len(golden) == 1401 and sha(prefix.with_suffix('.stdout')) == SUPPORTED_ORACLE_SHA
                and positive(cp, prefix, golden), '有限支持合同CP1401B oracle不符')

    def expand_oracle(self, name):
        source_hash, size, digest, encoded = EXPAND_CONTRACTS[name]
        entry = ROOT / ('tests/fixtures/text_expandtabs/' + name + '.py')
        require(sha(entry) == source_hash, 'CP前源码改变')
        label = 'expandtabs-' + name + '-cp'
        require(label not in self.labels, '禁止重复CP')
        self.labels.add(label)
        cwd = self.out / 'cwd' / label; cwd.mkdir(parents=True)
        env = dict(PATH='/usr/bin:/bin', LANG='C.UTF-8', LC_ALL='C.UTF-8',
                   PYTHONNOUSERSITE='1', PYTHONHASHSEED='0', PYTHONUTF8='1', PYTHONDONTWRITEBYTECODE='1')
        record = shared.capture(self.out / label, [self.python, '-B', entry], cwd, 15,
                                env, limit=128 * 1024, memory_limit=512 * 1024 * 1024)
        require(sha(entry) == source_hash and sha(self.python) == shared.PYTHON_SHA, 'CP后身份改变')
        golden = bytes.fromhex(encoded)
        require(len(golden) == size and sha(self.out / (label + '.stdout')) == digest and
                positive(record, self.out / label, golden), '新源CP与冻结原流不符')

    def managed_domain(self, domain):
        typing = domain == 'typing'
        target = self.out / ('trx-' + domain)
        self.checked('csharp-' + domain, [self.dotnet, 'test',
            ROOT / 'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj', '-c', 'Release',
            '--no-build', '--no-restore', '--filter',
            'FullyQualifiedName~' + ('PyTyping' if typing else 'PyTextExpandTabs'),
            '--logger', 'trx;LogFileName=result.trx', '--results-directory', target], 300)
        path = target / 'result.trx'
        if typing:
            detail = verify_trx(path)
        else:
            rows = ET.parse(path).findall('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}UnitTestResult')
            require(rows and all(x.get('outcome') == 'Passed' for x in rows), 'expandtabs测试失败/跳过/零测试')
            actual = collections.Counter(x.attrib['testName'].removeprefix('RyuPy.Runtime.Tests.') for x in rows)
            require(actual == collections.Counter(EXPAND_FACTS), 'expandtabs Fact漏跑/重复/越界')
            detail = {'total': len(rows), 'all_passed': True}
        save(self.out / ('csharp-' + domain + '-results.json'), detail)
        return detail

    def expand_products(self):
        for name, (_, _, _, encoded) in EXPAND_CONTRACTS.items():
            label = 'expandtabs-' + name
            if not self.phase(label + '-cp', lambda name=name: self.expand_oracle(name)):
                continue
            entry = ROOT / ('tests/fixtures/text_expandtabs/' + name + '.py')
            golden = bytes.fromhex(encoded)
            self.product_timeout = 15
            self.phase(label + '-jit', lambda: self.jit(label, entry, golden))
            self.phase(label + '-native', lambda: self.native(entry, golden, name=name, timeout=180))
            self.product_timeout = 30

    def markdown_products(self):
        inputs = self.inputs(self.python)
        full = inputs / 'markdown_sample.py'
        plain = inputs / 'markdown_plain_probe.py'; plain.write_bytes(plain_probe(full))
        expected = (inputs / 'stdout.txt').read_bytes()
        require(len(expected) == 77, '原完整golden不符')
        self.preserve(inputs, 'inputs')
        self.phase('markdown-plain', lambda: self.jit('markdown-plain', plain, expected[:48], inputs))
        self.phase('markdown-full', lambda: self.jit('markdown-full', full, expected, inputs))

    def execute_expandtabs(self):
        # 只增加有依赖的阶段；任一测试域失败不能掩盖其它独立域，最终仍红。
        before = None
        stages = ['csharp-build', 'csharp-typing', 'csharp-expandtabs', 'tools',
                  'supported-cp', 'supported-jit', 'supported-native',
                  'full-source-deletion-boundary', 'markdown-inputs', 'markdown-plain', 'markdown-full']
        stages += ['expandtabs-' + name + '-' + mode for name in EXPAND_CONTRACTS
                   for mode in ('cp', 'jit', 'native')]
        self.result['stages'] = {x: {'passed': False, 'status': 'not-run'} for x in stages}
        self.result.update(scope='expandtabs三同源合同、NoReturn原保护及Markdown两阶段；非全库验收',
                           not_run=['expandtabs boundaries.py', '原完整NoReturn CP/Native重跑',
                                    '性能及Markdown全库；默认项目晋升不由本runner判定'])
        try:
            event = json.loads(Path(os.environ['GITHUB_EVENT_PATH']).read_text())
            require(event.get('head_commit', {}).get('message') == EXPAND_TITLE, '新精确标题不符')
            adjusted = dict(event, head_commit=dict(event['head_commit'], message=TITLE))
            check_context(os.environ, adjusted)
            before = self.identity('before')
            require(before['head'] == os.environ['GITHUB_SHA'], '真实HEAD不符')
            self.checked('direct-parent', ['git', 'rev-parse', 'HEAD^', 'HEAD^{tree}', 'HEAD^^{tree}'], 20)
            parent, tree, parent_tree = (self.out / 'direct-parent.stdout').read_text().splitlines()
            require((parent, parent_tree) == (EXPAND_PARENT, EXPAND_TREE) and tree == before['tree'],
                    '真实直接父/tree不符；不得查询grandparent或私有历史')
            self.checked('expand-base-files', ['git', 'ls-tree', '-r', '-z', parent_tree], 20)
            base = shared.tree_records((self.out / 'expand-base-files.stdout').read_bytes())
            changed = {p for p in base.keys() | before['files'].keys() if base.get(p) != before['files'].get(p)}
            require(changed <= set(EXPAND_FROZEN) | CI_PATHS, '出现候选外差异')
            frozen = dict(FROZEN, **EXPAND_FROZEN)
            require(all(sha(ROOT / p) == digest for p, digest in frozen.items()), '源码冻结hash不符')
            self.frozen.update({str(ROOT / p): digest for p, digest in frozen.items()})
            save(self.out / 'source-identity.json', dict(before, direct_parent=parent,
                 direct_parent_tree=parent_tree, frozen_paths=frozen, changed=sorted(changed)))
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
                 'sha256': {name: sha(path) for name, path in tools.items()}, 'feature': shared.FEATURE})
            saved = self.out / 'sources'; saved.mkdir()
            for name in (CONTRACT, SUPPORTED, *('tests/fixtures/text_expandtabs/' + x + '.py'
                                               for x in EXPAND_CONTRACTS)):
                shutil.copy2(ROOT / name, saved / Path(name).name)
            self.preserve(saved, 'contract-sources')
            save(self.out / 'full-contract-reference.json', {
                'status': 'not-run', 'source': CONTRACT, 'source_sha256': FROZEN[CONTRACT],
                'reference_run': 42754725069, 'python_sha256': shared.PYTHON_SHA,
                'cp_exit': 0, 'cp_stdout_bytes': 1482, 'cp_stdout_sha256': ORACLE_SHA,
                'native_previous_exit': 1, 'native_previous_relocated': False,
                'note': '历史完整源CP及Native不重跑；本次JIT删除负界不等于完整源支持。'})
            if not self.phase('csharp-build', lambda: self.managed(build_only=True)):
                return 1
            self.phase('csharp-typing', lambda: self.managed_domain('typing'))
            self.phase('csharp-expandtabs', lambda: self.managed_domain('expandtabs'))
            if not self.phase('tools', self.build_tools):
                return 1
            if self.phase('supported-cp', self.supported_oracle):
                golden = (self.out / 'supported-oracle.stdout').read_bytes()
                self.phase('supported-jit', lambda: self.jit('supported', ROOT / SUPPORTED, golden))
                self.phase('supported-native', lambda: self.native(ROOT / SUPPORTED, golden))
            self.result['supported_contract_passed'] = all(self.result['stages'][n]['passed'] for n in
                ('csharp-build', 'csharp-typing', 'tools', 'supported-cp', 'supported-jit', 'supported-native'))
            self.phase('full-source-deletion-boundary',
                       lambda: self.jit('full-source-deletion-boundary', ROOT / CONTRACT, None))
            self.expand_products()
            self.phase('markdown-inputs', self.markdown_products)
            self.result['passed'] = all(x['passed'] for x in self.result['stages'].values())
        except Exception as error:
            self.result.update(error=str(error), traceback=traceback.format_exc())
        finally:
            try:
                require(before is not None and self.identity('after') == before, '源码身份改变或未核验')
                require(all(sha(Path(p)) == h for p, h in self.frozen.items()), '工具/源/产物身份改变')
                self.result['identity_preserved'] = True
            except Exception as error:
                self.result.update(passed=False, supported_contract_passed=False,
                                   identity_preserved=False, identity_error=str(error))
            save(self.out / 'result.json', self.result)
        return 0 if self.result['passed'] else 1

    def execute(self):
        if self.expandtabs:
            return self.execute_expandtabs()
        before = None
        try:
            check_context(os.environ, json.loads(Path(os.environ['GITHUB_EVENT_PATH']).read_text()))
            before = self.identity('before')
            require(before['head'] == os.environ['GITHUB_SHA'], '候选HEAD不符')
            self.checked('base-files', ['git', 'ls-tree', '-r', '-z', BASE_TREE], 20)
            base = shared.tree_records((self.out / 'base-files.stdout').read_bytes())
            changed = {p for p in base.keys() | before['files'].keys() if base.get(p) != before['files'].get(p)}
            require(changed <= set(FROZEN) | CI_PATHS, '出现候选外差异')
            require(all(sha(ROOT / p) == h for p, h in FROZEN.items()), '冻结15路径hash不符')
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
            # 保原完整源及新有限支持源的确切内容；无publicdict/模块删除旁路。
            saved = self.out / 'sources'; saved.mkdir()
            for name in (CONTRACT, SUPPORTED):
                shutil.copy2(ROOT / name, saved / Path(name).name)
            self.preserve(saved, 'contract-sources')
            save(self.out / 'full-contract-cp-reference.json', {
                'status': 'not-run', 'reference_run': 42754725069,
                'reference_remote_head': REMOTE_BASE_HEAD, 'source': CONTRACT,
                'source_sha256': FROZEN[CONTRACT], 'python_sha256': shared.PYTHON_SHA,
                'stdout_bytes': 1482, 'stdout_sha256': ORACLE_SHA, 'stderr_bytes': 0, 'exit': 0,
                'note': '本次不重跑原CP；保既有同版本同源oracle，不截断为成功输出。'})
            save(self.out / 'full-contract-native-reference.json', {
                'status': 'not-run', 'reference_run': 42754725069, 'source': CONTRACT,
                'reference_remote_head': REMOTE_BASE_HEAD, 'previous_status': 'failed',
                'previous_publish_exit': 0, 'previous_native_exit': 1, 'previous_relocated': False,
                'note': '旧完整源Native删除失败原件保留；本次唯一Native仅验新支持源，不覆写旧失败。'})
            if self.phase('supported-cp', self.supported_oracle):
                golden = (self.out / 'supported-oracle.stdout').read_bytes()
                self.phase('supported-jit', lambda: self.jit('supported', ROOT / SUPPORTED, golden))
                self.phase('supported-native', lambda: self.native(ROOT / SUPPORTED, golden))
            self.result['supported_contract_passed'] = all(self.result['stages'][name]['passed']
                for name in ('csharp', 'tools', 'supported-cp', 'supported-jit', 'supported-native'))
            self.phase('full-source-deletion-boundary',
                lambda: self.jit('full-source-deletion-boundary', ROOT / CONTRACT, None))
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
                self.result.update(passed=False, supported_contract_passed=False, identity_preserved=False, identity_error=str(error))
            save(self.out / 'result.json', self.result)
        return 0 if self.result['passed'] else 1


if __name__ == '__main__':
    raise SystemExit(Runner(expandtabs=os.environ.get('RYUPY_VALIDATION_MODE') == 'text-expandtabs').execute())
