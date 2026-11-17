#!/usr/bin/env python3
"""固定 HEAD provider 仓外独立 JIT 生命周期门；保留成功/失败三流，清理全部构建产物。"""
import argparse
import io
import tarfile
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import shlex
import shutil
import subprocess
import tempfile


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--python', type=Path, required=True)
    parser.add_argument('--case', action='append', choices=['scoped', 'unscoped', 'keep-owned', 'external', 'external-d'])
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[3]
    source = Path(__file__).resolve().parent
    logs = Path.home()/'.cache/ryupy/validation/adr0228'
    logs.mkdir(parents=True, exist_ok=True)
    path = logs/(datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ')+'.log')
    print('LOG', path, flush=True)
    with path.open('x') as log, tempfile.TemporaryDirectory(prefix='ryupy-0228-', dir='/tmp') as tmp:
        work = Path(tmp)

        def run(command, timeout=120, required=True, env=None, quiet=False):
            log.write('\nCOMMAND '+shlex.join(map(str, command))+'\n'); log.flush()
            result = subprocess.run(command, cwd=work, text=True, capture_output=True, timeout=timeout, env=env)
            log.write(f'EXIT {result.returncode}\nSTDOUT\n{result.stdout}\nSTDERR\n{result.stderr}\n'); log.flush()
            if not quiet:
                print(result.stdout, end='', flush=True)
            if result.stderr:
                print(result.stderr, end='', flush=True)
            if required and result.returncode:
                raise RuntimeError('命令失败，见 '+str(path))
            return result

        # 并行产品开发不可渗入本轮：只读取冻结 git 对象，不修改工作树。
        revision = '1489dd2a'
        provider = work/'provider'; provider.mkdir()
        names = run(['git', '-C', str(root), 'ls-tree', '-r', '--name-only', revision,
                     'runtime/RyuPy.CPython'], quiet=True).stdout.splitlines()
        for name in names:
            target = provider/Path(name).relative_to('runtime/RyuPy.CPython')
            target.parent.mkdir(parents=True, exist_ok=True)
            content = subprocess.check_output(['git', '-C', str(root), 'show', revision+':'+name])
            target.write_bytes(content)
        shutil.copy2(source/'HostProbe.cs.in', provider/'HostProbe.cs')
        # 仅对仓外冻结副本作两处所有权观察接线；现有仓库源码没有修改。
        # 钩子在 constructor 登记强 owner，避免依赖 weak identity 表或 CLR GC 时机。
        def connect(filename, old, new):
            target = provider/filename
            text = target.read_text()
            assert text.count(old) == 1, '固定源码接线漂移：'+filename
            target.write_text(text.replace(old, new))
        connect('PythonObject.cs', 'token = host.Track(pointer);',
                'token = host.Track(pointer); host.ObserveProgramOwner(this);')
        connect('CPythonStockThinFunctions.cs', 'Interlocked.Increment(ref CPythonHost.StockThinOwnedHandles);',
                'Interlocked.Increment(ref CPythonHost.StockThinOwnedHandles); function.Host.ObserveProgramThinOwner(this);')
        log.write('SOURCE EXPERIMENT constructor strong owner hooks: PythonObject + StockThinFunctionOwner\n')
        cfg = json.loads(run([str(args.python.resolve()), '-I', '-B', '-c',
            'import sys,sysconfig,json; print(json.dumps(dict(home=sys.base_prefix,version=".".join(map(str,sys.version_info[:3])),lib=sysconfig.get_config_var("LIBDIR"),library=sysconfig.get_config_var("LDLIBRARY"),gil=sysconfig.get_config_var("Py_GIL_DISABLED"))))']).stdout)
        assert cfg['version'] in ('3.12.13', '3.13.3') and not cfg['gil']
        series = '.'.join(cfg['version'].split('.')[:2])
        # Runtime 同样从固定版本仓外构建，杜绝并行中性 API 开发的 DLL 渗入。
        archive = subprocess.check_output(['git', '-C', str(root), 'archive', revision, 'runtime/RyuPy.Runtime'])
        frozen = work/'frozen'
        with tarfile.open(fileobj=io.BytesIO(archive)) as tar:
            tar.extractall(frozen, filter='data')
        runtime_output = work/'runtime-output'
        run(['dotnet', 'build', str(frozen/'runtime/RyuPy.Runtime/RyuPy.Runtime.csproj'),
             '--disable-build-servers', '-c', 'Release', '-o', str(runtime_output), '-v', 'minimal'],
            env=dict(os.environ, DOTNET_ROLL_FORWARD='Major'))
        shutil.copy2(runtime_output/'RyuPy.Runtime.dll', work/'RyuPy.Runtime.dll')
        log.write(f'BASE provider+Runtime {revision}\nPYTHON {cfg}\n')
        props = subprocess.check_output(['git', '-C', str(root), 'show', revision+':runtime/RyuPy.Runtime/RuntimeDependencies.props'])
        (provider/'RuntimeDependencies.props').write_bytes(props)
        (provider/'ThirdPartyNotices.sqlite3.txt').write_bytes(subprocess.check_output(['git', '-C', str(root), 'show', revision+':runtime/RyuPy.Runtime/ThirdPartyNotices.sqlite3.txt']))
        (provider/'Provider.csproj').write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<TargetFramework>net10.0</TargetFramework><AssemblyName>RyuPy.CPython</AssemblyName><AssemblyVersion>1.0.0.0</AssemblyVersion>
<ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><AllowUnsafeBlocks>true</AllowUnsafeBlocks>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup><Import Project="RuntimeDependencies.props"/>
<ItemGroup><Reference Include="RyuPy.Runtime"><HintPath>../RyuPy.Runtime.dll</HintPath></Reference>
<AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo"><_Parameter1>RyuPy.CPython.Tests</_Parameter1></AssemblyAttribute>
</ItemGroup></Project>''')
        runner = work/'runner'; runner.mkdir()
        shutil.copy2(source/'Program.cs.in', runner/'Program.cs')
        (runner/'Runner.csproj').write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><AssemblyName>RyuPy.CPython.Tests</AssemblyName>
<ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup><ItemGroup><ProjectReference Include="../provider/Provider.csproj"/>
<Reference Include="RyuPy.Runtime"><HintPath>../RyuPy.Runtime.dll</HintPath></Reference></ItemGroup></Project>''')
        output = work/'output'
        env = dict(os.environ, DOTNET_ROLL_FORWARD='Major')
        run(['dotnet', 'build', str(runner/'Runner.csproj'), '--disable-build-servers', '-c', 'Release', '-o', str(output), '-v', 'minimal'], env=env)
        shim = output/'libryupy_cpython.dylib'
        run(['clang', '-dynamiclib', '-O2', '-Wall', '-Wextra', '-Werror',
             '-I'+str(Path(cfg['home'])/'include'/f'python{series}'), '-I'+str(provider/'native'),
             str(source/'NativeProbe.c'), str(Path(cfg['lib'])/cfg['library']), '-o', str(shim)])
        config = work/'host.json'
        config.write_text(json.dumps(dict(schema=1, python_version=cfg['version'], development=True,
            home=cfg['home'], library=str(Path(cfg['lib'])/cfg['library']), shim=str(shim),
            search_paths=[str(Path(cfg['home'])/'lib'/f'python{series}'), str(Path(cfg['home'])/'lib'/f'python{series}'/'lib-dynload')],
            modules=['builtins', 'gc', 'sys', 'atexit'], container_transfer='proxy')))
        failed = False
        for case in args.case or ['unscoped', 'keep-owned', 'scoped', 'external', 'external-d']:
            result = run(['dotnet', str(output/'RyuPy.CPython.Tests.dll'), str(config), case], required=False, env=env)
            # 失败对照同样保留，SIGSEGV/异常退出永远不能作为预期泄漏通过。
            expected = 2 if case in ('unscoped', 'keep-owned') else 0
            passed = result.returncode == expected and (not result.stderr if expected == 0 else 'AFTER ' in result.stdout)
            line = f'CASE python={cfg["version"]} case={case} exit={result.returncode} expected={expected} accepted={passed}\n'
            print(line, end='', flush=True); log.write(line); log.flush()
            failed |= not passed
        log.write('CLEANUP 仓外构建目录由 TemporaryDirectory 清理\n')
        if failed:
            raise RuntimeError('生命周期门失败，见 '+str(path))


if __name__ == '__main__':
    main()
