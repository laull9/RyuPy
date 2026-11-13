#!/usr/bin/env python3
"""同候选包组合CPython provider和直接C ABI；即时CP三流、中文路径搬迁、缺库/同名域负例。"""
import argparse
import json
import os
from pathlib import Path
import shutil
import sys
import tarfile
import tempfile

from run import declaration, run


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ryupy', type=Path, required=True)
    parser.add_argument('--provider', type=Path, required=True)
    parser.add_argument('--distribution-metadata', type=Path, required=True)
    parser.add_argument('--python', default=sys.executable)
    args = parser.parse_args()
    cli, fixture = args.ryupy.resolve(), Path(__file__).resolve().parent
    config_path = args.provider.resolve()
    template = json.loads(config_path.read_text())
    for key in ('distribution_root', 'bridge_directory', 'shim'):
        if key in template:
            template[key] = str((config_path.parent / template[key]).resolve())
    dotnet = shutil.which('dotnet')
    assert dotnet and sys.platform in ('darwin', 'linux')
    mac = sys.platform == 'darwin'
    rid = 'osx-arm64' if mac else 'linux-x64'
    with tempfile.TemporaryDirectory(prefix='ryupy-combined-c-') as temporary:
        work = Path(temporary).resolve()
        source = work / '中文 空格 & 输入'
        source.mkdir()
        app = source / 'combined_app.py'
        shutil.copyfile(fixture / app.name, app)
        shutil.copyfile(fixture / 'combo_helper.py', source / 'combo_helper.py')
        distribution = work / 'distribution-input'
        shutil.copytree(template['distribution_root'], distribution,
                        ignore=shutil.ignore_patterns('__pycache__', '*.pyc'))
        shutil.copyfile(args.distribution_metadata / 'PYTHON.json', distribution / 'PYTHON.json')
        shutil.copytree(args.distribution_metadata / 'licenses', distribution / 'licenses', dirs_exist_ok=True)
        template['distribution_root'] = str(distribution)
        template['modules'] = ['json', 'combo_helper']
        template['container_transfer'] = 'synchronized'
        template['payloads'] = [{'source': str(source / 'combo_helper.py'), 'destination': 'combo_helper.py'}]
        template['source']['licenses'] = ['lib/python3.12/LICENSE.txt'] + [
            str(path.relative_to(distribution)) for path in sorted((distribution / 'licenses').glob('*.txt'))]
        provider = work / 'provider.json'
        provider.write_text(json.dumps(template))
        # 构建阶段获取固定官方C源码，产品运行阶段无网络和原始输入。
        archive = work / 'zlib.tar.gz'
        run(['curl', '-fLsS', 'https://zlib.net/fossils/zlib-1.3.1.tar.gz', '-o', archive])
        with tarfile.open(archive) as package:
            package.extractall(work, filter='data')
        zlib = work / 'zlib-1.3.1'
        run(['./configure', '--shared'], cwd=zlib)
        run(['make', '-j2'], cwd=zlib)
        library = source / ('thirdparty.dylib' if mac else 'thirdparty.so')
        shutil.copyfile(zlib / ('libz.1.3.1.dylib' if mac else 'libz.so.1.3.1'), library)
        binding = source / 'binding.json'
        exports = [('bound', 'compressBound', ['usize'], 'usize'),
                   ('checksum', 'crc32_z', ['u64', 'bytes'], 'u64')]
        binding.write_text(json.dumps(declaration(rid, library.name, exports)))
        baseline = run([args.python, '-B', fixture / 'run.py', '--reference', app, library, binding],
                       env=dict(os.environ, PYTHONPATH=str(source)))
        expected = (baseline.returncode, baseline.stdout, baseline.stderr)
        # 实际provider库存存在同名模块时，必须在编译/部署之前明确拒绝双权威。
        conflict = source / 'conflict.py'
        conflict.write_text('value = 1\n')
        conflict_template = dict(template, modules=['json', 'combo_helper', 'native_demo'],
                                 payloads=template['payloads'] + [{'source': str(conflict), 'destination': 'native_demo.py'}])
        conflict_provider = work / 'conflict-provider.json'
        conflict_provider.write_text(json.dumps(conflict_template))
        conflict_result = run([cli, '--cpython-provider', conflict_provider, 'build', app,
                               '--native-binding', binding, '-o', work / 'conflict-out'], check=False)
        assert conflict_result.returncode != 0 and b'native_demo' in conflict_result.stderr, conflict_result
        assert b'C ABI' in conflict_result.stderr or b'provider' in conflict_result.stderr, conflict_result.stderr
        moved = []
        for aot in (False, True):
            output = work / ('build-aot' if aot else 'build-jit')
            result = run([cli, '--cpython-provider', provider, 'build', app, '--native-binding', binding,
                          *(['--aot'] if aot else []), '-o', output])
            generated = [Path(line[len('已生成 '):]) for line in result.stderr.decode().splitlines()
                         if line.startswith('已生成 ')]
            entry = next(path for path in generated if path.suffix != '.dll') if aot else generated[0]
            destination = work / ('中文 空格 & 搬迁 AOT' if aot else '中文 空格 & 搬迁 JIT')
            shutil.move(entry.parent, destination)
            manifest = json.loads((destination / 'ryupy-artifact.json').read_text())
            assert all(item['linkage'] == ('aot_shared' if aot else 'jit_shared')
                       for item in manifest['native_bindings']), manifest
            provider_plan = json.loads((destination / 'cpython-provider.json').read_text())
            assert provider_plan['entry_provider'] == 'ryupy_compiled_source', provider_plan
            config = json.loads((destination / 'ryupy-cpython.json').read_text())
            assert (destination / config['library']).resolve().is_relative_to(destination), config
            assert (destination / config['library']).is_file()
            native_path = destination / '_internal/native/v1/b0' / ('library.dylib' if mac else 'library.so')
            assert native_path.is_file(), native_path
            moved.append((aot, destination / entry.name, native_path))
        shutil.rmtree(source)
        shutil.rmtree(distribution)
        shutil.rmtree(zlib)
        archive.unlink()
        for name in ('build-jit', 'build-aot', 'conflict-out'):
            if (work / name).exists():
                shutil.rmtree(work / name)
        unrelated = work / 'unrelated'
        unrelated.mkdir()
        empty_path = work / 'empty-path'
        empty_path.mkdir()
        env = {key: value for key, value in os.environ.items()
               if not key.startswith(('PYTHON', 'RYUPY_', 'DYLD_', 'LD_'))}
        env['PATH'] = str(empty_path)
        for aot, entry, native_path in moved:
            host_report = unrelated / ('host-aot.json' if aot else 'host-jit.json')
            actual_env = dict(env, RYUPY_CPYTHON_REPORT=str(host_report))
            result = run([entry] if aot else [dotnet, entry], cwd=unrelated, env=actual_env)
            assert (result.returncode, result.stdout, result.stderr) == expected, (aot, result, expected)
            host = json.loads(host_report.read_text())
            assert host['state'] == 'closed' and Path(host['python_library']).resolve().is_relative_to(entry.parent), host
            assert Path(host['shim_library']).resolve().is_relative_to(entry.parent), host
            assert not host['synchronization_failure'], host
            native_path.unlink()
            failure = run([entry] if aot else [dotnet, entry], cwd=unrelated, env=env, check=False)
            assert failure.returncode != 0 and b'ImportError' in failure.stderr, failure.stderr
            assert str(native_path).encode() in failure.stderr, failure.stderr
            assert b'Unable to load' in failure.stderr or b'dlopen' in failure.stderr, failure.stderr
            print('json+direct zlib', 'AOT' if aot else 'JIT', '三流/搬迁/加载路径/缺库通过', flush=True)


if __name__ == '__main__':
    main()
