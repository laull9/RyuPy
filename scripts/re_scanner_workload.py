"""原样 Markdown attr_list 三调用：完整202B JIT及合法Legacy AOT预检，不替换原extra入口。"""
from pathlib import Path
import shutil
import re_scanner_contract as c
from distribution_metadata_identity import isolated
import distribution_metadata_contract as metadata
from textwrap_dedent_native import preserve_before_validation

old, require, sha, save = c.shared.old, c.require, c.sha, c.shared.save


def argv(runner, target, aot=False):
    source = runner.inputs_dir / runner.data['real_workload']['source']
    args = [runner.cli, 'build', source, '--diagnostic-stages', '--out-dir', target,
            '-I', runner.inputs_dir / 'stdlib', '-I', runner.inputs_dir / 'markdown',
            '--include-module', 'html.parser', '--include-module', 'markdown.extensions.attr_list',
            '--distribution-metadata', runner.inputs_dir / 'metadata-request.json']
    # JIT沿原真实html.parser显式实例选型；AOT不带该不合法选项，只用已有Legacy图资格检查。
    return args + (['--aot'] if aot else ['--instance-module', 'html.parser'])


def source_map(runner, plan_path, aot=False):
    plan = c.parse(plan_path.read_bytes())
    require(plan['compiled_feature'] is True and plan['session_policy'] == 'ordinary-dynamic-module-pilot', '业务模块模式改变')
    rows = plan['modules']
    names = [row['module'] for row in rows]
    require(len(names) == len(set(names)) and all(row['compiled'] is True for row in rows), '业务模块图重复/未编译')
    require({'__main__', 'markdown', 'markdown.extensions.attr_list', 'html.parser'} <= set(names), '原业务入口/attr_list图缺失')
    require(not {'markdown.extensions.extra', 'markdown.extensions.md_in_html'} & set(names), 'attr_list补充图混入完整extra')
    if aot:
        require(all(row['execution'] == 'LegacySingleton' and row['reason'] == 'aot-not-migrated'
                    and row['ordinary_name_handlers'] is False and row['relative_from_import'] is False for row in rows), '业务AOT不是合法原Legacy图')
    known = c.parse((c.ROOT / 'tests/validation/text_search.json').read_bytes())['markdown_sources']
    result = {'__main__': runner.data['real_workload']['source_sha256']}
    for name in names:
        if name == '__main__': continue
        require(name in known, '新增未冻结业务源码模块：' + name)
        row = known[name]
        require(sha(runner.inputs_dir / row['path']) == row['sha256'], '业务上游源码改变')
        result[name] = row['sha256']
    return result


def jit(runner):
    real = runner.data['real_workload']
    target = runner.out / 'products/attr-list'
    runner.barrier('attr-list-before-compile')
    record = runner.command('attr-list-compile', argv(runner, target), 180, runner.env)
    runner.barrier('attr-list-after-compile')
    require(c.shared.complete(record), '业务CLI编译失败，JIT未执行')
    pe = target / 'attr_list_entry.dll'
    runner.audit(lambda: require(pe.is_file() and sha(target / 'RyuPy.Runtime.dll') == sha(runner.runtime), '业务PE/runtime身份不符'))
    sources = runner.audit(lambda: source_map(runner, target / 'module-environment.json'))
    report, _ = runner.audit(lambda: metadata.audit_pe(runner, 'attr-list', pe,
        target / 'distribution-metadata.json', runner.inputs_dir / 'metadata-request.json'))
    runner.snapshot_reports['attr-list'] = report
    runner.preserve(target, 'attr-list-pe')
    save(runner.out / 'attr-list-before-execution.json', dict(pe_sha256=sha(pe), runtime_sha256=sha(runner.runtime), sources=sources))
    runner.record_run('attr-list-jit-run', [runner.dotnet, pe], (c.ROOT / real['golden']).read_text(), real['source_sha256'])
    return dict(stdout_bytes=202, source_unchanged=True, default_project_delta=0)


def native(runner):
    target = runner.out / 'attr-list-native-build'
    require(not target.exists(), '业务Native域已尝试，禁止补采')
    require(shutil.disk_usage(runner.out).free >= 2 * 1024**3, 'Native空间低于2GiB，停止且不清资料')
    name = 'attr-list-native'
    runner.barrier(name + '-before-compile')
    record = runner.command(name + '-publish', argv(runner, target, True), 1200, runner.env)
    # 高层CLI先作已有图/资源资格门，只有合法图才进入ILC；失败也先保存已产生dist。
    raw = preserve_before_validation(runner, target, 'attr_list_entry')
    runner.barrier(name + '-after-compile')
    if not c.shared.complete(record):
        save(runner.out / 'attr-list-aot-boundary.json', dict(passed=False, record=record,
             native_execution='not-run', known_E0100=b'E0100' in (runner.out / (name + '-publish.stderr')).read_bytes(),
             scope='真实合法Legacy配置图/资源/publish失败；不是expected-fail通过'))
        raise ValueError('attr_list合法Legacy AOT未通过，保完整原诊断，不扩全模块AOT')
    layout = runner.audit(lambda: old.reported_aot((runner.out / (name + '-publish.stderr')).read_bytes(), target,
        'attr_list_entry', 'linux-x64'))
    runner.audit(lambda: require(len(raw) == 1 and raw[0].samefile(layout.dist), '业务reported dist与已归档原件不符'))
    sources = runner.audit(lambda: source_map(runner, layout.managed.parent / 'module-environment.json', True))
    report, _ = runner.audit(lambda: metadata.audit_pe(runner, name, layout.managed,
        layout.managed.parent / 'distribution-metadata.json', runner.inputs_dir / 'metadata-request.json'))
    require(report == runner.snapshot_reports['attr-list'], '业务JIT/AOT快照不同源')
    sdk = old.read_sdk_version(layout.host / 'sdk.version')
    require(sdk == runner.data['sdk_version'], '业务AOT SDK不符')
    manifest = runner.audit(lambda: metadata.verify_native(layout.dist, layout.native, sdk, report,
        runner.runtime, layout.managed, sources))
    save(runner.out / 'attr-list-native-manifest.json', manifest)
    saved = runner.out / 'attr-list-native-managed'; saved.mkdir()
    for path in layout.managed.parent.iterdir():
        if path.is_file(): shutil.copy2(path, saved / path.name)
    runner.preserve(saved, 'attr-list-native-managed')
    runner.preserve(layout.dist, 'attr-list-native-original')
    moved = runner.out / 'attr-list-relocated'
    binary = runner.audit(lambda: metadata.relocate(layout.dist, layout.native, moved))
    runner.audit(lambda: metadata.verify_native(moved, binary, sdk, report, runner.runtime, layout.managed, sources))
    runner.preserve(moved, 'attr-list-native-relocated')
    golden = (c.ROOT / runner.data['real_workload']['golden']).read_bytes()
    env = {'PATH': '/usr/bin:/bin', 'LC_ALL': 'C.UTF-8', 'TZ': 'UTC'}
    with isolated(runner, [runner.inputs_dir], 'attr-list-all-compiled-inputs'):
        runner.run_positive('attr-list-native-original-run', [layout.native], golden, env)
        runner.run_positive('attr-list-native-relocated-run', [binary], golden, env)
    return dict(stdout_bytes=202, original=True, relocated=True, default_project_delta=0)
