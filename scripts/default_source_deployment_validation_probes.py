"""默认来源生产probe接线：两Rust策略、精确C# Fact及单个新鲜metadata宿主，均保独立原流。"""
import collections
import json
from pathlib import Path
import re
import shutil
import xml.etree.ElementTree as ET
import markdown_closure_probe as shared

require, sha, save = shared.require, shared.sha, shared.save
RUST_DRIVER = (
    'default_source_policy_explicit_off_aot_fixed_and_token',
    'default_source_soft_parse_rejection_retains_caller_and_remaps',
    'default_source_soft_to_hard_catalog_dependency_fixed_point',
    'default_source_hint_cycle_and_native_parent_are_inactive',
    'default_source_only_tagged_failure_is_deferred',
    'default_source_metadata_full_inventory_auto_failure_explicit_override',
    'default_source_final_deployment_rejects_overwrite',
    'default_source_actual_support_boundary_not_unknown_hir',
    'default_source_nested_hints_metadata_and_alias_sets_reach_fixed_point',
)
RUST_ARCHIVE = ('default_source_archive_fixed_members_and_corruption',)
RUST_EXTRA = {
    'module_resolver': ('modules::module_resolver::tests::', 'default_source_resolver_never_borrows_another_root_child'),
    'source_catalog': ('source_catalog::tests::', 'default_source_embedded_catalog_and_license_are_locked'),
    'module_hints': ('module_hints::tests::', 'default_source_alias_candidates_are_not_overwritten'),
}
RUST_NAMES = tuple('default_source_tests::' + name for name in RUST_DRIVER) + tuple(
    'source_catalog::archive::tests::' + name for name in RUST_ARCHIVE) + tuple(
    prefix + name for prefix, name in RUST_EXTRA.values())
CS_CLASS = 'RyuPy.Runtime.Tests.PyDefaultSourceCandidatesTests'
CS_METHOD = 'DefaultSourceCandidateActivationPreservesCacheNativeAndFailurePhases'
CS_NAMES = (CS_CLASS + '.' + CS_METHOD,)
HOST_SOURCE = 'tests/validation/default_source_hosts/AutoUnavailable.cs'
HOST_PROJECT = 'tests/validation/source_provenance/host/Probe.csproj'
STAGES = ('rust-default', 'rust-pilot', 'runtime-facts', 'metadata-fresh-host')


def source_manifest(root):
    """只核真实源的显式名单，不把自动发现到的任意新测试当作固定覆盖。"""
    paths = {'crates/ryupy-driver/src/default_source_tests.rs': RUST_DRIVER,
             'crates/ryupy-driver/src/source_catalog_archive.rs': RUST_ARCHIVE}
    paths.update({'crates/ryupy-driver/src/' + source + '.rs': (name,)
                  for source, (_, name) in RUST_EXTRA.items()})
    for name, expected in paths.items():
        actual = re.findall(r'#\[test\]\s*fn (default_source_\w+)\(', (root / name).read_text())
        require(collections.Counter(actual) == collections.Counter(expected), '生产Rust probe名单改变：' + name)
    cs = 'runtime/RyuPy.Runtime.Tests/PyDefaultSourceCandidatesTests.cs'
    require(re.findall(r'\[Fact\]\s+public void (\w+)\(', (root / cs).read_text()) == [CS_METHOD],
            'C#生产probe Fact名单改变')
    files = [*paths, cs, HOST_SOURCE, HOST_PROJECT]
    return {'rust_default': RUST_NAMES, 'rust_pilot': RUST_NAMES, 'runtime_facts': CS_NAMES,
            'fresh_host_stdout': 'import-ok\nrequire-ni\nhooks-ni\n',
            'files': {name: sha(root / name) for name in files}}


def verify_rust(stdout):
    rows = re.findall(r'^test (\S+) \.\.\. (\S+)\s*$', stdout, re.M)
    require(collections.Counter(rows) == collections.Counter((name, 'ok') for name in RUST_NAMES),
            'Rust probe零测试/漏项/重复/失败/ignored/越界')
    summaries = re.findall(r'^test result: (.*)$', stdout, re.M)
    require(len(summaries) == 1 and re.fullmatch(
        r'ok\. 13 passed; 0 failed; 0 ignored; 0 measured; \d+ filtered out; finished in .+', summaries[0]),
        'Rust probe汇总不是精确13/0/0')
    return {'passed': True, 'tests': list(RUST_NAMES), 'total': len(rows), 'ignored': 0}


def verify_trx(path):
    ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    root = ET.parse(path).getroot()
    rows = root.findall('.//t:UnitTestResult', ns)
    require(collections.Counter(row.get('testName') for row in rows) == collections.Counter(CS_NAMES)
            and all(row.get('outcome') == 'Passed' for row in rows), 'C#零测试/漏名/重复/失败/skip/越界')
    definitions = root.findall('.//t:TestDefinitions/t:UnitTest', ns)
    require(len(definitions) == len(rows) == 1, 'C#真实测试定义数量不符')
    method = definitions[0].find('t:TestMethod', ns)
    require(method is not None and method.get('className') == CS_CLASS and method.get('name') == CS_METHOD
            and definitions[0].get('id') == rows[0].get('testId'), 'C#结果与实际方法定义不匹配')
    counters = root.find('.//t:ResultSummary/t:Counters', ns)
    require(counters is not None and all(counters.get(key) == '1' for key in ('total', 'executed', 'passed'))
            and all(counters.get(key, '0') == '0' for key in ('failed', 'error', 'timeout', 'aborted',
                'inconclusive', 'notExecuted', 'disconnected', 'passedButRunAborted', 'notRunnable')),
            'TRX汇总非单次真实通过')
    return {'passed': True, 'tests': list(CS_NAMES), 'total': 1, 'skipped': 0}


def execution_identity(path, prefix=None):
    """仅执行输入的读取/哈希取证属于硬身份边界；不吞测试失败、TRX或业务错误。"""
    path = Path(path)
    try:
        resolved = path.resolve(strict=True)
        digest = sha(path)
        if prefix is not None:
            with path.open('rb') as stream:
                if stream.read(len(prefix)) != prefix:
                    raise shared.IdentityError('实际执行文件格式不符：' + str(path))
    except OSError as error:
        raise shared.IdentityError('实际执行输入无法取证：' + str(path)) from error
    return {'path': str(resolved), 'sha256': digest}


def execution_snapshot(directory):
    """成功构建后的完整执行目录不可读取，不得降格为普通业务失败继续执行。"""
    try:
        return shared.snapshot(directory)
    except OSError as error:
        raise shared.IdentityError('实际执行目录无法取证：' + str(directory)) from error


def materialize_input(source, target):
    try:
        shutil.copyfile(source, target)
    except OSError as error:
        raise shared.IdentityError('实际host输入无法物化：' + str(source)) from error
    source_id, target_id = execution_identity(source), execution_identity(target)
    if source_id['sha256'] != target_id['sha256']:
        raise shared.IdentityError('host物化源码身份改变：' + str(target))
    return target_id


def run_command(runner, name, argv, timeout, env=None):
    runner.barrier()
    record = runner.command(name, argv, timeout, env)
    runner.barrier()
    require(shared.successful(record), name + '正常exit0/完整双流失败')
    return record


def execute(runner, name):
    root, out = shared.REPO, runner.out
    # 使用同HEAD的真实命令，单位测试与用户JIT分账，不拿前者替代后者。
    if name in ('rust-default', 'rust-pilot'):
        argv = ['cargo', 'test', '--locked', '-p', 'ryupy-driver', '--lib']
        if name == 'rust-pilot': argv += ['--features', shared.old.FEATURE]
        argv += ['default_source_', '--', '--nocapture', '--test-threads=1']
        run_command(runner, name, argv, 900)
        save(out / (name + '-tests.json'), verify_rust((out / (name + '.stdout')).read_text()))
    elif name == 'runtime-facts':
        project = root / 'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj'
        run_command(runner, 'runtime-probe-build', [runner.dotnet, 'build', project,
                    '-c', 'Release', '-nologo', '-v', 'q'], 600)
        actual = project.parent / 'bin/Release/net10.0/RyuPy.Runtime.dll'
        actual_id = execution_identity(actual)
        if actual_id['sha256'] != execution_identity(runner.runtime)['sha256']:
            raise shared.IdentityError('C#测试实际Runtime异源')
        for path in (actual, actual.parent / 'RyuPy.Runtime.Tests.dll'):
            runner.frozen[str(path)] = execution_identity(path)['sha256']
        trx = out / 'runtime-probe-trx'
        run_command(runner, name, [runner.dotnet, 'test', project, '-c', 'Release', '--no-build',
            '--no-restore', '--filter', 'FullyQualifiedName~' + CS_CLASS,
            '--logger', 'trx;LogFileName=default-source.trx', '--results-directory', trx], 300)
        save(out / 'runtime-facts-tests.json', verify_trx(trx / 'default-source.trx'))
        runner.frozen[str(trx / 'default-source.trx')] = sha(trx / 'default-source.trx')
        save(out / 'runtime-facts-runtime.json', execution_identity(actual))
    elif name == 'metadata-fresh-host':
        target = out / 'metadata-fresh-host'; target.mkdir()
        # 复用既有最小Reference项目，仅把本probe放到该项目固定Program.cs输入位置。
        for source, dest in [(HOST_SOURCE, 'Program.cs'), (HOST_PROJECT, 'Probe.csproj')]:
            runner.frozen[str(target / dest)] = materialize_input(root / source, target / dest)['sha256']
        binary = target / 'bin'
        run_command(runner, 'metadata-host-build', [runner.dotnet, 'build', target / 'Probe.csproj',
            '-c', 'Release', '-nologo', '-o', binary, '-p:RuntimePath=' + str(runner.runtime)], 300)
        actual = binary / 'RyuPy.Runtime.dll'
        actual_id, host_id = execution_identity(actual), execution_identity(binary / 'Probe.dll')
        if actual_id['sha256'] != execution_identity(runner.runtime)['sha256']:
            raise shared.IdentityError('fresh host实际Runtime异源')
        runner.frozen.update({str(binary / path): row['sha256'] for path, row in execution_snapshot(binary).items()})
        save(out / 'metadata-host-runtime.json', {**actual_id,
             'host_path': host_id['path'], 'host_sha256': host_id['sha256']})
        run_command(runner, 'metadata-host-run', [runner.dotnet, binary / 'Probe.dll'], 30, runner.env)
        require((out / 'metadata-host-run.stdout').read_bytes() == b'import-ok\nrequire-ni\nhooks-ni\n'
                and (out / 'metadata-host-run.stderr').read_bytes() == b'', 'fresh host精确完整原流不符')
    else:
        raise ValueError('未知生产probe阶段：' + name)
