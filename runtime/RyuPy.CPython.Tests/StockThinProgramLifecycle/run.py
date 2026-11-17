#!/usr/bin/env python3
"""实际产品程序集/正式shim的仓外独立 JIT/NativeAOT 生命周期门；保留成功/失败三流，清理全部构建产物。"""
import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import shlex
import shutil
import signal
import subprocess
import tempfile


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--python', type=Path, required=True)
    parser.add_argument('--aot', action='store_true', help='发布并直接运行真实NativeAOT runner')
    parser.add_argument('--case', action='append', choices=['scoped', 'external', 'external-d', 'external-p', 'external-self', 'external-cell', 'external-container', 'zero', 'entry-fail', 'no-host', 'pre-entry-fail', 'scalar', 'external-callback', 'borrowed-receiver', 'callback-result', 'terminal-proxy', 'external-proxy', 'terminal-cycle', 'terminal-alias', 'frame-builtins'])
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[3]
    source = Path(__file__).resolve().parent
    logs = Path.home()/'.cache/ryupy/validation/adr0228'
    logs.mkdir(parents=True, exist_ok=True)
    path = logs/(datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ')+f'-{os.getpid()}.log')
    print('LOG', path, flush=True)
    with path.open('x') as log, tempfile.TemporaryDirectory(prefix='ryupy-0228-', dir='/tmp') as tmp:
        work = Path(tmp)

        def run(command, timeout=120, required=True, env=None, quiet=False):
            log.write('\nCOMMAND '+shlex.join(map(str, command))+'\n'); log.flush()
            process = subprocess.Popen(command, cwd=work, text=True, stdout=subprocess.PIPE,
                                       stderr=subprocess.PIPE, stdin=subprocess.DEVNULL, env=env, start_new_session=True)
            try:
                stdout, stderr = process.communicate(timeout=timeout)
            except BaseException:
                try:
                    os.killpg(process.pid, signal.SIGKILL)
                except ProcessLookupError:
                    pass
                stdout, stderr = process.communicate()
                log.write('INTERRUPTED\nSTDOUT\n'+stdout+'\nSTDERR\n'+stderr+'\n'); log.flush()
                raise
            result = subprocess.CompletedProcess(command, process.returncode, stdout, stderr)
            log.write(f'EXIT {result.returncode}\nSTDOUT\n{result.stdout}\nSTDERR\n{result.stderr}\n'); log.flush()
            if not quiet:
                print(result.stdout, end='', flush=True)
            if result.stderr:
                print(result.stderr, end='', flush=True)
            if required and result.returncode:
                raise RuntimeError('命令失败，见 '+str(path))
            return result

        cfg = json.loads(run([str(args.python.resolve()), '-I', '-B', '-c',
            'import sys,sysconfig,json; print(json.dumps(dict(home=sys.base_prefix,version=".".join(map(str,sys.version_info[:3])),lib=sysconfig.get_config_var("LIBDIR"),library=sysconfig.get_config_var("LDLIBRARY"),gil=sysconfig.get_config_var("Py_GIL_DISABLED"))))']).stdout)
        assert cfg['version'] in ('3.12.13', '3.13.3') and not cfg['gil']
        series = '.'.join(cfg['version'].split('.')[:2])
        # 只复制当前已构建的真实产品DLL，不注入源码、不改构造器或callback。
        inputs = work/'inputs'; inputs.mkdir()
        for assembly in ('RyuPy.Runtime', 'RyuPy.CPython'):
            built = root/f'runtime/{assembly}/bin/Release/net10.0/{assembly}.dll'
            assert built.is_file(), '先构建实际产品provider'
            shutil.copy2(built, inputs/built.name)
        (inputs/'RuntimeDependencies.props').write_bytes((root/'runtime/RyuPy.Runtime/RuntimeDependencies.props').read_bytes())
        shutil.copy2(root/'runtime/RyuPy.Runtime/ThirdPartyNotices.sqlite3.txt', inputs/'ThirdPartyNotices.sqlite3.txt')
        log.write('PRODUCT current built Runtime/provider + official native/build.sh\n')
        runner = work/'runner'; runner.mkdir()
        shutil.copy2(source/'Program.cs.in', runner/'Program.cs')
        shutil.copy2(source/'HostProbe.cs.in', runner/'Observer.cs')
        (runner/'Runner.csproj').write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><AssemblyName>RyuPy.CPython.Tests</AssemblyName>
<ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup><Import Project="../inputs/RuntimeDependencies.props"/><ItemGroup><Reference Include="RyuPy.CPython"><HintPath>../inputs/RyuPy.CPython.dll</HintPath></Reference>
<Reference Include="RyuPy.Runtime"><HintPath>../inputs/RyuPy.Runtime.dll</HintPath></Reference></ItemGroup></Project>''')
        output = work/'output'
        env = dict(os.environ, DOTNET_ROLL_FORWARD='Major')
        if args.aot:
            run(['dotnet', 'publish', str(runner/'Runner.csproj'), '--disable-build-servers', '-c', 'Release', '-r', 'osx-arm64', '-p:PublishAot=true', '-p:IlcTreatWarningsAsErrors=true', '-p:ILLinkTreatWarningsAsErrors=true', '-o', str(output), '-v', 'minimal'], timeout=300, env=env)
        else:
            run(['dotnet', 'build', str(runner/'Runner.csproj'), '--disable-build-servers', '-c', 'Release', '-o', str(output), '-v', 'minimal'], env=env)
        run(['sh', str(root/'runtime/RyuPy.CPython/native/build.sh'), cfg['home'], str(output), series])
        shim = output/'libryupy_cpython.dylib'
        run(['clang', '-dynamiclib', '-O2', '-Wall', '-Wextra', '-Werror',
             '-I'+str(Path(cfg['home'])/'include'/f'python{series}'),
             str(source/'NativeProbe.c'), str(shim), str(Path(cfg['lib'])/cfg['library']),
             '-o', str(output/'libstock_program_observer.dylib')])
        config = work/'host.json'
        config_data = dict(schema=1, python_version=cfg['version'], development=True,
            home=cfg['home'], library=str(Path(cfg['lib'])/cfg['library']), shim=str(shim),
            search_paths=[str(Path(cfg['home'])/'lib'/f'python{series}'), str(Path(cfg['home'])/'lib'/f'python{series}'/'lib-dynload')],
            modules=['builtins', 'gc', 'sys', 'atexit'], container_transfer='proxy')
        failed = False
        for case in args.case or ['scoped', 'external', 'external-d', 'external-p', 'external-self', 'external-cell', 'external-container', 'zero', 'entry-fail', 'no-host', 'pre-entry-fail', 'scalar', 'external-callback', 'borrowed-receiver', 'callback-result', 'terminal-proxy', 'external-proxy', 'terminal-cycle', 'terminal-alias', 'frame-builtins']:
            config_data['container_transfer'] = 'synchronized' if case in ('external-container', 'callback-result') else 'proxy'
            config.write_text(json.dumps(config_data))
            env['RYUPY_CPYTHON_REPORT'] = str(work/f'{case}-report.json')
            env['DOTNET_TieredCompilation'] = '0' if case == 'borrowed-receiver' else '1'
            command = [str(output/'RyuPy.CPython.Tests')] if args.aot else ['dotnet', str(output/'RyuPy.CPython.Tests.dll')]
            result = run(command + [str(config), case, 'aot' if args.aot else 'jit'], required=False, env=env)
            # 失败对照同样保留，SIGSEGV/异常退出永远不能作为预期泄漏通过。
            expected = 0
            env['RYUPY_CPYTHON_REPORT'] = str(work/f'{case}-report.json')
            if case in ('no-host', 'pre-entry-fail'):
                report_path = Path(env['RYUPY_CPYTHON_REPORT'])
                if report_path.exists():
                    assert 'stock_compiled_program' not in json.loads(report_path.read_text())
                assert result.returncode == 0 and not result.stderr and 'PASS '+case in result.stdout
                log.write('CASE '+case+' accepted=True\n'); continue
            if case == 'terminal-cycle':
                report = json.loads(Path(env['RYUPY_CPYTHON_REPORT']).read_text())
                terminal = report['stock_compiled_program']
                log.write('EXPECTED_CYCLE_REPORT '+json.dumps(report)+'\n')
                assert result.returncode == 0 and 'EXPECTED_INVISIBLE_CYCLE' in result.stdout
                assert 'stock程序终态owned/native引用未归零' in result.stderr
                assert terminal['terminal'] and terminal['ledger'] > 0 and report['managed_roots'] > 0
                assert terminal['post_finalize_decrefs'] == 0 and terminal['late_releases'] == 0
                print('PASS negative invisible cycle retained', flush=True)
                continue
            after = next((line for line in result.stdout.splitlines() if line.startswith('AFTER ')), '')
            fields = dict(item.split('=', 1) for item in after.split() if '=' in item)
            diagnostic = fields.get('finalize') == '0' and fields.get('postFinalizeDecref') == '0'
            report_path = Path(env['RYUPY_CPYTHON_REPORT'])
            if not report_path.exists():
                raise RuntimeError(f'进程没有生成终态报告: case={case} exit={result.returncode}，见 {path}')
            report = json.loads(report_path.read_text())
            terminal = report['stock_compiled_program']
            log.write('REPORT '+json.dumps(report)+'\n')
            diagnostic &= terminal['permission'] and terminal['terminal'] and all(terminal[key] == 0 for key in
                ('finalize_result','native_live','core_roots','owned_h','ledger','queue','late_releases','post_finalize_decrefs','entry_final_records','internal_proxy_roots','terminal_proxy_owner_records'))
            if case == 'terminal-proxy':
                assert 0 < terminal['terminal_proxy_peak_records'] < 32, '临时owned不能随1000次回调线性强保留'
            expected_stderr = 'ValueError: expected-diagnostic-report\n' if case == 'entry-fail' else ''
            passed = result.returncode == expected and result.stderr == expected_stderr and diagnostic
            line = f'CASE python={cfg["version"]} case={case} exit={result.returncode} expected={expected} accepted={passed}\n'
            print(line, end='', flush=True); log.write(line); log.flush()
            failed |= not passed
        log.write('CLEANUP 仓外构建目录由 TemporaryDirectory 清理\n')
        if failed:
            raise RuntimeError('生命周期门失败，见 '+str(path))


if __name__ == '__main__':
    main()
