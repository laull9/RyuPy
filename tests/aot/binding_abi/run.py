#!/usr/bin/env python3
"""v3 声明 ABI 实机验收：严格 ctypes 差分、静态 AOT、搬迁、错误与资源组合。"""
import argparse
import copy
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys
import tempfile

from contract import declaration

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
    work = Path(tempfile.mkdtemp(prefix='abi-', dir=args.out.resolve()))
    logs = work / 'logs'
    logs.mkdir()
    print(work, flush=True)
    records = {'status': 'failed', 'host': platform.platform(), 'steps': []}
    env = {**os.environ, 'DOTNET_CLI_UI_LANGUAGE': 'en-US', 'PYTHONUTF8': '1'}
    dotnet = shutil.which(os.environ.get('DOTNET', 'dotnet'))
    assert dotnet

    def run(label, command, *, check=True, cwd=work, timeout=90):
        command = list(map(str, command))
        result = subprocess.run(command, cwd=cwd, env=env, capture_output=True, timeout=timeout)
        (logs / (label + '.stdout')).write_bytes(result.stdout)
        (logs / (label + '.stderr')).write_bytes(result.stderr)
        records['steps'].append({'step': label, 'argv': command, 'exit': result.returncode})
        print(label, result.returncode, flush=True)
        if check:
            assert result.returncode == 0, (label, result.stderr.decode(errors='replace'))
        return result

    def save(path, value):
        path.write_text(json.dumps(value, ensure_ascii=True, indent=2), encoding='utf-8')

    def jit_entry(result):
        paths = [Path(line.removeprefix('已生成 ')) for line in result.stderr.decode().splitlines()
                 if line.startswith('已生成 ')]
        assert len(paths) == 1 and paths[0].is_file()
        return paths[0]

    try:
        win, mac = sys.platform == 'win32', sys.platform == 'darwin'
        assert (win and platform.machine().lower() in ('amd64', 'x86_64')) or (
            mac and platform.machine() == 'arm64') or (
            sys.platform == 'linux' and platform.machine() == 'x86_64')
        rid = 'win-x64' if win else 'osx-arm64' if mac else 'linux-x64'
        records['rid'] = rid
        source = work / '中文 & inputs'
        source.mkdir()
        fixture = Path(__file__).resolve().parent
        for name in ('app.py', 'library.c', 'contract.py'):
            shutil.copy2(fixture / name, source / name)
        app = source / 'app.py'
        shared = source / ('abi_c.dll' if win else 'libabi_c.dylib' if mac else 'libabi_c.so')
        missing = source / ('missing.dll' if win else 'missing.dylib' if mac else 'missing.so')
        archive, bad_archive = source / ('abi.lib' if win else 'abi.a'), source / ('bad.lib' if win else 'bad.a')
        for omitted, dynamic, destination in [(False, True, shared), (True, True, missing),
                                               (False, False, archive), (True, False, bad_archive)]:
            label = 'c-' + destination.name
            obj = source / (label + ('.obj' if win else '.o'))
            if win:
                run(label + '-compile', ['cl', '/nologo', '/O2', '/MT', '/c',
                    *(['/DOMIT_OPTIONAL'] if omitted else []), source / 'library.c', '/Fo' + str(obj)])
                run(label, ['link', '/NOLOGO', '/DLL', '/OUT:' + str(destination), obj] if dynamic
                    else ['lib', '/NOLOGO', '/OUT:' + str(destination), obj])
            else:
                flags = ['-mmacosx-version-min=12.0'] if mac else []
                run(label + '-compile', ['cc', '-O2', '-Wall', '-Wextra', '-Werror', '-fPIC', *flags,
                    *(['-DOMIT_OPTIONAL'] if omitted else []), '-c', source / 'library.c', '-o', obj])
                run(label, ['cc', *flags, '-dynamiclib' if mac else '-shared', obj, '-o', destination]
                    if dynamic else ['ar', 'rcs', destination, obj])
        binding = source / 'binding.json'
        declarations = declaration(rid, shared.name, archive.name)
        save(binding, declarations)
        (source / 'reference.py').write_text(
            'import contract, json, runpy, sys\nfrom pathlib import Path\n'
            f'sys.modules["abi"] = contract.reference(Path({str(shared)!r}), '
            'json.loads(Path("binding.json").read_text()))\n'
            'runpy.run_path("app.py", run_name="__main__")\n', encoding='utf-8')
        reference = run('cpython', [args.python, 'reference.py'], cwd=source).stdout
        unrelated = work / 'unrelated-cwd'
        unrelated.mkdir()

        def application(label, entry, *, native=False, mode=None):
            return run(label, ([entry] if native else [dotnet, entry]) + ([mode] if mode else []),
                       cwd=unrelated, timeout=40)

        jit_output, aot_output = work / 'jit-build', work / 'aot-build'
        command = [args.ryupy, 'build', app, '--native-binding', binding]
        jit = jit_entry(run('jit-build', [*command, '-o', jit_output]))
        assert application('jit', jit).stdout == reference
        jit_manifest = json.loads((jit.parent / 'ryupy-artifact.json').read_text())
        assert jit_manifest['schema_version'] == 7
        assert isinstance(jit_manifest['native_bindings'][0]['signature'], dict)
        layout = reported_aot(run('aot-build', [*command, '--aot', '-o', aot_output], timeout=600).stderr,
                              aot_output, 'app', rid)
        assert application('aot', layout.native, native=True).stdout == reference
        assert json.loads(layout.manifest.read_text())['schema_version'] == 7
        project = (layout.host / 'Host.csproj').read_text()
        assert project.count('DirectPInvoke Include=') == len(declarations['exports']) - 1
        assert project.count('NativeLibrary Include=') == 1

        # 声明验证必须先于任何原生文件读取；每项独立改动，保留具体中文诊断。
        invalid = []
        def case(label, change, expected):
            value = copy.deepcopy(declarations)
            change(value)
            value['target']['shared'] = 'absent.so'
            invalid.append((label, value, expected))
        def signature(value):
            return value['exports'][0]['signature']
        def parameters(value):
            return signature(value)['parameters']
        case('unknown-type', lambda value: parameters(value)[0].update(type='pointer'), '不支持的C ABI参数类型')
        case('unknown-field', lambda value: signature(value).update(marshal=True), '无效v3 ABI签名')
        case('null-scalar-ownership', lambda value: parameters(value)[0].update(ownership=None), '无效v3 ABI签名')
        case('null-buffer-ownership', lambda value: parameters(value)[0].update(type='bytes',
             ownership=None, length='usize'), '无效v3 ABI签名')
        case('stdcall', lambda value: signature(value).update(calling_convention='stdcall'), '仅支持cdecl')
        case('ownership', lambda value: parameters(value)[0].update(type='bytes'), 'borrowed_call')
        case('length', lambda value: parameters(value).__setitem__(0, {'name': 'data', 'type': 'bytes',
             'ownership': 'borrowed_call', 'length': 'u32'}), 'usize字节长度')
        case('buffer-return', lambda value: signature(value).update(returns='utf8'), '返回值必须')
        case('void-input', lambda value: parameters(value)[0].update(type='void'), '有效输入类型')
        case('too-many', lambda value: signature(value).update(parameters=[{'name': 'arg' + str(index),
             'type': 'i8'} for index in range(17)]), '最多16个')
        case('duplicate', lambda value: signature(value).update(parameters=parameters(value) * 2), '唯一ASCII')
        case('symbol-conflict', lambda value: value['exports'][-1].update(symbol='echo_i8'), '冲突ABI')
        case('old-signature', lambda value: value['exports'][0].update(signature='cdecl_i32_i32_i64'), '无效v3')
        case('v2-new-signature', lambda value: value.update(schema_version=2), '只支持固定')
        for label, value, expected in invalid:
            save(binding, value)
            result = run('reject-' + label, [*command, '-o', work / 'rejected'], check=False)
            assert result.returncode != 0 and expected in result.stderr.decode(errors='replace'), label
            assert not (work / 'rejected').exists()
        save(binding, declarations)

        # 缺符号由静态链接器拒绝；此前成功发布的目录原样可执行。
        declarations['target']['archive'] = bad_archive.name
        save(binding, declarations)
        failure = run('aot-missing-symbol', [*command, '--aot', '-o', aot_output], check=False, timeout=600)
        assert failure.returncode != 0 and 'optional' in failure.stderr.decode(errors='replace')
        assert application('aot-prior-survives', layout.native, native=True).stdout == reference
        declarations['target']['archive'] = archive.name
        save(binding, declarations)

        # v3 + 显式资源使用 schema8，共用同一发布事务。
        package = source / 'pkgdata'
        package.mkdir()
        (package / '__init__.py').write_text('', encoding='utf-8')
        (package / 'payload.txt').write_text('中文\x00payload', encoding='utf-8')
        resource_app = source / 'resource_app.py'
        resource_app.write_text('import pkgdata, abi\nfrom importlib.resources import files\n'
            'print(abi.utf8(files(pkgdata).joinpath("payload.txt").read_text(encoding="utf-8")))\n', encoding='utf-8')
        resources = source / 'resources.json'
        save(resources, {'schema_version': 1, 'modules': [{'module': 'pkgdata', 'files': ['payload.txt']}]})
        resource_output = work / 'resource-build'
        resource_layout = reported_aot(run('resource-aot-build', [args.ryupy, 'build', resource_app,
            '--native-binding', binding, '--resources', resources, '--aot', '-o', resource_output],
            timeout=600).stderr, resource_output, 'resource_app', rid)
        assert json.loads(resource_layout.manifest.read_text())['schema_version'] == 8
        resource_expected = str(len('中文\x00payload'.encode()) + sum('中文\x00payload'.encode())).encode() + os.linesep.encode()
        assert application('resource-aot', resource_layout.native, native=True).stdout == resource_expected

        moved_jit, moved_aot, moved_resource = work / 'moved-jit', work / 'moved-aot', work / 'moved-resource'
        shutil.move(jit.parent, moved_jit)
        shutil.move(layout.dist, moved_aot)
        shutil.move(resource_layout.dist, moved_resource)
        shutil.move(source, work / 'inputs.unavailable')
        shutil.move(aot_output, work / 'aot-build.unavailable')
        shutil.move(resource_output, work / 'resource-build.unavailable')
        jit, native = moved_jit / jit.name, moved_aot / layout.native.name
        assert application('jit-relocated', jit).stdout == reference
        assert application('aot-relocated', native, native=True).stdout == reference
        assert application('resource-relocated', moved_resource / resource_layout.native.name,
                           native=True).stdout == resource_expected
        relative = Path(jit_manifest['native_bindings'][0]['relative_file'])
        for label in ('missing-symbol', 'no-library', 'invalid-library'):
            destination = work / label
            shutil.copytree(moved_jit, destination)
            library = destination / relative
            if label == 'missing-symbol':
                shutil.copy2(work / 'inputs.unavailable' / missing.name, library)
            elif label == 'no-library':
                library.unlink()
            else:
                library.write_bytes(b'invalid image')
            assert application(label, destination / jit.name, mode='loader').stdout == b'loader-failure-ok' + os.linesep.encode()
        records.update(status='passed', relocated={'jit': str(jit), 'aot': str(native)},
                       scope='本宿主标量、受控借用输入、装载与返回异常、静态AOT及资源组合')
    finally:
        (work / 'result.json').write_text(json.dumps(records, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
