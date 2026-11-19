#!/usr/bin/env python3
"""真实 CP/JIT/Native 联合分发：catalog 源码、显式资源、v3 C ABI 和搬迁失败保旧包。"""
import argparse
import ctypes
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
    parser.add_argument('--python', default=sys.executable)
    parser.add_argument('--out', type=Path, required=True)
    args = parser.parse_args()
    args.ryupy = args.ryupy.resolve()
    args.out.mkdir(parents=True, exist_ok=True)
    work = Path(tempfile.mkdtemp(prefix='stdlib-dist-', dir=args.out.resolve()))
    logs = work / 'logs'
    logs.mkdir()
    records = {'status': 'failed', 'platform': platform.platform(), 'steps': []}
    env = {**os.environ, 'DOTNET_CLI_UI_LANGUAGE': 'en-US', 'PYTHONDONTWRITEBYTECODE': '1'}
    dotnet = shutil.which(os.environ.get('DOTNET', 'dotnet'))
    assert dotnet

    def run(label, command, *, check=True, cwd=work, process_env=env, timeout=240):
        result = subprocess.run(list(map(str, command)), cwd=cwd, env=process_env,
                                capture_output=True, timeout=timeout)
        (logs / (label + '.stdout')).write_bytes(result.stdout)
        (logs / (label + '.stderr')).write_bytes(result.stderr)
        records['steps'].append({'step': label, 'exit': result.returncode})
        print(label, result.returncode, flush=True)
        if check:
            assert result.returncode == 0, (label, result.stderr.decode(errors='replace'))
        return result

    def save(path, value):
        path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

    try:
        fixture = Path(__file__).resolve().parent
        source = work / '中文 & source'
        source.mkdir()
        for name in ('app.py', 'library.c'):
            shutil.copy2(fixture / name, source / name)
        shutil.copytree(fixture / 'pkgdata', source / 'pkgdata')
        win, mac = sys.platform == 'win32', sys.platform == 'darwin'
        machine = platform.machine().lower()
        assert machine in ('arm64', 'aarch64', 'x86_64', 'amd64'), machine
        rid = ('win' if win else 'osx' if mac else 'linux') + ('-arm64' if machine in ('arm64', 'aarch64') else '-x64')
        records['rid'] = rid
        shared = source / ('probe.dll' if win else 'libprobe.dylib' if mac else 'libprobe.so')
        archive = source / ('probe.lib' if win else 'probe.a')
        obj = source / ('probe.obj' if win else 'probe.o')
        if win:
            run('shared', ['cl', '/nologo', '/O2', '/MT', '/LD', source / 'library.c',
                           '/Fo' + str(source / 'shared.obj'), '/Fe' + str(shared)])
            run('object', ['cl', '/nologo', '/O2', '/MT', '/c', source / 'library.c', '/Fo' + str(obj)])
            run('archive', ['lib', '/NOLOGO', '/OUT:' + str(archive), obj])
        else:
            minimum = ['-mmacosx-version-min=12.0'] if mac else []
            run('shared', ['cc', *minimum, '-O2', '-fPIC', '-dynamiclib' if mac else '-shared',
                           source / 'library.c', '-o', shared])
            run('object', ['cc', *minimum, '-O2', '-c', source / 'library.c', '-o', obj])
            run('archive', ['ar', 'rcs', archive, obj])
        binding = {'schema_version': 3, 'module': 'native_probe', 'library': 'probe_c',
                   'exports': [{'name': 'sum_bytes', 'symbol': 'probe_sum_bytes', 'signature': {
                       'calling_convention': 'cdecl', 'parameters': [{'name': 'data', 'type': 'bytes',
                           'ownership': 'borrowed_call', 'length': 'usize'}], 'returns': 'u64'}}],
                   'target': {'rid': rid, 'shared': shared.name, 'archive': archive.name}}
        save(source / 'binding.json', binding)
        save(source / 'resources.json', {'schema_version': 1,
             'modules': [{'module': 'pkgdata', 'files': ['data.txt']}]})
        # CPython 动态加载的唯一用途是自有 C fixture 参考；生产调用走静态生成绑定。
        reference = ("import ctypes,runpy,sys,types; lib=ctypes.CDLL(sys.argv[1]); "
                     "f=lib.probe_sum_bytes; f.argtypes=[ctypes.c_void_p,ctypes.c_size_t]; "
                     "f.restype=ctypes.c_uint64; m=types.ModuleType('native_probe'); "
                     "m.sum_bytes=lambda data:f(data,len(data)); sys.modules['native_probe']=m; "
                     "sys.path.insert(0,sys.argv[2]); runpy.run_path(sys.argv[2]+'/app.py')")
        baseline = run('cpython', [args.python, '-c', reference, shared, source]).stdout
        common = [args.ryupy, 'build', source / 'app.py', '--native-binding', source / 'binding.json']
        jit = run('jit-build', [*common, '-o', work / 'jit'])
        entries = [Path(line.removeprefix('已生成 ')) for line in jit.stderr.decode().splitlines()
                   if line.startswith('已生成 ')]
        assert len(entries) == 1
        # JIT 没有资源位置清单时使用源码包路径；本合同的资源搬迁通过 AOT 验收。
        assert run('jit-run', [dotnet, entries[0]]).stdout == baseline
        report = json.loads((work / 'jit/app.source-plan.json').read_text())
        assert report['modules']['native_probe']['provider'] == 'c_abi_binding'
        assert report['modules']['colorsys']['provider'] == 'cpython_source'
        assert report['static_closure_complete']
        output = work / 'publish'
        built = run('aot-build', [*common, '--aot', '--resources', source / 'resources.json', '-o', output])
        layout = reported_aot(built.stderr, output, 'app', rid)
        manifest = json.loads(layout.manifest.read_text())
        assert manifest['schema_version'] == 9
        assert manifest['scope']['declared_resources_complete']
        assert manifest['scope']['automatic_module_locations_complete']
        assert any(item['module_name'] == 'colorsys' for item in manifest['sources'])
        assert len(manifest['resources']) == 1
        assert run('native-original', [layout.native]).stdout == baseline
        old = {p.relative_to(layout.dist): p.read_bytes() for p in layout.dist.rglob('*') if p.is_file()}
        (source / 'pkgdata/data.txt').unlink()
        failed = run('missing-resource-build', [*common, '--aot', '--resources', source / 'resources.json',
                                               '-o', output], check=False)
        assert failed.returncode != 0
        assert old == {p.relative_to(layout.dist): p.read_bytes() for p in layout.dist.rglob('*') if p.is_file()}
        moved = work / '中文 & relocated'
        shutil.move(layout.dist, moved)
        shutil.rmtree(source)
        shutil.rmtree(layout.build)
        shutil.rmtree(work / 'jit')
        cwd = work / 'empty-cwd'
        cwd.mkdir()
        clean = {key: value for key, value in env.items() if not key.startswith(('PYTHON', 'DOTNET', 'LD_', 'DYLD_', 'RYUPY_'))}
        clean['PATH'] = str(cwd)
        assert run('native-relocated', [moved / layout.native.name], cwd=cwd, process_env=clean).stdout == baseline
        resource = moved / manifest['resources'][0]['relative_path']
        resource.unlink()
        missing = run('native-missing-resource', [moved / layout.native.name], cwd=cwd,
                      process_env=clean, check=False)
        assert missing.returncode != 0 and b'FileNotFoundError' in missing.stderr
        records['status'] = 'passed'
    finally:
        save(work / 'result.json', records)
        print(work, flush=True)


if __name__ == '__main__':
    main()
