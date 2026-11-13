#!/usr/bin/env python3
"""公开多导出C ABI验收：真实C、JIT/静态AOT搬迁及错误隔离；直接检查行为，不校验文件hash。"""
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


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ryupy', type=Path, required=True)
    parser.add_argument('--out', type=Path, required=True)
    parser.add_argument('--python', default=sys.executable)
    args = parser.parse_args()
    args.ryupy = args.ryupy.resolve()
    args.out.mkdir(parents=True, exist_ok=True)
    work = Path(tempfile.mkdtemp(prefix='multi-', dir=args.out.resolve()))
    print(work, flush=True)
    logs = work / 'logs'
    logs.mkdir()
    records = {'status': 'failed', 'host': platform.platform(), 'steps': []}
    env = {**os.environ, 'DOTNET_CLI_UI_LANGUAGE': 'en-US', 'PYTHONUTF8': '1'}
    dotnet = shutil.which(os.environ.get('DOTNET', 'dotnet'))
    assert dotnet

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

    def published(result, aot=False, output=None):
        if aot:
            return reported_aot(result.stderr, output, 'app', rid)
        paths = [Path(line.removeprefix('已生成 ')) for line in result.stderr.decode().splitlines()
                 if line.startswith('已生成 ')]
        assert len(paths) == 1 and paths[0].is_file()
        return paths[0]

    def application(label, entry, mode='normal', *, native=False, check=True, environment=env):
        return run(label, ([entry] if native else [dotnet, entry]) + [mode],
                   check=check, timeout=30, cwd=unrelated, environment=environment)

    def import_error(result):
        assert result.returncode == 1
        assert result.stdout == b''
        text = result.stderr.decode()
        assert text.rstrip().splitlines()[-1] == (
            "ImportError: cannot load declared native library 'multi_c' symbol 'demo_subtract'")
        assert 'in <module>' in text and 'Unhandled exception' not in text

    try:
        win = sys.platform == 'win32'
        mac = sys.platform == 'darwin'
        assert (win and platform.machine().lower() in ('amd64', 'x86_64')) or (
            mac and platform.machine() == 'arm64') or (
            sys.platform == 'linux' and platform.machine() == 'x86_64')
        rid = 'win-x64' if win else 'osx-arm64' if mac else 'linux-x64'
        records['rid'] = rid
        source = work / '中文 & inputs'
        source.mkdir()
        fixture = Path(__file__).resolve().parent
        for name in ('app.py', 'library.c', 'reference.py'):
            shutil.copy2(fixture / name, source / name)
        app = source / 'app.py'
        shared = source / ('multi_c.dll' if win else 'libmulti_c.dylib' if mac else 'libmulti_c.so')
        missing = source / ('missing.dll' if win else 'missing.dylib' if mac else 'missing.so')
        archive = source / ('multi.lib' if win else 'multi.a')
        bad_archive = source / ('missing.lib' if win else 'missing.a')
        c = source / 'library.c'
        if win:
            for label, omit, destination in [('c-shared', False, shared), ('c-missing', True, missing)]:
                run(label, ['cl', '/nologo', '/O2', '/MT', '/LD', *(['/DOMIT_SUBTRACT'] if omit else []),
                            c, '/Fo' + str(source / (label + '.obj')), '/Fe' + str(destination)], cwd=source)
            for label, omit, destination in [('c-static', False, archive), ('c-static-missing', True, bad_archive)]:
                obj = source / (label + '.obj')
                run(label + '-object', ['cl', '/nologo', '/O2', '/MT', '/c', *(['/DOMIT_SUBTRACT'] if omit else []),
                                       c, '/Fo' + str(obj)])
                run(label, ['lib', '/NOLOGO', '/OUT:' + str(destination), obj])
        else:
            deployment = ['-mmacosx-version-min=12.0'] if mac else []
            for label, omit, destination in [('c-shared', False, shared), ('c-missing', True, missing)]:
                run(label, ['cc', *deployment, '-O2', '-fPIC', '-dynamiclib' if mac else '-shared',
                            *(['-DOMIT_SUBTRACT'] if omit else []), c, '-o', destination])
            for label, omit, destination in [('c-static', False, archive), ('c-static-missing', True, bad_archive)]:
                obj = source / (label + '.o')
                run(label + '-object', ['cc', *deployment, '-O2', '-c', *(['-DOMIT_SUBTRACT'] if omit else []),
                                       c, '-o', obj])
                run(label, ['ar', 'rcs', destination, obj])
        exports = [{'name': name, 'symbol': symbol, 'signature': 'cdecl_i32_i32_i64'} for name, symbol in
                   [('add', 'demo_add'), ('subtract', 'demo_subtract'), ('add_alias', 'demo_add')]]
        declaration = {'schema_version': 2, 'module': 'native_math', 'library': 'multi_c', 'exports': exports,
                       'target': {'rid': rid, 'shared': shared.name, 'archive': archive.name}}
        binding = source / 'binding.json'
        binding.write_text(json.dumps(declaration), encoding='utf-8')
        reference = run('cpython-reference', [args.python, source / 'reference.py', shared, app]).stdout
        assert reference.endswith(b'multi-export-ok' + os.linesep.encode())
        unrelated = work / 'unrelated-cwd'
        unrelated.mkdir()
        jit_output, aot_output = work / 'jit-build', work / 'aot-build'
        jit = published(run('jit-build', [args.ryupy, 'build', app, '--native-binding', binding, '-o', jit_output]))
        jit_manifest = json.loads((jit.parent / 'ryupy-artifact.json').read_text())
        assert jit_manifest['schema_version'] == 4
        assert [b['export'] for b in jit_manifest['native_bindings']] == ['add', 'subtract', 'add_alias']
        assert len({b['body']['name'] for b in jit_manifest['native_bindings']}) == 3
        assert len({b['export_id'] for b in jit_manifest['native_bindings']}) == 3
        assert len([a for a in jit_manifest['artifacts'] if a['role'] == 'native_shared']) == 1
        layout = published(run('aot-build', [args.ryupy, 'build', app, '--native-binding', binding,
                                           '--aot', '-o', aot_output], timeout=600), True, aot_output)
        project = (layout.host / 'Host.csproj').read_text()
        assert project.count('DirectPInvoke Include=') == 2
        assert 'multi_c!demo_add' in project and 'multi_c!demo_subtract' in project
        assert project.count('NativeLibrary Include=') == 1
        native_manifest = json.loads(layout.manifest.read_text())
        assert native_manifest['schema_version'] == 4
        assert len(native_manifest['native_bindings']) == 3
        assert all(b['linkage'] == 'aot_static' and b['relative_file'] is None
                   for b in native_manifest['native_bindings'])
        assert not any(p.suffix.lower() in ('.dll', '.dylib', '.so', '.a', '.lib') for p in layout.dist.rglob('*'))
        # 后续符号缺失必须由真实链接器拒绝，已成功发布目录仍能执行原行为。
        declaration['target']['archive'] = bad_archive.name
        binding.write_text(json.dumps(declaration), encoding='utf-8')
        failed = run('aot-missing-symbol', [args.ryupy, 'build', app, '--native-binding', binding,
                                          '--aot', '-o', aot_output], check=False, timeout=600)
        assert failed.returncode != 0
        assert 'demo_subtract' in failed.stderr.decode(errors='replace')
        failures = list(layout.build.glob('attempt-*/build-error.log'))
        assert len(failures) == 1
        assert 'demo_subtract' in failures[0].read_text()
        assert not list(failures[0].parent.rglob('ryupy-artifact.json'))
        assert application('aot-prior-dist-survives', layout.native, native=True).stdout == reference
        # 保留源/SDK树作为证据，但原绝对路径全部失效；只搬交付目录。
        moved_jit = work / 'moved-jit'
        shutil.move(jit.parent, moved_jit)
        moved_aot = work / 'moved-aot'
        shutil.move(layout.dist, moved_aot)
        shutil.move(aot_output, work / 'aot-build.unavailable')
        saved_source = work / 'inputs.unavailable'
        shutil.move(source, saved_source)
        jit = moved_jit / jit.name
        native = moved_aot / layout.native.name
        assert application('jit-relocated', jit).stdout == reference
        assert application('aot-relocated', native, native=True).stdout == reference
        for label, entry, is_native in [('jit', jit, False), ('aot', native, True)]:
            failure = application(label + '-type', entry, 'type', native=is_native, check=False)
            assert failure.returncode == 1 and failure.stdout == b''
            text = failure.stderr.decode()
            assert text.rstrip().splitlines()[-1] == (
                "TypeError: subtract() argument 'left' must be an actual integer, not bool or a converted value")
            assert 'in fail_call' in text and 'in <module>' in text and 'Unhandled exception' not in text
        relative = Path(jit_manifest['native_bindings'][0]['relative_file'])
        broken = work / 'missing-symbol'
        shutil.copytree(moved_jit, broken)
        shutil.copy2(saved_source / missing.name, broken / relative)
        assert application('jit-second-symbol-missing', broken / jit.name, 'missing-symbol').stdout == (
            b'9' + os.linesep.encode() + b'second-symbol-missing' + os.linesep.encode() + b'13' + os.linesep.encode())
        no_library = work / 'no-library'
        shutil.copytree(moved_jit, no_library)
        (no_library / relative).unlink()
        # 即使搜索路径放着正确库，resolver仍只能用包内路径；导入保持惰性。
        decoy_env = {**env, 'PATH': str(saved_source) + os.pathsep + env.get('PATH', ''),
                     'LD_LIBRARY_PATH': str(saved_source), 'DYLD_LIBRARY_PATH': str(saved_source)}
        config = no_library / jit.with_suffix('.runtimeconfig.json').name
        value = json.loads(config.read_text())
        value['runtimeOptions'].setdefault('configProperties', {})[
            'System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported'] = False
        config.write_text(json.dumps(value), encoding='utf-8')
        assert application('jit-no-library-lazy', no_library / jit.name, 'lazy', environment=decoy_env).stdout == (
            b'imported' + os.linesep.encode())
        import_error(application('jit-no-library', no_library / jit.name, 'load', check=False, environment=decoy_env))
        invalid = work / 'invalid-library'
        shutil.copytree(moved_jit, invalid)
        (invalid / relative).write_bytes(b'invalid native library')
        import_error(application('jit-invalid-library', invalid / jit.name, 'load', check=False))
        records['status'] = 'passed'
        records['relocated'] = {'jit': str(jit), 'aot': str(native)}
        records['scope'] = '本宿主、多导出固定数值ABI；资源组合与第三方依赖闭包另行实现'
    finally:
        (work / 'result.json').write_text(json.dumps(records, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
