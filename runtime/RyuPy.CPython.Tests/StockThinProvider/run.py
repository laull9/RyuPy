#!/usr/bin/env python3
"""在仓外编译真实 provider 程序集与独立 Runner，每个固定 stock 版本使用独立进程。"""
import argparse
import json
import os
import platform
from pathlib import Path
import subprocess
import tempfile


def run(command, cwd, env=None, timeout=55):
    result = subprocess.run(command, cwd=cwd, env=env, capture_output=True, text=True, timeout=timeout)
    if result.returncode or result.stderr:
        raise RuntimeError(f"失败 exit={result.returncode}: {command}\n{result.stdout}\n{result.stderr}")
    print(result.stdout, end="", flush=True)
    return result.stdout


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--python', type=Path, required=True)
    parser.add_argument('--resurrection', action='store_true', help='单独运行析构复活回归')
    parser.add_argument('--ordinary-only', action='store_true', help='仅诊断普通矩阵，显式排除复活门')
    parser.add_argument('--unit', action='store_true', help='同时运行六项托管协议单测')
    args = parser.parse_args()
    if platform.system() != 'Darwin' or platform.machine() != 'arm64':
        parser.error('当前 Runner 驱动只验收 macOS arm64')
    root = Path(__file__).resolve().parents[3]
    # 使用已验主线 Runtime 二进制，避免并行重建/改写产品 obj；本任务不修改 Runtime 源码。
    runtime = root/'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'
    if not runtime.is_file():
        parser.error('需要已构建的主线 Runtime DLL')
    with tempfile.TemporaryDirectory(prefix='ryupy-stock-thin-provider-') as temporary:
        work = Path(temporary)
        cfg = json.loads(run([str(args.python.resolve()), '-I', '-B', '-c',
            'import json,sys,sysconfig; print(json.dumps(dict(home=sys.base_prefix,version=".".join(map(str,sys.version_info[:3])),lib=sysconfig.get_config_var("LIBDIR"),library=sysconfig.get_config_var("LDLIBRARY"),gil=sysconfig.get_config_var("Py_GIL_DISABLED"))))'], work))
        if cfg['version'] not in ('3.12.13', '3.13.3') or cfg['gil']:
            parser.error('只验固定常规 GIL 3.12.13/3.13.3')
        series = '.'.join(cfg['version'].split('.')[:2])
        run(['sh', str(root/'runtime/RyuPy.CPython/native/build.sh'), cfg['home'], str(work), series], work)
        tools_library = work/'out/libstock_thin_test_tools.dylib'
        tools_library.parent.mkdir()
        run(['clang', '-dynamiclib', '-O2', '-Wall', '-Wextra', '-Werror',
             '-I'+str(Path(cfg['home'])/'include'/f'python{series}'),
             str(Path(__file__).parent/'NativeTools.c'), str(Path(cfg['lib'])/cfg['library']),
             '-o', str(tools_library)], work)
        (work/'provider').mkdir()
        (work/'runner').mkdir()
        provider = work/'provider/Provider.csproj'
        provider.write_text(f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<TargetFramework>net10.0</TargetFramework><AssemblyName>RyuPy.CPython</AssemblyName>
<AssemblyVersion>1.0.0.0</AssemblyVersion><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable>
<AllowUnsafeBlocks>true</AllowUnsafeBlocks><IsAotCompatible>true</IsAotCompatible>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors><EnableDefaultCompileItems>false</EnableDefaultCompileItems>
</PropertyGroup><ItemGroup><Compile Include="{root}/runtime/RyuPy.CPython/*.cs"/>
<Reference Include="RyuPy.Runtime"><HintPath>{runtime}</HintPath></Reference>
<AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo"><_Parameter1>RyuPy.CPython.Tests</_Parameter1></AssemblyAttribute>
</ItemGroup></Project>''')
        runner = work/'runner/Runner.csproj'
        runner.write_text(f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><AssemblyName>RyuPy.CPython.Tests</AssemblyName>
<ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<EnableDefaultCompileItems>false</EnableDefaultCompileItems>
</PropertyGroup><ItemGroup><Compile Include="{Path(__file__).parent}/*.cs"/>
<ProjectReference Include="{provider}"/><Reference Include="RyuPy.Runtime"><HintPath>{runtime}</HintPath></Reference>
</ItemGroup></Project>''')
        run(['dotnet', 'build', str(runner), '--disable-build-servers', '-c', 'Release', '-o', str(work/'out'), '--nologo', '-v', 'minimal'], work)
        config = work/'host.json'
        config.write_text(json.dumps(dict(schema=1, python_version=cfg['version'], development=True,
            home=cfg['home'], library=str(Path(cfg['lib'])/cfg['library']),
            shim=str(work/'libryupy_cpython.dylib'),
            search_paths=[str(Path(cfg['home'])/'lib'/f'python{series}'),
                          str(Path(cfg['home'])/'lib'/f'python{series}'/'lib-dynload')],
            modules=['builtins', '__main__', 'gc', 'types', 'sys'], container_transfer='synchronized')))
        env = dict(os.environ, DOTNET_ROLL_FORWARD='Major', PYTHONDONTWRITEBYTECODE='1')
        command = ['dotnet', str(work/'out/RyuPy.CPython.Tests.dll'), str(config)]
        if args.resurrection:
            run(command+['resurrect'], work, env)
        else:
            run(command, work, env)
            run(command+['blocked'], work, env)
            run(command+['watcher-full'], work, env)
            if not args.ordinary_only:
                run(command+['resurrect'], work, env)
        if args.unit:
            (work/'unit').mkdir()
            tests = work/'unit/Tests.csproj'
            tests.write_text(f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<TargetFramework>net10.0</TargetFramework><AssemblyName>RyuPy.CPython.Tests</AssemblyName>
<ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<EnableDefaultCompileItems>false</EnableDefaultCompileItems><IsPackable>false</IsPackable>
</PropertyGroup><ItemGroup><Compile Include="{root}/runtime/RyuPy.CPython.Tests/StockThinFunctionContractTests.cs"/>
<ProjectReference Include="{provider}"/><Reference Include="RyuPy.Runtime"><HintPath>{runtime}</HintPath></Reference>
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1"/><PackageReference Include="xunit" Version="2.9.3"/>
<PackageReference Include="xunit.runner.visualstudio" Version="3.1.4"/><Using Include="Xunit"/>
</ItemGroup></Project>''')
            run(['dotnet', 'test', str(tests), '--disable-build-servers', '-c', 'Release', '-v', 'minimal'], work, env)
    print('CLEANUP 仓外程序集/shim/配置已清理')


if __name__ == '__main__':
    main()
