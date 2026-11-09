"""hosted早期路径门：同源C#构建/定向测试、原include反例、四路径案与empty-from JIT；不替代完整门。"""
import collections
import json
import os
from pathlib import Path
import shutil
import sys
import traceback
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / 'tests'))
from run_projects_gate import candidate_identity
sys.path.insert(0, str(ROOT / 'tests/aot/pyfiglet_ci'))
sys.path.insert(0, str(ROOT / 'tests/aot'))
from guards import require, save, sha, reported_managed
from run import Recorder, base_environment

# 以方法和参数行数锁定新44条测试，防止过滤器拼错/zero-tests冒充通过。
NEW_TESTS = {
    'HostRegistrationCannotEscapeParentWithPathComponentsInChildName': 1,
    'EmptyExactSequencesRejectFreshResourceChildWithoutPublishing': 2,
    'EqualNewSequencesAndRepeatedDirectoriesAllowFreshChild': 4,
    'MissingPathPreservesOriginalAttributeErrorAndOuterHandler': 1,
    'RealPathGetterFailurePrecedesHooksAndKeepsIdentity': 1,
    'FinderConfigurationPrecedesEmptyPathNegativeResult': 3,
    'UnknownPathsRemainExplicitWithoutUserProtocolsOrFalseAbsence': 1,
    'IncoherentChildIsUnsupportedEvenWhenThePathIsEmpty': 2,
    'SourceNonPackageFirstReadsPathBeforeClassifyingItsSource': 1,
    'PublicMetadataChangesDoNotChooseOrRejectTheSource': 1,
    'InitializingAndParentImportedTargetsBypassChangedPathsAndLaterHooks': 1,
    'FailedChildRetainsDependencyAndRereadsPathBeforeRetry': 1,
    'ParentFailurePrecedesNewPathAndFinderChecks': 1,
    'CachedParentUsesItsOwnLocationAfterNameReregistration': 1,
    'ParentInitializationSelectsTheLatestChildRegistration': 1,
    'InstanceParentAndChildUseTheSameActualDictionaryPath': 1,
    'KindZeroAndOldResourceParentsKeepTheirClosedWorldImports': 1,
    'LegacyAndLocationlessChildrenDoNotAddSourceParentChecks': 1,
    'UnknownNamesKeepTheOldClosedWorldBranchWithoutNewGetters': 1,
    'FindingThroughUncachedIntermediateParentsReadsEachPathOnce': 1,
    'IntermediateParentChecksAreNotRepeatedWhenTheSecondGetterWouldFail': 1,
    'GetterImportedIntermediateParentIsReusedByTheFinalCacheCheck': 1,
    'DiscoveryKeepsItsOwnStrictPathDomainAndMessage': 1,
    'DiscoveryParentGetterFailureKeepsIdentityAndDoesNotExecuteIntermediate': 1,
    'RelativeFromlistKeepsTwoPhasesWithoutDoublingChildPathReads': 1,
    'EmptyPathBecomesSourceImportErrorWithoutRunningChild': 2,
    'MatchingBodyFailureLeavesHandlerBeforeFinalError': 2,
    'UnrelatedOrNamelessBodyFailureKeepsOriginalIdentity': 3,
    'PublishedParentValueWinsAfterMatchingBodyFailure': 1,
    'SecondAttributeReadKeepsFailureAndOuterHandler': 1,
    'SecondAttributeMissCanReturnTheExistingCacheWithoutRestartingImport': 1,
    'NameDescriptorRemainsExplicitAndRestoresOuterHandler': 1,
    'KindZeroKeepsClosedWorldAndOldDiagnosticFormat': 1,
}
OLD_CLASSES = ('PyImportlibUtilTests', 'PyModuleRelativeImportTests',
               'PyModuleRegistrationVersionTests', 'PyLocationlessModuleMetadataTests',
               'PyModuleCacheAliasTests')

# 旧类同样锁每个方法的实际case行数，不能只凭每类一个结果冒称完整覆盖。
OLD_TESTS = {
    'PyImportlibUtilTests.StableFunctionIdentityAndContainerAliasUseTheSameSignature': 1,
    'PyImportlibUtilTests.FindingTopLevelDoesNotRunOrCacheTargetAndReturnsFreshEquivalentSpecs': 1,
    'PyImportlibUtilTests.ParentRunsBeforeLookupButTargetDoesNotRunOrBindToParent': 1,
    'PyImportlibUtilTests.PackageDiscoveryAllocatesIndependentListsWithoutRunningThePackage': 1,
    'PyImportlibUtilTests.ParentImportingTargetStillReturnsFreshFinderSpec': 1,
    'PyImportlibUtilTests.CachedSpecReturnsCurrentArbitraryValueAndRelativeErrorsKeepOriginalName': 1,
    'PyImportlibUtilTests.CachedGetterErrorsOtherThanAttributeErrorKeepTheirIdentity': 1,
    'PyImportlibUtilTests.RelativeNamesResolveAndAbsoluteNamesIgnorePackageObject': 1,
    'PyImportlibUtilTests.NonPackageParentHasExactNameAndAttributeErrorCause': 1,
    'PyImportlibUtilTests.UnknownCompiledDomainAndLegacyMetadataFailExplicitlyWithoutExecutingTarget': 1,
    'PyImportlibUtilTests.ParentPathMutationIsRejectedOnlyWhenLookupIsNeeded': 1,
    'PyImportlibUtilTests.ParentExceptionPropagatesAndAllowsARealRetry': 1,
    'PyImportlibUtilTests.ChangedAncestorPathStopsUncachedIntermediateParentBeforeItsBody': 1,
    'PyImportlibUtilTests.CachedIntermediateParentDoesNotRecheckItsAncestorPath': 1,
    'PyImportlibUtilTests.NativeParentWithoutSourceMetadataIsNotMisreportedAsANonPackage': 1,
    'PyImportlibUtilTests.NonPackageAncestorStopsIntermediateBodyAtTheOrdinaryImportPhase': 1,
    'PyImportlibUtilTests.ConvertedAttributeErrorsPreserveTheOuterExceptionChain': 1,
    'PyModuleRelativeImportTests.MetadataUsesTargetGlobalsAndPreservesErrorPhases': 1,
    'PyModuleRelativeImportTests.FromlistCompletesBeforeAnyAttributeBinding': 1,
    'PyModuleRelativeImportTests.FailedChildRetainsEarlierDependencyAndCanRetry': 1,
    'PyModuleRelativeImportTests.NonPackageMissingExportDiffersFromUnknownPackageDiscovery': 1,
    'PyModuleRelativeImportTests.HookIsCheckedBeforeMetadataAndDependencies': 1,
    'PyModuleRelativeImportTests.FromlistReadsCurrentNameAndPathAfterPreviousChild': 1,
    'PyModuleRelativeImportTests.ParentReentryUsesPartialCachedChildWithoutSecondExecution': 1,
    'PyModuleRelativeImportTests.CachedChildDoesNotConsumeModifiedPath': 1,
    'PyModuleRelativeImportTests.MatchingMissingNameDefersFailureUntilAttributePhase': 1,
    'PyModuleRelativeImportTests.SharedNameReadHasNoneDefaultAndRejectsDescriptorsWithoutCallbacks': 1,
    'PyModuleRegistrationVersionTests.LegacyResourceEntryKeepsMissingMetadataAndResourceReads': 2,
    'PyModuleRegistrationVersionTests.LegacyOverridesAreRejectedInOriginalGetterOrder': 2,
    'PyModuleRegistrationVersionTests.LegacyGetterExceptionIsNotConvertedOrSwallowed': 1,
    'PyLocationlessModuleMetadataTests.InitialValuesAndSlotMutationDoNotInventSource': 2,
    'PyLocationlessModuleMetadataTests.CircularPackageRetryCreatesFreshPathAndKeepsSuccessfulDependency': 1,
    'PyLocationlessModuleMetadataTests.RealSourceLocationKeepsSpecsAndResourceCapability': 1,
    'PyLocationlessModuleMetadataTests.ManualSourceRegistrationWithoutSlotsKeepsInitialFallback': 2,
    'PyLocationlessModuleMetadataTests.OldKindZeroAndUnregisteredNamesDoNotGainMetadata': 1,
    'PyModuleCacheAliasTests.NativeAliasSharesIdentityAndCurrentAttributesWithoutReinitialization': 1,
    'PyModuleCacheAliasTests.SourceAndInstanceModulesKeepOriginalMetadata': 2,
    'PyModuleCacheAliasTests.UnexecutedPublicCloneStaysEmptyUntilExplicitLoaderExecution': 1,
    'PyModuleCacheAliasTests.EmptyModuleAndExplicitExecutionFailureKeepOriginalDictionaryAndCache': 1,
    'PyModuleCacheAliasTests.InitializingAndEscapedFailedModulesCannotBePublishedUnderAliases': 1,
    'PyModuleCacheAliasTests.UnsupportedStringKeysFailBeforeMutation': 5,
    'PyModuleCacheAliasTests.NonStringKeysAndStringSubclassDoNotRunProtocols': 1,
    'PyModuleCacheAliasTests.NonModuleValuesFailWithoutInspectingObjectsOrReservingKeys': 1,
    'PyModuleCacheAliasTests.ExistingRegistrationsCachesAndAliasesRejectEvenSameObjectWrites': 1,
    'PyModuleCacheAliasTests.RegisteredPackagePrefixesAreReservedButTextPrefixesAreNot': 1,
    'PyModuleCacheAliasTests.NonIdentifierAndOrdinalDistinctKeysRemainValid': 1,
    'PyModuleCacheAliasTests.DeletionRejectsPresentAbsentAndCallbackKeysWithoutChangingCache': 1,
    'PyModuleCacheAliasTests.SavedSettersAndSeparateViewsShareCacheWhileReadListsStaySnapshots': 1,
    'PyModuleCacheAliasTests.MultipleAliasesKeepOneTargetAndDoNotIntroduceAnEvictionLimit': 1,
    'PyModuleCacheAliasTests.EveryRegistrationEntryRejectsAliasNamesAndDescendants': 7,
    'PyModuleCacheAliasTests.DeclarationPreflightRejectsDescendantsBeforeGeneratedSlotWrites': 1,
    'PyModuleCacheAliasTests.RegistrationAliasGuardPrecedesLocationValidation': 4,
    'PyModuleCacheAliasTests.ExistingPackageChildAttributeWorksButAliasSubmoduleImportDoesNot': 1,
    'PyModuleCacheAliasTests.FinderRejectsAliasDescendantsBeforeParentPathGetterCallbacks': 1,
    'PyModuleCacheAliasTests.FromMissingAttributePreservesOriginalMissingAndOuterContext': 1,
    'PyModuleCacheAliasTests.ExistingGetterFailuresRemainOriginalAndNonAliasMissingImportIsUnchanged': 1,
    'PyModuleCacheAliasTests.FindSpecReadsTheCurrentOriginalObjectAndPreservesMissingSemantics': 1,
    'PyModuleCacheAliasTests.AssignmentAndCacheHitNeverProbeModuleAttributes': 1,
    'PyModuleCacheAliasTests.AllNewRejectionsCaptureCallerFramesAndActiveException': 1,
    'PyModuleCacheAliasTests.NormalRegistrationReplacementAndInitializationFailureRetryRemainAvailable': 1,
    'PyModuleCacheAliasTests.SetterKeywordArgumentsFailBeforePublishingOrReservingAlias': 6,
}


def verify_tests(path):
    rows = ET.parse(path).findall('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}UnitTestResult')
    require(rows and all(row.get('outcome') == 'Passed' for row in rows), 'C#存在失败/跳过/无测试')
    names = [row.attrib['testName'].split('(', 1)[0] for row in rows]
    new = collections.Counter(name.rsplit('.', 1)[-1] for name in names if '.PySourceImport' in name)
    require(dict(new) == NEW_TESTS, '新44条测试未全部实际运行')
    old = collections.Counter('.'.join(name.split('.')[-2:]) for name in names if '.PySourceImport' not in name)
    require(dict(old) == OLD_TESTS, '旧模块回归方法/case计数不符')
    return {'total': len(rows), 'new': sum(new.values()), 'old': sum(old.values()), 'all_passed_zero_skips': True}


def verify_result(actual, case):
    require(actual.returncode == case['expected_exit'], '错误退出/信号/异常成功：' + case['name'])
    expected = case['expected_stdout']
    if 'stdout_source_root' in case:
        expected = expected.replace('{SOURCE_ROOT}', str(ROOT / case['stdout_source_root']))
    require(actual.stdout == expected.encode(), 'stdout不符：' + case['name'])
    if case['expected_exit'] == 0:
        require(not actual.stderr, '正臂stderr非空：' + case['name'])
    else:
        lines = actual.stderr.decode().rstrip().splitlines()
        require(lines and lines[-1] == case['expected_terminal_error'], '原反例不是正常MNFE')


def verify_graph(path, expected):
    graph = json.loads(path.read_text())
    rows = graph['modules']
    require(graph['compiled_feature'] is True and graph['session_policy'] == 'ordinary-dynamic-module-pilot',
            '构建feature/policy不符')
    require(sorted(row['module'] for row in rows) == sorted(expected), '实际模块图不符')
    require(all(row['compiled'] and row['execution'] == 'LegacySingleton' for row in rows), '本组普通图执行模式改变')


def identity(path):
    path = Path(path).resolve(strict=True)
    return {'path': str(path), 'bytes': path.stat().st_size, 'sha256': sha(path)}


def execute(out, run):
    require(os.environ.get('GITHUB_ACTIONS') == 'true' and
            os.environ.get('RUNNER_ENVIRONMENT') == 'github-hosted', '只允许hosted，禁止本地隐式构建')
    require(sys.version_info[:3] == (3, 12, 13), '必须CP3.12.13')
    before = candidate_identity(ROOT)
    require(not before['status'] and before['head'] == os.environ['GITHUB_SHA'], '必须同HEAD clean source')
    save(out / 'source-before.json', before)
    contract = json.loads((HERE / 'fixtures.json').read_text())
    require(contract['python_version'] == '3.12.13' and len(contract['cases']) == 6, '固定六案改变')
    for case in contract['cases']:
        require(all(sha(ROOT / name) == value for name, value in case['files'].items()), 'fixture不再原样')
    dotnet = Path(os.environ['DOTNET']).resolve(strict=True)
    runtime = ROOT / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'
    tests = ROOT / 'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj'
    cli = ROOT / 'target/debug/ryupy'
    env = base_environment(dotnet, runtime)
    tool_paths = [dotnet, Path(sys.executable), Path(shutil.which('cargo')), Path(shutil.which('rustc'))]
    tools = [identity(path) for path in tool_paths]
    save(out / 'tools-before.json', tools)
    frozen = {}
    try:
        run.run('runtime-test-build', [dotnet, 'build', tests, '-c', 'Release', '-nologo'],
                cwd=ROOT, env=env, check=True, timeout=600)
        managed = ROOT / 'runtime/RyuPy.Runtime.Tests/bin/Release/net10.0'
        saved = out / 'managed-inputs'; saved.mkdir()
        for name in ('RyuPy.Runtime.dll', 'RyuPy.Runtime.Tests.dll'):
            source = managed / name; frozen[str(source)] = sha(source)
            shutil.copyfile(source, saved / name)
            require(sha(saved / name) == frozen[str(source)], 'C#输入复制漂移')
            frozen[str(saved / name)] = sha(saved / name)
        require(sha(runtime) == sha(managed / 'RyuPy.Runtime.dll'), 'runtime构建role不一致')
        frozen[str(runtime)] = sha(runtime)
        save(out / 'managed-identities.json', {'runtime': identity(runtime),
             'originals': [identity(managed / name) for name in ('RyuPy.Runtime.dll', 'RyuPy.Runtime.Tests.dll')],
             'saved': [identity(saved / name) for name in ('RyuPy.Runtime.dll', 'RyuPy.Runtime.Tests.dll')]})
        test_filter = '|'.join('FullyQualifiedName~' + name for name in ('PySourceImport',) + OLD_CLASSES)
        trx = out / 'trx'
        run.run('runtime-tests', [dotnet, 'test', tests, '-c', 'Release', '--no-build', '--no-restore',
                '--filter', test_filter, '--logger', 'trx;LogFileName=source-import.trx',
                '--results-directory', trx], cwd=ROOT, env=env, check=True, timeout=300)
        save(out / 'runtime-tests.json', verify_tests(trx / 'source-import.trx'))
        run.run('cli-build', ['cargo', 'build', '--locked', '-p', 'ryupy-cli', '--features',
                'ryupy-driver/ordinary-dynamic-module-pilot'], cwd=ROOT, env=env, check=True, timeout=1200)
        frozen[str(cli)] = sha(cli)
        save(out / 'compiled-tools.json', {'source_head': before['head'], 'feature':
             'ryupy-driver/ordinary-dynamic-module-pilot', 'configuration': 'debug',
             'files': {path: identity(path) for path in frozen},
             'budget': {'csharp_build': 1, 'csharp_test': 1, 'cli_build': 1, 'cp': 6, 'pe_build': 7, 'jit': 7}})

        def cwd(label):
            path = out / 'cwd' / label; path.mkdir(parents=True)
            require(not any(path.iterdir()), 'cwd不空'); return path

        for case in contract['cases']:
            name, entry = case['name'], ROOT / case['entry']
            actual = run.run(name + '-cp', [sys.executable, '-B', '-S', entry],
                             cwd=cwd(name + '-cp'), env=env, timeout=15)
            verify_result(actual, case)
            arms = (False, True) if name == 'parent_path_first' else (False,)
            for explicit in arms:
                label = name + ('-include' if explicit else '-original-graph')
                target = out / 'products' / label
                require(not target.exists(), '产物根已存在')
                args = [cli, 'build', entry, '--diagnostic-stages', '--out-dir', target]
                if explicit: args += ['--include-module', 'pkg.data']
                built = run.run(label + '-build', args, cwd=cwd(label + '-build'), env=env, check=True, timeout=120)
                dll = reported_managed(built.stderr, target)
                expected = case['graph'] + (['pkg.data'] if explicit else [])
                verify_graph(target / 'module-environment.json', expected)
                require(sha(dll.parent / 'RyuPy.Runtime.dll') == frozen[str(runtime)], 'PE Runtime不符')
                artifacts = {str(path.relative_to(out)): identity(path) for path in target.rglob('*') if path.is_file()}
                save(out / (label + '-artifacts.json'), artifacts)
                actual = run.run(label + '-jit', [dotnet, dll], cwd=cwd(label + '-jit'), env=env, timeout=20)
                verify_result(actual, case)
                require(all(sha(out / path) == value['sha256'] for path, value in artifacts.items()), 'JIT产物变化')
                save(out / (label + '-result.json'), {'passed': True, 'explicit_include': explicit,
                     'source_head': before['head'], 'exit': actual.returncode})
    finally:
        after = candidate_identity(ROOT); save(out / 'source-after.json', after)
        tool_after = [identity(path) for path in tool_paths]; save(out / 'tools-after.json', tool_after)
        checks = {path: sha(Path(path)) == value for path, value in frozen.items()}
        save(out / 'unchanged.json', checks)
        require(before == after and tools == tool_after and all(checks.values()), '源码或工具前后改变')


def main():
    out = ROOT / 'target/source-import-path-evidence'; out.mkdir(parents=True, exist_ok=False)
    run = Recorder(out / 'logs')
    report = {'passed': False, 'scope': '仅C#定向和原反例/四路径案/empty-from JIT；完整20/fullcheck/Native由原jobs独立保护'}
    try:
        execute(out, run); report['passed'] = True
    except Exception as error:
        report.update(error=str(error), traceback=traceback.format_exc()); raise
    finally:
        save(out / 'result.json', report)
        save(out / 'stream-identities.json', {str(p.relative_to(out)): identity(p)
             for p in (out / 'logs').iterdir() if p.suffix in ('.stdout', '.stderr')})


if __name__ == '__main__':
    main()
