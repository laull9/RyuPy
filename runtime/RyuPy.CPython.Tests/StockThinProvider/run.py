#!/usr/bin/env python3
"""仓外构建独立 provider Runner；默认 JIT，--aot 显式验证真实 NativeAOT 静态 C# 业务叶子。"""
import argparse
from datetime import datetime, timezone
import json
import os
import platform
from pathlib import Path
import shlex
import shutil
import signal
import subprocess
import tempfile
from xml.sax.saxutils import escape


LOG_ROOT = Path.home()/'.cache/ryupy/validation/adr0208-20261117'


class ValidationLog:
    """即使构建失败或超时也持久保存命令、stdout、stderr；产物由临时目录单独管理。"""

    def __init__(self, mode, python):
        LOG_ROOT.mkdir(parents=True, exist_ok=True)
        stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ')
        label = python.resolve().parent.parent.name
        self.path = LOG_ROOT/f'stock-thin-provider-{mode}-{label}-{stamp}-{os.getpid()}.log'
        self.stream = self.path.open('x', encoding='utf-8')

    def write(self, value):
        self.stream.write(value)
        self.stream.flush()

    def run(self, command, cwd, env=None, timeout=55):
        self.write('\nCOMMAND '+shlex.join(map(str, command))+'\n')
        print('RUN '+shlex.join(map(str, command)), flush=True)
        process = subprocess.Popen(command, cwd=cwd, env=env, stdin=subprocess.DEVNULL,
                                   stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                   text=True, start_new_session=True)
        try:
            stdout, stderr = process.communicate(timeout=timeout)
        except BaseException:
            # 只终止本命令的独立进程组，防止失败清理后遗留 ilc/clang；不停止共享构建服务。
            try:
                os.killpg(process.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
            stdout, stderr = process.communicate()
            self.write('INTERRUPTED\nSTDOUT\n'+stdout+'\nSTDERR\n'+stderr+'\n')
            raise
        self.write(f'EXIT {process.returncode}\nSTDOUT\n{stdout}\nSTDERR\n{stderr}\n')
        print(stdout, end='', flush=True)
        if process.returncode or stderr:
            raise RuntimeError(f'命令失败 exit={process.returncode}，详情见 {self.path}\n{stderr}')
        return stdout

    def close(self):
        self.stream.close()


def xml_path(path):
    return escape(str(path), {'"': '&quot;'})


def write_projects(root, source, work, runtime):
    """仅引用已验 Runtime DLL；依赖版本沿用手写 props，不引入 Runtime ProjectReference。"""
    (work/'provider').mkdir()
    (work/'runner').mkdir()
    provider = work/'provider/Provider.csproj'
    provider.write_text(f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<TargetFramework>net10.0</TargetFramework><AssemblyName>RyuPy.CPython</AssemblyName>
<AssemblyVersion>1.0.0.0</AssemblyVersion><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable>
<AllowUnsafeBlocks>true</AllowUnsafeBlocks><IsAotCompatible>true</IsAotCompatible>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors><EnableDefaultCompileItems>false</EnableDefaultCompileItems>
</PropertyGroup><Import Project="{xml_path(root/'runtime/RyuPy.Runtime/RuntimeDependencies.props')}"/>
<ItemGroup><Compile Include="{xml_path(root/'runtime/RyuPy.CPython')}/*.cs"/>
<Reference Include="RyuPy.Runtime"><HintPath>{xml_path(runtime)}</HintPath></Reference>
<AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo"><_Parameter1>RyuPy.CPython.Tests</_Parameter1></AssemblyAttribute>
</ItemGroup></Project>''')
    runner = work/'runner/Runner.csproj'
    runner.write_text(f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><AssemblyName>RyuPy.CPython.Tests</AssemblyName>
<ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<IsAotCompatible>true</IsAotCompatible><ILLinkTreatWarningsAsErrors>true</ILLinkTreatWarningsAsErrors>
<IlcTreatWarningsAsErrors>true</IlcTreatWarningsAsErrors><EnableDefaultCompileItems>false</EnableDefaultCompileItems>
</PropertyGroup><ItemGroup><Compile Include="{xml_path(source)}/*.cs"/>
<ProjectReference Include="{xml_path(provider)}"/><Reference Include="RyuPy.Runtime"><HintPath>{xml_path(runtime)}</HintPath></Reference>
</ItemGroup></Project>''')
    return provider, runner


def run_unit(log, root, work, provider, runtime, env):
    """xUnit 是附加的 JIT 协议门；不把 xUnit 本身声明为 NativeAOT 测试。"""
    (work/'unit').mkdir()
    tests = work/'unit/Tests.csproj'
    tests.write_text(f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<TargetFramework>net10.0</TargetFramework><AssemblyName>RyuPy.CPython.Tests</AssemblyName>
<ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<EnableDefaultCompileItems>false</EnableDefaultCompileItems><IsPackable>false</IsPackable>
</PropertyGroup><ItemGroup><Compile Include="{xml_path(root/'runtime/RyuPy.CPython.Tests/StockThinFunctionContractTests.cs')}"/>
<ProjectReference Include="{xml_path(provider)}"/><Reference Include="RyuPy.Runtime"><HintPath>{xml_path(runtime)}</HintPath></Reference>
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1"/><PackageReference Include="xunit" Version="2.9.3"/>
<PackageReference Include="xunit.runner.visualstudio" Version="3.1.4"/><Using Include="Xunit"/>
</ItemGroup></Project>''')
    log.write('UNIT mode=jit\n')
    log.run(['dotnet', 'test', str(tests), '--disable-build-servers', '-c', 'Release', '-v', 'minimal'],
            work, dict(env, RYUPY_STOCK_THIN_MODE='jit'), timeout=120)


def validate(args, log):
    root = Path(__file__).resolve().parents[3]
    source = Path(__file__).resolve().parent
    existing_runtime = root/'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'
    if not existing_runtime.is_file():
        raise RuntimeError('需要已构建的主线 Runtime DLL，本驱动不会重建 Runtime')
    # 主代理维护 Program；缺少真实运行模式断言时不允许驱动仅凭 publish 名称宣称 AOT。
    program = (source/'Program.cs').read_text()
    main = program[program.index('public static int Main'):]
    guard = main.find('RuntimeFeature.IsDynamicCodeSupported')
    if guard < 0 or guard > main.index('CPythonHost.Start') or 'RYUPY_STOCK_THIN_MODE' not in main:
        raise RuntimeError('Runner.Main 必须在 Host.Start 前核验实际 JIT/NativeAOT 运行模式')
    mode = 'aot' if args.aot else 'jit'
    temporary_parent = Path(tempfile.gettempdir()).resolve()
    if temporary_parent == root or root in temporary_parent.parents:
        temporary_parent = root.parent
    with tempfile.TemporaryDirectory(prefix=f'ryupy-stock-thin-provider-{mode}-', dir=temporary_parent) as temporary:
        work = Path(temporary)
        log.write(f'WORK {work}\nMODE {mode}\n')
        cfg = json.loads(log.run([str(args.python.resolve()), '-I', '-B', '-c',
            'import json,sys,sysconfig; print(json.dumps(dict(home=sys.base_prefix,version=".".join(map(str,sys.version_info[:3])),lib=sysconfig.get_config_var("LIBDIR"),library=sysconfig.get_config_var("LDLIBRARY"),gil=sysconfig.get_config_var("Py_GIL_DISABLED"))))'], work))
        if cfg['version'] not in ('3.12.13', '3.13.3') or cfg['gil']:
            raise RuntimeError('只验固定常规 GIL stock 3.12.13/3.13.3')
        series = '.'.join(cfg['version'].split('.')[:2])
        native = work/'native'
        native.mkdir()
        log.run(['sh', str(root/'runtime/RyuPy.CPython/native/build.sh'), cfg['home'], str(native), series], work)
        tools = native/'libstock_thin_test_tools.dylib'
        log.run(['clang', '-dynamiclib', '-O2', '-Wall', '-Wextra', '-Werror',
                 '-I'+str(Path(cfg['home'])/'include'/f'python{series}'), str(source/'NativeTools.c'),
                 str(Path(cfg['lib'])/cfg['library']), '-o', str(tools)], work)
        inputs = work/'inputs'
        inputs.mkdir()
        runtime = inputs/'RyuPy.Runtime.dll'
        shutil.copy2(existing_runtime, runtime)
        provider, runner = write_projects(root, source, work, runtime)
        output = work/mode
        env = dict(os.environ, DOTNET_ROLL_FORWARD='Major', PYTHONDONTWRITEBYTECODE='1',
                   RYUPY_STOCK_THIN_MODE=mode)
        if args.aot:
            log.run(['dotnet', 'publish', str(runner), '--disable-build-servers', '-c', 'Release',
                     '-r', 'osx-arm64', '-p:PublishAot=true', '-o', str(output), '--nologo', '-v', 'minimal'],
                    work, env, timeout=600)
            executable = output/'RyuPy.CPython.Tests'
            if not executable.is_file():
                raise RuntimeError('NativeAOT publish 未生成原生 Runner')
            command = [str(executable)]
        else:
            log.run(['dotnet', 'build', str(runner), '--disable-build-servers', '-c', 'Release',
                     '-o', str(output), '--nologo', '-v', 'minimal'], work, env, timeout=120)
            command = ['dotnet', str(output/'RyuPy.CPython.Tests.dll')]
        # shim/tools 与本次执行输出并置；固定发行仍使用原绝对路径，本门没有搬迁声明。
        shutil.copy2(native/'libryupy_cpython.dylib', output/'libryupy_cpython.dylib')
        shutil.copy2(tools, output/tools.name)
        config = work/'host.json'
        config.write_text(json.dumps(dict(schema=1, python_version=cfg['version'], development=True,
            home=cfg['home'], library=str(Path(cfg['lib'])/cfg['library']),
            shim=str(output/'libryupy_cpython.dylib'),
            search_paths=[str(Path(cfg['home'])/'lib'/f'python{series}'),
                          str(Path(cfg['home'])/'lib'/f'python{series}'/'lib-dynload')],
            modules=['builtins', '__main__', 'gc', 'types', 'sys'], container_transfer='synchronized')))
        cases = ['resurrect'] if args.resurrection else ['ordinary', 'blocked', 'watcher-full']
        if not args.resurrection and not args.ordinary_only:
            cases.append('resurrect')
        for case in cases:
            log.write(f'HOST mode={mode} python={cfg["version"]} case={case}\n')
            log.run(command+[str(config)]+([] if case == 'ordinary' else [case]), work, env)
        if args.unit:
            run_unit(log, root, work, provider, runtime, env)
        summary = f'PASS suite mode={mode} python={cfg["version"]} host_processes={len(cases)} cases={",".join(cases)}\n'
        print(summary, end='', flush=True)
        log.write(summary)
    log.write('CLEANUP 仓外程序集/原生Runner/shim/tools/配置已清理\n')
    print('CLEANUP 仓外构建产物已清理', flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--python', type=Path, required=True)
    parser.add_argument('--aot', action='store_true', help='发布并直接运行真实 osx-arm64 NativeAOT Runner')
    parser.add_argument('--resurrection', action='store_true', help='单独运行正常 GC 析构复活门')
    parser.add_argument('--ordinary-only', action='store_true', help='仅诊断普通矩阵，排除复活门')
    parser.add_argument('--unit', action='store_true', help='附加运行 JIT 托管协议单测')
    args = parser.parse_args()
    if platform.system() != 'Darwin' or platform.machine() != 'arm64':
        parser.error('当前 Runner 驱动只验收 macOS arm64')
    if args.resurrection and args.ordinary_only:
        parser.error('--resurrection 与 --ordinary-only 不能同时使用')
    log = ValidationLog('aot' if args.aot else 'jit', args.python)
    print(f'LOG {log.path}', flush=True)
    try:
        validate(args, log)
    except BaseException as error:
        # TemporaryDirectory 已在异常展开中清理；日志独立保留，失败不覆盖旧运行。
        log.write(f'FAILED {type(error).__name__}: {error}\nCLEANUP 临时目录作用域已退出\n')
        raise
    finally:
        log.close()


if __name__ == '__main__':
    main()
