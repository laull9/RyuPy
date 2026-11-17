#!/usr/bin/env python3
"""真实 Python→CIL 薄函数产品门：同次 oracle、保留模块函数正常退出、删除输入后搬迁。

只调用已构建 CLI；业务输入来自手写 pending 用例。命令、三流及原生生命周期报告均保存在
仓外日志，临时源码、venv、产品及构建目录无论成功失败都会清理。默认验 JIT，发布形态分别选择。
"""
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

from support import ROOT, audit_native_paths, environment, inspect_host_report, rid, standalone_layout
from onefile_support import audit_onefile_output


CASES = ('identity', 'global_rebinding', 'builtin_and_function', 'shutdown')


class Recorder:
    """每个命令保留完整证据；超时仅终止该命令自己的进程组。"""
    def __init__(self, stream):
        self.stream = stream

    def run(self, command, cwd, env, timeout=240):
        command = list(map(str, command))
        self.stream.write('\nCOMMAND '+shlex.join(command)+'\n')
        self.stream.flush()
        process = subprocess.Popen(command, cwd=cwd, env=env, start_new_session=True,
                                   stdin=subprocess.DEVNULL, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        try:
            stdout, stderr = process.communicate(timeout=timeout)
        except BaseException:
            try:
                os.killpg(process.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
            stdout, stderr = process.communicate()
            self.stream.write('INTERRUPTED\n'+repr((stdout, stderr))+'\n')
            self.stream.flush()
            raise
        self.stream.write(f'EXIT {process.returncode}\nSTDOUT\n{stdout.decode()}\nSTDERR\n{stderr.decode()}\n')
        self.stream.flush()
        return subprocess.CompletedProcess(command, process.returncode, stdout, stderr)


def require_success(result, label):
    if result.returncode:
        raise RuntimeError(f'{label} exit={result.returncode}: {result.stderr.decode()}')
    return result


def validate_case(args, log, version, mode, case, workspace):
    work = workspace/(mode+'-'+case)
    inputs, output = work/'private input', work/'build output'
    inputs.mkdir(parents=True)
    entry = inputs/'entry.py'
    shutil.copy2(ROOT/'tests/cases/stock_thin_compiler'/(case+'.py'), entry)
    build_env = environment()
    require_success(log.run(['uv', 'venv', '--python', args.python, inputs/'.venv'], work, build_env), 'venv')
    oracle = require_success(log.run([args.python, '-I', '-B', entry], work, build_env), '同次 CPython')
    if oracle.stderr:
        raise RuntimeError('oracle stderr 非空')
    command = [args.cli, '--cpython-provider', 'auto', '--experimental-stock-thin-functions',
               'build', entry, '--out-dir', output]
    if mode == 'standalone':
        command.append('--aot')
    elif mode == 'onefile':
        command.append('--onefile')
    built = require_success(log.run(command, work, build_env, 360), '真实 CLI 构建')
    # 只搬迁本次公开产品；中间程序集不能充当 NativeAOT 可执行入口。
    relocated = work/'已搬迁 产品'
    if mode == 'standalone':
        layout = standalone_layout(built.stderr, output, 'entry', rid())
        shutil.move(layout.dist, relocated)
        executable = relocated/layout.native.name
    elif mode == 'onefile':
        executable = audit_onefile_output(output, 'entry')
        relocated.mkdir()
        shutil.move(executable, relocated/executable.name)
        executable = relocated/executable.name
    else:
        shutil.move(output, relocated)
        executable = relocated/'entry.dll'
    shutil.rmtree(inputs)
    if output.exists():
        shutil.rmtree(output)
    if mode != 'onefile':
        audit_native_paths(relocated)
    report = work/'host report.json'
    run_env = environment()
    run_env.update(PATH='', PYTHONHOME='/missing-python-home', PYTHONPATH='/missing-python-path',
                   RYUPY_CPYTHON_REPORT=str(report))
    runtime_temp = work/'private runtime temp'
    runtime_temp.mkdir()
    run_env['TMPDIR'] = str(runtime_temp)
    execution = [shutil.which('dotnet'), executable] if mode == 'jit' else [executable]
    actual = log.run(execution, work, run_env)
    if (actual.returncode, actual.stdout, actual.stderr) != (oracle.returncode, oracle.stdout, oracle.stderr):
        raise RuntimeError(f'{version}/{mode}/{case} 三流或退出码不同，见完整日志')
    report_root = relocated
    if mode == 'onefile':
        raw = json.loads(report.read_text())
        report_root = Path(os.path.commonpath((raw['python_library'], raw['shim_library'])))
        if not report_root.is_relative_to(runtime_temp.resolve()):
            raise RuntimeError('onefile 未从本次私有解包目录加载原生闭包')
        if any(runtime_temp.iterdir()):
            raise RuntimeError('onefile 运行后解包目录未清理')
    observed = inspect_host_report(report, report_root)
    log.stream.write('HOST_REPORT\n'+json.dumps(observed, ensure_ascii=False, indent=2)+'\n')
    log.stream.flush()
    # 原生薄函数计数来自专用终态，而非仅凭 stdout 或 Dispose 的清账状态。
    thin = observed['stock_compiled_program']
    if not thin['permission'] or not thin['terminal']:
        raise RuntimeError('生成入口没有使用专用程序结束权限')
    for key in ('native_live', 'core_roots', 'owned_h', 'ledger', 'queue', 'late_releases',
                'post_finalize_decrefs', 'finalize_result'):
        if thin[key] != 0:
            raise RuntimeError(f'正常结束未归零: {key}={thin[key]}')
    if thin['native_calls'] <= 0 or thin['destroyed'] <= 0:
        raise RuntimeError('没有真实静态 core 调用或 owner 析构证据')
    if case == 'shutdown' and thin['native_calls'] != 1:
        raise RuntimeError('shutdown 输入只有 atexit 调用业务函数，真实调用计数必须为 1')
    print(f'PASS {version} {mode} {case}', flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--python', required=True, type=Path)
    parser.add_argument('--cli', type=Path, default=ROOT/'target/debug/ryupy')
    parser.add_argument('--mode', choices=('jit', 'standalone', 'onefile', 'all'), default='jit')
    parser.add_argument('--case', action='append', choices=CASES)
    args = parser.parse_args()
    args.python = args.python.resolve()
    args.cli = args.cli.resolve()
    version = subprocess.check_output([args.python, '-I', '-B', '-c',
        'import sys; print(".".join(map(str, sys.version_info[:3])))'], text=True).strip()
    if version not in ('3.12.13', '3.13.3'):
        raise RuntimeError('当前显式候选仅验固定 stock 3.12.13 / 3.13.3')
    logs = Path.home()/'.cache/ryupy/validation/adr0208-20261117'
    logs.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ')
    path = logs/f'stock-thin-compiler-{version}-{args.mode}-{stamp}.log'
    print('LOG', path, flush=True)
    with path.open('x', encoding='utf-8') as stream:
        with tempfile.TemporaryDirectory(prefix='ryupy-thin-compiler-', dir='/tmp') as temporary:
            workspace = Path(temporary)
            stream.write('WORK '+str(workspace)+'\n')
            modes = ('jit', 'standalone', 'onefile') if args.mode == 'all' else (args.mode,)
            try:
                for mode in modes:
                    for case in args.case or CASES:
                        validate_case(args, Recorder(stream), version, mode, case, workspace)
            finally:
                stream.write('CLEANING '+str(workspace)+'\n')
        stream.write('CLEANED\n')


if __name__ == '__main__':
    main()
