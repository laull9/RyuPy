#!/usr/bin/env python3
"""显式Mach-O组件真实产品门：两独立ABI/布局、缺失冲突版本负例、foreign mapping与删除输入搬迁。"""
import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import shutil
import struct
import subprocess
import sys
import tempfile

from fixtures import CASES, prepare
ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT))
from tests.cpython.support import private_distribution, declaration, audit_native_paths


def run(command, cwd, env, log, success=True):
    result = subprocess.run(list(map(str, command)), cwd=cwd, env=env,
                            capture_output=True, timeout=900)
    log.write_bytes(result.stdout + result.stderr)
    if success and result.returncode:
        raise AssertionError((command, result.returncode, result.stderr.decode()))
    return result


def version_negative(binary, load_name, major):
    """修改手写fixture的LC_LOAD_DYLIB要求；测试期间不执行损坏映像。"""
    data = bytearray(binary.read_bytes())
    assert data[:4] == b'\xcf\xfa\xed\xfe'
    offset = 32
    for _ in range(struct.unpack_from('<I', data, 16)[0]):
        command, size = struct.unpack_from('<II', data, offset)
        if command == 0xc:
            name_offset = struct.unpack_from('<I', data, offset + 8)[0]
            name = data[offset + name_offset:offset + size].split(b'\0', 1)[0].decode()
            if name == load_name:
                struct.pack_into('<I', data, offset + 20, (major + 1) << 16)
                binary.write_bytes(data)
                return
        offset += size
    raise AssertionError('fixture缺少要求测试的加载边')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ryupy', type=Path, required=True)
    parser.add_argument('--python', type=Path, required=True)
    parser.add_argument('--distribution', type=Path, default=Path.home() / '.local/share/uv/python/cpython-3.12.13-macos-aarch64-none')
    parser.add_argument('--metadata', type=Path, default=Path.home() / '.cache/ryupy/cpython-provider/pbs-20260623-metadata/python')
    parser.add_argument('--mode', choices=['jit', 'standalone'], action='append')
    parser.add_argument('--evidence', type=Path, default=Path.home() / '.cache/ryupy/validation/native-components')
    args = parser.parse_args()
    assert sys.platform == 'darwin', '本竖切是Mach-O实际门；ELF/PE组件协议另验'
    evidence = args.evidence.expanduser().resolve() / datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ')
    assert not evidence.is_relative_to(ROOT)
    evidence.mkdir(parents=True)
    print(evidence, flush=True)
    env = {k: v for k, v in os.environ.items()
           if not k.startswith(('PYTHON', 'RYUPY_', 'LD_', 'DYLD_', 'VIRTUAL_ENV', 'CONDA'))}
    env.update(PYTHONDONTWRITEBYTECODE='1', PYTHONHASHSEED='0', TZ='UTC')
    rows = []
    with tempfile.TemporaryDirectory(prefix='ryupy-native-components-') as temporary:
        work = Path(temporary).resolve()
        inputs = work / '构建 输入'
        inputs.mkdir()
        venv = inputs / 'venv'
        run(['uv', 'venv', '--python', args.python, venv], work, env, evidence / 'venv.log')
        python = venv / 'bin/python'
        version = run([python, '-I', '-c', 'import platform; print(platform.python_version())'], work, env, evidence / 'version.log')
        assert version.stdout.strip() == b'3.12.13'
        base = private_distribution(args.distribution, inputs, args.metadata)
        base_site = base / 'lib/python3.12/site-packages'
        base_site.mkdir(exist_ok=True)
        (base_site / 'retained_legacy_marker.py').write_text('VALUE = "旧显式provider保留基础site"\n')
        products = []
        for case in CASES:
            label = case['name']
            root = inputs / label
            root.mkdir()
            prefix, extension, package, component = prepare(case, root, python, env)
            run(['uv', 'pip', 'install', '--python', python, '--no-deps', package], work, env, evidence / (label + '-install.log'))
            installed = venv / 'lib/python3.12/site-packages' / extension.name
            manifest = root / 'components.json'
            manifest.write_text(json.dumps(component))
            entry = root / 'entry.py'
            factory = '.make()' if case['custom'] else '.mapping()'
            entry.write_text('"""CIL入口，消费通用原生依赖、CP mapping和index协议。"""\n'
                             'import ' + case['module'] + ' as provider\n'
                             'source = provider' + factory + '\n'
                             'result = {"prefix": 0, **source, "left": 99, "tail": 9}\n'
                             'print(result, oct(provider.make()))\n'
                             'for label in ["text", "emoji", "lone", "big", "one", "float", "zero", "bool", "none"]:\n'
                             '    foreign = provider.scalar(label)\n'
                             '    local = {"text": "left", "emoji": "\\U0001f600", "lone": chr(0xd800) + "x", "big": 1 << 20000, "one": 1, "float": True, "zero": 0, "bool": 1.0, "none": None}[label]\n'
                             '    left = {**provider.key_mapping(foreign, 1), local: 2}\n'
                             '    right = {local: 1, **provider.key_mapping(foreign, 2)}\n'
                             '    print(label, len(left), left[local], next(iter(left)) is foreign, len(right), right[foreign], next(iter(right)) is local)\n'
                             'emoji = provider.scalar("emoji")\n'
                             'pair = provider.scalar("pair")\n'
                             'a = {**provider.key_mapping(emoji, 1), **provider.key_mapping(pair, 2), "\\U0001f600": 3}\n'
                             'b = {"\\U0001f600": 1, **provider.key_mapping(pair, 2), **provider.key_mapping(emoji, 3)}\n'
                             'print("codepoints", len(a), a[emoji], a[pair], next(iter(a)) is emoji, len(b), b[emoji], b[pair])\n'
                             'n1 = provider.scalar("nan")\n'
                             'n2 = provider.scalar("nan")\n'
                             'nans = {**provider.key_mapping(n1, 1), **provider.key_mapping(n1, 2), **provider.key_mapping(n2, 3)}\n'
                             'print("nan", len(nans), nans[n1], nans[n2], next(iter(nans)) is n1)\n')
            oracle = run([python, '-B', entry], work, dict(env, DYLD_LIBRARY_PATH=str(prefix / 'lib')),
                         evidence / (label + '-oracle.log'))
            config = declaration(base, [{'source': str(installed), 'destination': extension.name}], [case['module']])
            provider = root / 'provider.json'
            provider.write_text(json.dumps(config))
            # 第二布局专门保护“只有native-components，无dependencies收集器”的旧显式provider。
            provider_flags = ['--cpython-provider', provider] if case['custom'] else []
            build_env = dict(env, VIRTUAL_ENV=str(venv))
            def invoke(name, selected=component, expect=None, use_component=True):
                manifest.write_text(json.dumps(selected))
                flags = ['--cpython-native-components', manifest] if use_component else []
                result = run([args.ryupy.resolve(), entry, '--mode', 'jit', '-o', work / ('bad-' + label + '-' + name),
                              *provider_flags, *flags], work, build_env, evidence / (label + '-' + name + '.log'), False)
                assert result.returncode and expect.encode() in result.stderr, result.stderr
                rows.append({'case': label, 'negative': name, 'status': 'rejected'})
            invoke('undeclared', expect='未唯一收集', use_component=False)
            def changed(): return json.loads(json.dumps(component))
            bad = changed(); bad['components'][0]['libraries'][0]['path'] = 'lib/missing.dylib'
            invoke('missing-source', bad, '文件缺失')
            bad = changed(); bad['components'][0]['libraries'][0]['current_version'] = '0.0.0'
            invoke('declared-version', bad, '声明版本')
            bad = changed(); bad['components'][0]['libraries'][0]['destination'] = extension.name
            invoke('destination-conflict', bad, '目标冲突')
            bad = changed(); clone = json.loads(json.dumps(bad['components'][0])); clone['name'] += '_second'
            clone['libraries'][0]['destination'] = 'another/' + clone['libraries'][0]['destination']
            clone['licenses'][0]['destination'] = 'another/' + clone['licenses'][0]['destination']
            bad['components'].append(clone)
            invoke('load-name-conflict', bad, '加载名冲突')
            bad = changed(); bad['components'][0]['licenses'] = []
            invoke('missing-license', bad, '许可证')
            original = installed.read_bytes()
            try:
                version_negative(installed, component['components'][0]['libraries'][0]['load_names'][0], case['major'])
                invoke('actual-abi-version', component, '版本不兼容')
            finally:
                installed.write_bytes(original)
            manifest.write_text(json.dumps(component))
            for mode in args.mode or ['jit', 'standalone']:
                name = label + '-' + mode
                output = work / ('build-' + name)
                built = run([args.ryupy.resolve(), entry, '--mode', mode, '-o', output, *provider_flags,
                             '--cpython-native-components', manifest], work, build_env, evidence / (name + '-build.log'))
                executable = [Path(line[len('已生成 '):]) for line in built.stderr.decode().splitlines()
                              if line.startswith('已生成 ')][-1]
                report = json.loads((executable.parent / 'cpython-provider.json').read_text())
                collection = report['dependency_collection']
                assert collection['native_components'][0]['licenses']
                if case['custom']:
                    assert 'collector' not in collection
                    assert (executable.parent / 'python/lib/python3.12/site-packages/retained_legacy_marker.py').is_file()
                shutil.copy2(executable.parent / 'cpython-provider.json', evidence / (name + '-provider.json'))
                audit_native_paths(executable.parent)
                relocated = work / ('搬迁 空格 ' + name)
                shutil.copytree(executable.parent, relocated)
                products.append((name, mode, relocated / executable.name, oracle))
                shutil.rmtree(output)
            identity = subprocess.check_output(['otool', '-D', prefix / 'lib' / Path(case['destination']).name], text=True)
            assert component['components'][0]['libraries'][0]['install_name'] in identity
        shutil.rmtree(inputs)
        empty = work / 'empty-path'; empty.mkdir()
        dotnet = shutil.which('dotnet')
        for name, mode, executable, oracle in products:
            command = [dotnet, executable] if mode == 'jit' else [executable]
            actual = run(command, empty, dict(env, PATH=str(empty), PYTHONHOME='/missing-python', PYTHONPATH='/missing-site'),
                         evidence / (name + '-run.log'))
            assert (actual.returncode, actual.stdout, actual.stderr) == (oracle.returncode, oracle.stdout, oracle.stderr)
            rows.append({'case': name, 'status': 'deleted_inputs_relocated_pass', 'execution': 'cil_foreign_mapping_native_dependency'})
            print('PASS', name, flush=True)
    (evidence / 'results.json').write_text(json.dumps(rows, ensure_ascii=False, indent=2) + '\n')
    (evidence / 'cleanup.json').write_text(json.dumps({'temporary_tree_removed': not work.exists()}))


if __name__ == '__main__':
    main()
