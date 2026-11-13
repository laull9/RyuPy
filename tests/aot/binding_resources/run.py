#!/usr/bin/env python3
"""复用多导出 C/CPython fixture、AOT 报告和资源合同，验组合发布与失败保旧包。"""
import argparse
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys
import tempfile

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from aot_layout import reported_aot
from resource_manifest import validate_resources


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ryupy', type=Path, required=True)
    parser.add_argument('--out', type=Path, required=True)
    parser.add_argument('--python', default=sys.executable)
    args = parser.parse_args()
    args.ryupy = args.ryupy.resolve()
    args.out.mkdir(parents=True, exist_ok=True)
    work = Path(tempfile.mkdtemp(prefix='binding-resources-', dir=args.out.resolve()))
    print(work, flush=True)
    logs = work / 'logs'
    logs.mkdir()
    records = {'status': 'failed', 'host': platform.platform(), 'steps': []}
    env = {**os.environ, 'DOTNET_CLI_UI_LANGUAGE': 'en-US', 'PYTHONUTF8': '1',
           'PYTHONDONTWRITEBYTECODE': '1'}

    def run(label, argv, *, check=True, timeout=90, cwd=work, environment=env):
        argv = list(map(str, argv))
        result = subprocess.run(argv, cwd=cwd, env=environment, capture_output=True, timeout=timeout)
        (logs / (label + '.stdout')).write_bytes(result.stdout)
        (logs / (label + '.stderr')).write_bytes(result.stderr)
        records['steps'].append({'step': label, 'argv': argv, 'exit': result.returncode})
        print(label, result.returncode, flush=True)
        if check:
            assert result.returncode == 0, (label, result.stderr.decode(errors='replace'))
        return result

    def save(path, value):
        path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

    def snapshot(root):
        # 直接逐字保存旧包，失败后比较原字节；不引入额外文件 hash 流程。
        return {p.relative_to(root).as_posix(): (p.read_bytes(), p.stat().st_mode)
                for p in root.rglob('*') if p.is_file()}

    try:
        win, mac = sys.platform == 'win32', sys.platform == 'darwin'
        assert (win and platform.machine().lower() in ('amd64', 'x86_64')) or (
            mac and platform.machine() == 'arm64') or (
            sys.platform == 'linux' and platform.machine() == 'x86_64')
        rid = 'win-x64' if win else 'osx-arm64' if mac else 'linux-x64'
        records['rid'] = rid
        fixture = Path(__file__).resolve().parent
        multi = fixture.parent / 'binding_multi'
        source = work / '中文 & inputs'
        shutil.copytree(fixture / 'pkgdata', source / 'pkgdata')
        for name in ('app.py', 'single.py', 'resources.json'):
            shutil.copy2(fixture / name, source / name)
        shutil.copy2(multi / 'library.c', source / 'library.c')
        shared = source / ('multi_c.dll' if win else 'libmulti_c.dylib' if mac else 'libmulti_c.so')
        archive = source / ('multi.lib' if win else 'multi.a')
        missing = source / ('missing.lib' if win else 'missing.a')
        c = source / 'library.c'
        if win:
            run('c-shared', ['cl', '/nologo', '/O2', '/MT', '/LD', c,
                             '/Fo' + str(source / 'shared.obj'), '/Fe' + str(shared)], cwd=source)
        else:
            deployment = ['-mmacosx-version-min=12.0'] if mac else []
            run('c-shared', ['cc', *deployment, '-O2', '-fPIC', '-dynamiclib' if mac else '-shared',
                             c, '-o', shared])
        for label, omit, destination in [('c-static', False, archive), ('c-static-missing', True, missing)]:
            obj = source / (label + ('.obj' if win else '.o'))
            if win:
                run(label + '-object', ['cl', '/nologo', '/O2', '/MT', '/c',
                                       *(['/DOMIT_SUBTRACT'] if omit else []), c, '/Fo' + str(obj)])
                run(label, ['lib', '/NOLOGO', '/OUT:' + str(destination), obj])
            else:
                run(label + '-object', ['cc', *deployment, '-O2', '-c',
                                       *(['-DOMIT_SUBTRACT'] if omit else []), c, '-o', obj])
                run(label, ['ar', 'rcs', destination, obj])
        exports = [{'name': name, 'symbol': symbol, 'signature': 'cdecl_i32_i32_i64'}
                   for name, symbol in [('add', 'demo_add'), ('subtract', 'demo_subtract')]]
        declaration = {'schema_version': 2, 'module': 'native_math', 'library': 'multi_c',
                       'exports': exports, 'target': {'rid': rid, 'shared': shared.name, 'archive': archive.name}}
        binding, resources = source / 'binding.json', source / 'resources.json'
        save(binding, declaration)
        reference_env = {**env, 'PYTHONPATH': str(source)}
        references = {}
        for stem in ('app', 'single'):
            references[stem] = run('cpython-' + stem, [args.python, multi / 'reference.py', shared,
                                                       source / (stem + '.py')], environment=reference_env)
        assert b'38 24' in references['app'].stdout and '随包数据'.encode() in references['app'].stdout
        unrelated = work / 'unrelated-cwd'
        unrelated.mkdir()
        # 同名诱饵不能成为资源缺失时的 cwd 回退来源。
        shutil.copytree(source / 'pkgdata', unrelated / 'pkgdata')
        output = work / 'output'

        def command(stem='app', *, aot=True, resource=resources):
            return [args.ryupy, 'build', source / (stem + '.py'), '--native-binding', binding,
                    '--resources', resource, '-o', output, *(['--aot'] if aot else [])]

        def application(label, native, reference):
            result = run(label, [native], cwd=unrelated, timeout=30)
            assert (result.stdout, result.stderr) == (reference.stdout, reference.stderr)

        def manifest(layout, version, count):
            data = json.loads(layout.manifest.read_text(encoding='utf-8'))
            assert data['schema_version'] == version
            validate_resources(data)
            assert len(data['resources']) == len(data['module_locations']) == 1
            package = next(s for s in data['sources'] if s['module_name'] == 'pkgdata')
            assert data['module_locations'][0]['module_id'] == package['module_id']
            assert data['resources'][0]['consumer_module_ids'] == [package['module_id']]
            assert len(data['native_bindings']) == count
            native_id = data['native_bindings'][0]['module_id']
            assert native_id not in {s['module_id'] for s in data['sources']}
            assert all(b['module_id'] == native_id and b['linkage'] == 'aot_static'
                       and b['relative_file'] is None for b in data['native_bindings'])
            assert [b['symbol'] for b in data['native_bindings']] == [e['symbol'] for e in exports[:count]]
            assert [x['role'] for x in data['build_inputs']].count('native_static_archive') == 1
            assert not any(a['role'] == 'native_shared' for a in data['artifacts'])
            actual = {p.relative_to(layout.dist).as_posix() for p in layout.dist.rglob('*') if p.is_file()}
            assert actual == {a['relative_path'] for a in data['artifacts']} | {'ryupy-artifact.json'}
            resource_path = layout.dist / data['resources'][0]['relative_path']
            assert resource_path.read_bytes() == (fixture / 'pkgdata/numbers.txt').read_bytes()
            assert not any(p.suffix.lower() in ('.py', '.dll', '.a', '.lib', '.so', '.dylib')
                           for p in layout.dist.rglob('*') if p.is_file())
            return data

        result = run('aot-v2', command(), timeout=600)
        layout = reported_aot(result.stderr, output, 'app', rid)
        data = manifest(layout, 6, 2)
        project = (layout.host / 'Host.csproj').read_text()
        assert project.count('DirectPInvoke Include=') == 2 and project.count('NativeLibrary Include=') == 1
        assert len(list((layout.sdk_root / 'resource-staging').iterdir())) == 1
        application('aot-original', layout.native, references['app'])
        original = snapshot(layout.dist)

        def failed(label, argv, message, *, sdk=False):
            attempts = set(layout.build.glob('attempt-*'))
            result = run(label, argv, check=False, timeout=600)
            assert result.returncode != 0 and message in result.stderr.decode(errors='replace')
            assert '已生成 '.encode() not in result.stderr
            assert snapshot(layout.dist) == original
            new = set(layout.build.glob('attempt-*')) - attempts
            assert len(new) == 1
            attempt = new.pop()
            assert message in (attempt / 'build-error.log').read_text()
            assert not list(attempt.rglob('ryupy-artifact.json'))
            assert bool(list(attempt.rglob('publish.stdout.log'))) == sdk
            application(label + '-old-dist', layout.native, references['app'])

        # 资源在 ILC 前冻结失败；错误声明与真正静态链接失败都保留旧成功 dist。
        resource_source = source / 'pkgdata/numbers.txt'
        raw = resource_source.read_bytes()
        resource_source.unlink()
        failed('missing-resource', command(), 'numbers.txt')
        resource_source.write_bytes(raw)
        bad_resources = source / 'wrong-resources.json'
        save(bad_resources, {'schema_version': 1, 'modules': [{'module': 'native_math', 'files': []}]})
        failed('generated-module-resource', command(resource=bad_resources), '资源模块不是本次已编译源码模块')
        declaration['exports'][1]['signature'] = 'invalid'
        save(binding, declaration)
        failed('wrong-binding', command(), '只支持固定cdecl_i32_i32_i64签名')
        declaration['exports'][1]['signature'] = 'cdecl_i32_i32_i64'
        declaration['target']['archive'] = missing.name
        save(binding, declaration)
        failed('static-missing-symbol', command(), 'demo_subtract', sdk=True)
        declaration['target']['archive'] = archive.name
        save(binding, declaration)
        rejected = run('jit-resources-rejected', command(aot=False), check=False)
        assert rejected.returncode != 0 and b'--aot' in rejected.stderr
        assert snapshot(layout.dist) == original
        # 原源变化不会改变已经冻结的资源，也不会隐式改变已成功 dist。
        resource_source.write_text('999 1 changed\n', encoding='utf-8')
        application('frozen-resource', layout.native, references['app'])
        resource_source.write_bytes(raw)
        single = {key: value for key, value in declaration.items() if key != 'exports'}
        single.update(schema_version=1, export='add', symbol='demo_add', signature='cdecl_i32_i32_i64')
        save(binding, single)
        result = run('aot-v1', command('single'), timeout=600)
        single_layout = reported_aot(result.stderr, output, 'single', rid)
        manifest(single_layout, 5, 1)
        # 完整移动交付树，再隐藏全部原源码、库、托管与 ILC 中间路径。
        moved = work / 'moved-app'
        moved_single = work / 'moved-single'
        shutil.move(layout.dist, moved)
        shutil.move(single_layout.dist, moved_single)
        shutil.move(output, work / 'output.unavailable')
        shutil.move(source, work / 'inputs.unavailable')
        assert not source.exists() and not output.exists() and not layout.managed.exists()
        native = moved / layout.native.name
        application('aot-v2-relocated', native, references['app'])
        application('aot-v1-relocated', moved_single / single_layout.native.name, references['single'])
        # 缺数据副本必须真实报错，不能从旧源码、cwd 诱饵或中间树补救。
        broken = work / 'missing-data-copy'
        shutil.copytree(moved, broken)
        (broken / data['resources'][0]['relative_path']).unlink()
        failure = run('relocated-missing-resource', [broken / native.name], check=False, cwd=unrelated)
        assert failure.returncode == 1 and failure.stdout == b''
        error = failure.stderr.decode()
        assert 'FileNotFoundError:' in error and 'numbers.txt' in error and 'in <module>' in error
        assert 'Unhandled exception' not in error
        records.update(status='passed', relocated={'v1': str(moved_single), 'v2': str(moved)},
                       scope='本宿主显式资源＋单库固定数值 C ABI；files().read_text()')
    finally:
        save(work / 'result.json', records)


if __name__ == '__main__':
    main()
