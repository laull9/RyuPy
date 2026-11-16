"""搜索同源验收的窄产品适配：不重跑CP；多模块来源与环境严格核验，原位成功才搬移。"""
import json
import shutil
from pathlib import Path
import typing_noreturn as old

shared = old.shared
require, sha, save, snapshot = old.require, old.sha, old.save, old.snapshot


def source_map(entry, inputs, data, markdown=False):
    result = {'__main__': {'path': str(entry), 'sha256': sha(entry), 'package': '',
                           'logical_source': 'entry/' + entry.name}}
    names = data['markdown_sources'] if markdown else ({'_markupbase': data['markdown_sources']['_markupbase']}
                                                       if inputs is not None else {})
    for name, row in names.items():
        path = inputs / row['path']
        require(sha(path) == row['sha256'], '锁定模块源改变：' + name)
        result[name] = dict(row, path=str(path))
    return result


def verify_sources(manifest, sources):
    rows = manifest['sources']
    require(len(rows) == len(sources) and {x['module_name'] for x in rows} == set(sources), '多源集合不符')
    require(next(x['module_id'] for x in rows if x['module_name'] == '__main__') ==
            manifest['entry']['module_id'], 'Native入口module_id未绑定__main__')
    for row in rows:
        expected = sources[row['module_name']]
        require(row['compiled_text_sha256'] == expected['sha256'] == sha(Path(expected['path'])) and
                row['logical_source'] == expected['logical_source'] and row['package'] == expected['package'],
                '逐source绑定不符：' + row['module_name'])


def verify_environment(path, data, sources, markdown, aot=False):
    actual = json.loads(path.read_text())
    expected = data['aot_environment' if aot else 'markdown_environment']
    if not markdown:
        expected = dict(expected, modules=[x for x in expected['modules'] if x['module'] in sources])
    require(actual == expected, '真实模块环境与冻结策略/来源不符')


def build_argv(runner, entry, target, inputs, markdown):
    if markdown:
        return shared.build_command(runner.cli, entry, inputs, target)
    return [runner.cli, 'build', entry, '--diagnostic-stages', '--out-dir', target] + (
        ['-I', inputs / 'stdlib'] if inputs is not None else [])


def jit(runner, name, entry, golden, inputs, markdown):
    target = runner.out / 'products' / name
    sources = source_map(entry, inputs, runner.data, markdown)
    record = runner.command(name + '-compile', build_argv(runner, entry, target, inputs, markdown), 120, runner.env)
    require(record['exit'] == 0 and record['failure'] is None, '编译失败；未运行JIT')
    require(sha(target / 'RyuPy.Runtime.dll') == sha(runner.runtime), 'JIT Runtime不符')
    verify_environment(target / 'module-environment.json', runner.data, sources, markdown)
    saved = runner.out / 'preserved-pe' / name
    shutil.copytree(target, saved)
    require(snapshot(target) == snapshot(saved), '执行前PE保存不符')
    runner.preserve(target, name + '-pe'); runner.preserve(saved, name + '-saved-pe')
    runner.run_positive(name + '-jit', [runner.dotnet, target / (entry.stem + '.dll')], golden)


def native(runner, name, entry, golden, inputs, markdown):
    previous, labels = runner.out, runner.labels
    runner.out = previous / 'native-domains' / ('search-' + name)
    runner.out.mkdir(parents=True, exist_ok=False); runner.labels = set()
    state = {'publish': 'not-run', 'validation': 'blocked', 'original': 'blocked', 'relocated': 'blocked'}
    try:
        require(shutil.disk_usage(runner.out).free >= 2 * 1024**3, 'Native可用磁盘不足2GiB')
        sources = source_map(entry, inputs, runner.data, markdown)
        save(runner.out / 'source-map.json', sources)
        target = runner.out / 'native-build'
        state['publish'] = 'started'
        record = runner.command('native-publish', build_argv(runner, entry, target, inputs, markdown) + ['--aot'],
                                1200 if markdown else 300, runner.env)
        succeeded = record['exit'] == 0 and record['failure'] is None
        state['publish'] = 'passed' if succeeded else 'failed'
        state['validation'] = 'started' if succeeded else 'blocked'
        runner.preserve_publish_evidence(target, entry.stem)
        # schema/layout验收之前保原manifest；旧SDK证据已保存原PE，失败不丢原件。
        manifests = list(target.rglob('ryupy-artifact.json'))
        require(len(manifests) <= 16, '原manifest数量超限')
        for index, path in enumerate(manifests):
            require(not path.is_symlink() and path.stat().st_size <= 4 * 1024**2, '原manifest不安全/超限')
            raw = runner.out / ('manifest-raw-' + str(index) + '.json')
            shutil.copy2(path, raw)
            runner.frozen[str(raw)] = sha(raw)
        require(succeeded, 'Native publish失败')
        layout = old.reported_aot((runner.out / 'native-publish.stderr').read_bytes(), target, entry.stem, 'linux-x64')
        old.preserve_dist(layout.dist, runner.out)
        runner.preserve(layout.dist, 'native-original')
        sdk = old.read_sdk_version(layout.host / 'sdk.version')
        require(sdk == '10.0.403', 'Native SDK不符')
        saved = runner.out / 'native-managed'; saved.mkdir()
        for path in layout.managed.parent.iterdir():
            if path.is_file(): shutil.copy2(path, saved / path.name)
        runner.preserve(saved, 'native-managed')
        manifest = old.verify_manifest(layout.dist, layout.native, 'linux-x64', sdk)
        verify_sources(manifest, sources)
        verify_environment(layout.managed.parent / 'module-environment.json', runner.data, sources, markdown, aot=True)
        expected_reasons = {x['module']: x['reason'] for x in runner.data['aot_environment']['modules']}
        require(all(x['environment_reason'] == expected_reasons[x['name']] for x in manifest['modules']),
                'Native manifest环境原因不符')
        options = manifest['compiler']['options']
        require(options['module_environment_policy'] == 'ordinary-dynamic-module-pilot' and
                options['instance_modules'] == (['html.parser'] if markdown else []) and
                all(not value for key, value in options.items() if key.startswith(('experimental_', 'diagnostic_'))),
                'Native编译选项不符')
        bindings = {x['role']: x for x in manifest['build_inputs']}
        require(bindings['runtime_managed_input']['sha256'] == sha(runner.runtime) ==
                sha(layout.managed.parent / 'RyuPy.Runtime.dll') and
                bindings['compiled_managed_input']['sha256'] == sha(layout.managed), 'Native输入绑定不符')
        save(runner.out / 'native-manifest.json', manifest)
        # 归档同样进入最终身份核验；原位失败不多跑搬移产品。
        runner.frozen[str(runner.out / 'native-dist.tar.gz')] = sha(runner.out / 'native-dist.tar.gz')
        state['validation'] = 'passed'; state['original'] = 'started'
        env = {'PATH': '/usr/bin:/bin', 'LC_ALL': 'C.UTF-8', 'TZ': 'UTC'}
        runner.run_positive('native-original', [layout.native], golden, env)
        state['original'] = 'passed'; state['relocated'] = 'started'
        moved = runner.out / 'relocated'
        binary, _ = old.relocate_manifest(layout.dist, layout.native, moved, 'linux-x64', sdk)
        require(snapshot(moved) == snapshot(layout.dist), '搬移身份改变')
        runner.preserve(moved, 'native-relocated')
        runner.run_positive('native-relocated', [binary], golden, env)
        state['relocated'] = 'passed'
    finally:
        for key, value in state.items():
            if value == 'started': state[key] = 'failed'
        save(runner.out / 'native-stages.json', state)
        runner.out, runner.labels = previous, labels


def markdown_boundary(runner, name, entry, inputs):
    """保原显式配置的AOT编译边界；不加资源、不移除instance、不承诺Native运行。"""
    sources = source_map(entry, inputs, runner.data, True)
    target = runner.out / 'products' / (name + '-aot-boundary')
    label = name + '-aot-boundary'
    record = runner.command(label, build_argv(runner, entry, target, inputs, True) + ['--aot'], 120, runner.env)
    stderr = (runner.out / (label + '.stderr')).read_bytes()
    expected = (record['exit'] == 1 and record['failure'] is None and record['raw_streams_complete'] and
                b'E0100' in stderr and '显式实例 AOT 需要资源清单且不支持原生绑定'.encode() in stderr)
    save(runner.out / (label + '-classification.json'), {'supported': False,
         'known_configuration_boundary_observed': expected, 'native_original': 'blocked',
         'native_relocated': 'blocked', 'source_map': sources, 'record': record,
         'reason': '原显式html.parser配置无资源清单；不扩大实例AOT语法域'})
    if target.is_dir(): runner.preserve(target, label + '-generated')
    raise ValueError('Markdown AOT现有配置E0100未支持；Native未运行' if expected else
                     'Markdown AOT边界与预期不符；保原流，不扩域或自动重试')
