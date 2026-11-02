#!/usr/bin/env python3
"""真实同宿主NativeAOT迁移验收；mac/windows不冒无SDK干净机器证明。"""
import argparse
import hashlib
import json
import os
import re
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import time
from smoke import MODES, compare, executable, published_native
from platform_runner import Runner
from platform_support import host, relocate, audit, clean_environment


def published_platform_native(stderr, output, rid):
    if rid != 'win-x64':
        return published_native(stderr, output)
    # Windows canonicalize可能返回\\?\前缀；用真实文件身份核父目录，不裁剪UNC字串。
    generated = [line.removeprefix('已生成 ') for line in
                 stderr.decode('utf-8', errors='strict').splitlines()
                 if line.startswith('已生成 ')]
    candidates = [Path(value) for value in generated if not value.lower().endswith('.dll')]
    if len(candidates) != 1:
        raise AssertionError(f'CLI未报告唯一native产物：{generated}')
    native = candidates[0]
    if not native.is_absolute() or not native.is_file():
        raise AssertionError(f'CLI native不是现存绝对文件：{native}')
    resolved = native.resolve()
    publish, root = resolved.parent, resolved.parent.parent
    if (publish.name != 'publish' or not root.name.startswith('ryupy-aot-')
            or not os.path.samefile(root.parent, output)):
        raise AssertionError(f'native不属于本轮独占publish目录：{resolved}')
    return resolved


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--out', required=True, type=Path)
    parser.add_argument('--python', default='python3.12')
    parser.add_argument('--dotnet', default=os.environ.get('DOTNET', 'dotnet'))
    parser.add_argument('--ryupy', required=True)
    parser.add_argument('--rid', required=True, choices=['linux-x64', 'osx-arm64', 'win-x64'])
    args = parser.parse_args()
    metadata = host(args.rid)
    if args.rid == 'linux-x64':
        # 原Linux Docker/语义路径原样保留；新入口不削减其干净镜像证据。
        return subprocess.call([sys.executable, str(Path(__file__).with_name('smoke.py')),
            '--out', str(args.out), '--python', args.python, '--dotnet', args.dotnet,
            '--ryupy', args.ryupy, '--rid', args.rid])
    args.out.mkdir(parents=True, exist_ok=True)
    work = Path(tempfile.mkdtemp(prefix='aot-platform-', dir=args.out.resolve()))
    logs = work/'logs'
    logs.mkdir()
    print(f'本轮产物与日志：{work}', flush=True)
    run = Runner(logs, deadline=time.monotonic() + 30 * 60)
    result = {'rid': args.rid, 'host': metadata, 'status': 'failed',
              'clean_machine_without_runtime_tested': False,
              'scope': 'relocated native on SDK-installed hosted runner, restricted environment'}
    try:
        python, dotnet, ryupy = map(executable, [args.python, args.dotnet, args.ryupy])
        env = {**os.environ, 'DOTNET': dotnet, 'PYTHONUTF8': '1', 'PYTHONIOENCODING': 'utf-8',
               'DOTNET_CLI_UI_LANGUAGE': 'en-US'}
        version = run.run('python-version', [python, '-c',
            "import sys; print(sys.implementation.name); print('.'.join(map(str,sys.version_info[:3])))"],
            cwd=work, env=env, check=True, timeout=30)
        assert version.stdout.decode().splitlines() == ['cpython', '3.12.13']
        info = run.run('dotnet-info', [dotnet, '--info'], cwd=work, env=env, check=True, timeout=30)
        # 固定CLI语言后校验真实Host架构与SDK RID；不以runner label代替工具链事实。
        info_text = info.stdout.decode('utf-8-sig')
        architectures = re.findall(r'^\s*Architecture:\s*(\S+)\s*$', info_text, re.M)
        rids = re.findall(r'^\s*RID:\s*(\S+)\s*$', info_text, re.M)
        expected_arch = 'arm64' if args.rid == 'osx-arm64' else 'x64'
        if not architectures or any(value != expected_arch for value in architectures) or rids != [args.rid]:
            raise AssertionError(f'.NET实际架构/RID不符：{architectures}, {rids}')
        rust = run.run('rustc-version', [executable('rustc'), '-vV'], cwd=work, env=env, check=True, timeout=30)
        rust_hosts = re.findall(r'^host: (\S+)\s*$', rust.stdout.decode('utf-8'), re.M)
        expected_host = 'aarch64-apple-darwin' if args.rid == 'osx-arm64' else 'x86_64-pc-windows-msvc'
        if rust_hosts != [expected_host]:
            raise AssertionError(f'Rust实际host不符：{rust_hosts}')
        result['toolchain_architecture'] = {'dotnet_architectures': architectures,
                                          'dotnet_rids': rids, 'rust_host': rust_hosts[0]}
        tools = {}
        if args.rid == 'osx-arm64':
            tools['otool'] = executable('otool')
            run.run('xcode-version', [executable('xcodebuild'), '-version'], cwd=work, check=True, timeout=30)
            run.run('clang-version', [executable('clang'), '--version'], cwd=work, check=True, timeout=30)
        else:
            tools['dumpbin'] = executable('dumpbin')  # workflow预先选择原生x64 VS工具环境。
            run.run('dumpbin-version', [tools['dumpbin'], '/?'], cwd=work, timeout=30)
        result['cli_sha256'] = hashlib.sha256(Path(ryupy).read_bytes()).hexdigest()
        source, il = work/'源码 & input with spaces', work/'产物 & output with spaces'
        source.mkdir()
        result['source_hashes'] = {}
        for name in ('main.py', 'state.py'):
            original = Path(__file__).parent/'fixtures'/name
            result['source_hashes'][name] = hashlib.sha256(original.read_bytes()).hexdigest()
            shutil.copy2(original, source/name)
        run.run('compile', [ryupy, 'build', str(source/'main.py'), '--out-dir', str(il)],
                cwd=work, env=env, check=True, timeout=600)
        # 删除构建树前锁实际被JIT/ILC消费的程序IL与runtime，而非只记CLI。
        result['il_sha256'] = hashlib.sha256((il/'main.dll').read_bytes()).hexdigest()
        result['runtime_sha256'] = hashlib.sha256((il/'RyuPy.Runtime.dll').read_bytes()).hexdigest()
        jit = {}
        for mode, values in MODES.items():
            reference = run.run(f'python-{mode}', [python, str(source/'main.py'), *values], cwd=work, env=env, timeout=30)
            jit[mode] = run.run(f'jit-{mode}', [dotnet, str(il/'main.dll'), *values], cwd=work, env=env, timeout=30)
            compare(reference, jit[mode], mode, python=True)
        published = run.run('publish', [ryupy, 'build', str(source/'main.py'), '--out-dir', str(il), '--aot'],
                            cwd=work, env=env, check=True, timeout=1800)
        native = published_platform_native(published.stderr, il, args.rid)
        result['aot_il_sha256'] = hashlib.sha256((il/'main.dll').read_bytes()).hexdigest()
        result['aot_runtime_sha256'] = hashlib.sha256((il/'RyuPy.Runtime.dll').read_bytes()).hexdigest()
        if hashlib.sha256((il/'RyuPy.Runtime.dll').read_bytes()).hexdigest() != result['runtime_sha256']:
            raise AssertionError('AOT与JIT runtime字节不同')
        for name in ('publish.stdout.log', 'publish.stderr.log'):
            shutil.copy2(native.parent.parent/name, logs/('driver-'+name))
        relocated = work/'搬迁 & native with spaces'
        binary, manifest = relocate(native, relocated, args.rid)
        result['deployment_manifest'] = manifest
        result['dependencies'] = audit(run, relocated, args.rid, tools)
        # 所有被删目录均本轮创建；不可删除工具链、checkout或用户out父目录。
        for directory in (source, il):
            if directory.is_symlink() or directory.resolve().parent != work.resolve():
                raise AssertionError(f'拒绝清理非本轮直接owned目录：{directory}')
            shutil.rmtree(directory)
            assert not directory.exists()
        result['owned_build_inputs_removed'] = True
        clean = clean_environment(work)
        result['native_environment'] = clean
        for mode, values in MODES.items():
            actual = run.run(f'native-{mode}', [str(binary), *values], cwd=relocated, env=clean, timeout=30)
            compare(jit[mode], actual, mode)
        result['status'] = 'passed'
        result['python_jit_native_relocated'] = 'passed'
    except BaseException as error:
        result['failure'] = repr(error)
        raise
    finally:
        (work/'result.json').write_text(json.dumps(result, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
