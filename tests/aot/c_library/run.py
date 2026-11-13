#!/usr/bin/env python3
"""通用动态 C 库：真实 zlib、显式依赖、JIT/AOT/搬迁及缺依赖负例；临时产物自动清理。"""
import argparse
import ctypes
import json
import os
from pathlib import Path
import platform
import runpy
import shutil
import subprocess
import sys
import tarfile
import tempfile
import types


def run(command, *, cwd=None, env=None, timeout=600, check=True):
    result = subprocess.run(list(map(str, command)), cwd=cwd, env=env,
                            capture_output=True, timeout=timeout)
    if check and result.returncode:
        raise AssertionError((command, result.returncode, result.stderr.decode(errors='replace')))
    return result


def declaration(rid, library, exports, dependencies=()):
    def parameter(index, kind):
        value = {'name': 'arg' + str(index), 'type': kind}
        if kind == 'bytes':
            value.update(ownership='borrowed_call', length='usize')
        return value
    return {'schema_version': 3, 'module': 'native_demo', 'library': 'arbitrary_c',
            'exports': [{'name': name, 'symbol': symbol, 'signature': {
                'calling_convention': 'cdecl', 'parameters': [parameter(i, kind)
                    for i, kind in enumerate(kinds)], 'returns': returns}}
                for name, symbol, kinds, returns in exports],
            'target': {'rid': rid, 'shared': library, 'linkage': 'dynamic',
                       'dependencies': list(dependencies)}}


def reference(app, library, exports):
    # 参考实现直接调用同一原生库；Python 标准库不替换被测 C 符号。
    native = ctypes.CDLL(str(library))
    module = types.ModuleType('native_demo')
    kinds = {'i32': ctypes.c_int32, 'usize': ctypes.c_size_t, 'u64': ctypes.c_uint64}
    for name, symbol, parameters, returns in exports:
        function = getattr(native, symbol)
        function.argtypes = [ty for kind in parameters for ty in
            ([ctypes.c_void_p, ctypes.c_size_t] if kind == 'bytes' else [kinds[kind]])]
        function.restype = kinds[returns]
        def invoke(*args, _function=function, _parameters=parameters):
            values, buffers = [], []
            for value, kind in zip(args, _parameters):
                if kind == 'bytes':
                    buffer = ctypes.create_string_buffer(value, max(1, len(value)))
                    buffers.append(buffer)
                    values.extend((ctypes.cast(buffer, ctypes.c_void_p), len(value)))
                else:
                    values.append(value)
            return _function(*values)
        setattr(module, name, invoke)
    sys.modules['native_demo'] = module
    runpy.run_path(str(app), run_name='__main__')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ryupy', type=Path)
    parser.add_argument('--python', default=sys.executable)
    parser.add_argument('--reference', nargs=3)
    args = parser.parse_args()
    if args.reference:
        app, library, binding = map(Path, args.reference)
        config = json.loads(binding.read_text())
        exports = [(item['name'], item['symbol'], [p['type'] for p in item['signature']['parameters']],
                    item['signature']['returns']) for item in config['exports']]
        reference(app, library, exports)
        return
    assert args.ryupy and args.ryupy.is_file()
    assert platform.machine() in ('arm64', 'x86_64') and sys.platform in ('darwin', 'linux')
    assert run([args.python, '-c', 'import platform; print(platform.python_version())']).stdout.strip() == b'3.12.13'
    executable = args.ryupy.resolve()
    fixture = Path(__file__).resolve().parent
    dotnet = shutil.which('dotnet')
    assert dotnet
    mac = sys.platform == 'darwin'
    rid, extension = ('osx-arm64', 'dylib') if mac else ('linux-x64', 'so')
    with tempfile.TemporaryDirectory(prefix='ryupy-c-library-') as temporary:
        work = Path(temporary).resolve()
        source = work / '中文 & inputs'
        source.mkdir()
        helper = source / ('libhelper.' + extension)
        library = source / ('libdemo.' + extension)
        linker = ['-dynamiclib'] if mac else ['-shared']
        run(['cc', '-Wall', '-Wextra', '-Werror', '-fPIC', *linker, fixture / 'dependency.c',
             *(['-Wl,-install_name,@rpath/' + helper.name] if mac else []), '-o', helper])
        run(['cc', '-Wall', '-Wextra', '-Werror', '-fPIC', *linker, fixture / 'library.c', helper,
             '-Wl,-rpath,' + ('@loader_path' if mac else '$ORIGIN'), '-o', library])
        # 固定官方发行源码，同次构建；第三方源码、许可证、二进制都留临时目录。
        archive = work / 'zlib.tar.gz'
        run(['curl', '-fLsS', 'https://zlib.net/fossils/zlib-1.3.1.tar.gz', '-o', archive])
        with tarfile.open(archive) as package:
            package.extractall(work, filter='data')
        zlib = work / 'zlib-1.3.1'
        run(['./configure', '--shared'], cwd=zlib)
        run(['make', '-j2'], cwd=zlib)
        zlibrary = source / ('libthirdparty.' + extension)
        shutil.copyfile(zlib / ('libz.1.3.1.dylib' if mac else 'libz.so.1.3.1'), zlibrary)
        exports = [('absolute', 'absolute', ['i32'], 'i32'),
                   ('measure', 'measure', ['bytes'], 'u64'), ('add', 'add', ['i32', 'i32'], 'i32')]
        zexports = [('bound', 'compressBound', ['usize'], 'usize'),
                    ('checksum', 'crc32_z', ['u64', 'bytes'], 'u64')]
        moved = []
        for name, native, functions, dependencies in [
                ('app', library, exports, [helper.name]), ('zlib_app', zlibrary, zexports, [])]:
            app = source / (name + '.py')
            shutil.copyfile(fixture / app.name, app)
            binding = source / (name + '.json')
            binding.write_text(json.dumps(declaration(rid, native.name, functions, dependencies)))
            baseline = run([args.python, '-B', __file__, '--reference', app, native, binding])
            expected = (baseline.returncode, baseline.stdout, baseline.stderr)
            for aot in (False, True):
                output = work / (name + ('-aot' if aot else '-jit'))
                result = run([executable, 'build', app, '--native-binding', binding,
                              *(['--aot'] if aot else []), '-o', output])
                generated = [Path(line[len('已生成 '):]) for line in result.stderr.decode().splitlines()
                             if line.startswith('已生成 ')]
                entry = next(path for path in generated if path.suffix != '.dll') if aot else generated[0]
                destination = work / (name + ('-moved-aot' if aot else '-moved-jit'))
                shutil.move(entry.parent, destination)
                entry = destination / entry.name
                manifest = json.loads((destination / 'ryupy-artifact.json').read_text())
                assert all(item['linkage'] == ('aot_shared' if aot else 'jit_shared')
                           for item in manifest['native_bindings'])
                assert manifest['native_dependencies'] == dependencies
                moved.append((name, aot, entry, expected))
        # 源码、原生输入、SDK工作根全部删除，清环境并切换 cwd。
        shutil.rmtree(source)
        shutil.rmtree(zlib)
        for path in work.iterdir():
            if path.name in ('app-aot', 'app-jit', 'zlib_app-aot', 'zlib_app-jit'):
                shutil.rmtree(path)
        unrelated = work / 'unrelated'
        unrelated.mkdir()
        empty_path = work / 'empty-path'
        empty_path.mkdir()
        env = {key: value for key, value in os.environ.items() if not key.startswith(('PYTHON', 'DYLD_', 'LD_'))}
        env['PATH'] = str(empty_path)
        for name, aot, entry, expected in moved:
            result = run([entry] if aot else [dotnet, entry], cwd=unrelated, env=env)
            assert (result.returncode, result.stdout, result.stderr) == expected, (name, aot, result, expected)
            print(name, 'AOT' if aot else 'JIT', '搬迁差分通过', flush=True)
            if name == 'app':
                dependency = entry.parent / '_internal/native/v1/b0' / helper.name
                dependency.unlink()
                failure = run([entry] if aot else [dotnet, entry], cwd=unrelated, env=env, check=False)
                assert failure.returncode != 0 and b'ImportError' in failure.stderr
                assert helper.name.encode() in failure.stderr, failure.stderr
                print(name, 'AOT' if aot else 'JIT', '缺依赖负例通过', flush=True)


if __name__ == '__main__':
    main()
