#!/usr/bin/env python3
"""真实CLI长入口owned诊断/旧版对照：同次CP输出、JIT/NativeAOT搬迁、独立wait4 RSS。

诊断进程单独开启报告；正式计时清空报告/STATS且交替CP/旧/新，先预热再采样。
只按Release真实退账验证终态；完整命令/三流存仓外，全部产品临时目录自动清理。
"""
import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import shutil
import signal
import subprocess
import sys
import tempfile
import threading
import time

from support import ROOT, audit_native_paths, environment, rid, standalone_layout
from stock_thin_compiler import Recorder, require_success


def measured(command, work, env, timeout=180):
    """磁盘三流避免管道阻塞；wait4只采当前新进程，watchdog只终止本进程组。"""
    with tempfile.TemporaryFile() as stdout, tempfile.TemporaryFile() as stderr:
        start = time.perf_counter()
        process = subprocess.Popen(list(map(str, command)), cwd=work, env=env, stdout=stdout,
                                   stderr=stderr, stdin=subprocess.DEVNULL, start_new_session=True)
        def stop():
            try:
                os.killpg(process.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
        watchdog = threading.Timer(timeout, stop)
        watchdog.start()
        try:
            _, status, usage = os.wait4(process.pid, 0)
            process.returncode = os.waitstatus_to_exitcode(status)
        finally:
            watchdog.cancel()
        elapsed = time.perf_counter() - start
        stdout.seek(0); stderr.seek(0)
        return dict(command=list(map(str, command)), exit=process.returncode,
                    stdout=stdout.read().decode(), stderr=stderr.read().decode(), seconds=elapsed,
                    rss_bytes=usage.ru_maxrss * (1 if sys.platform == 'darwin' else 1024))


def check_output(actual, expected):
    if any(actual[key] != expected[key] for key in ('exit', 'stdout', 'stderr')):
        raise RuntimeError('真实CIL与同次CPython三流不同')


def build(args, recorder, work, env, label, runtime, control):
    """冻结同组Runtime、仅替换provider；真实CLI构建后删除输入和中间产物再搬迁。"""
    source = work/(label+' input'); source.mkdir()
    entry = source/'entry.py'; shutil.copy2(control, entry)
    require_success(recorder.run(['uv', 'venv', '--python', args.python, source/'.venv'], work, env), 'venv')
    output = work/(label+' output')
    command = [args.cli, '--cpython-provider', 'auto', '--experimental-stock-thin-functions',
               'build', entry, '--out-dir', output]
    if args.mode == 'standalone':
        command.append('--aot')
    result = require_success(recorder.run(command, work, dict(env, RYUPY_RUNTIME_DIR=str(runtime)), 360), 'CLI')
    relocated = work/(label+' 已搬迁 产品')
    if args.mode == 'standalone':
        layout = standalone_layout(result.stderr, output, 'entry', rid())
        shutil.move(layout.dist, relocated)
        command = [relocated/layout.native.name]
        shutil.rmtree(output)
    else:
        shutil.move(output, relocated)
        command = [shutil.which('dotnet'), relocated/'entry.dll']
    shutil.rmtree(source)
    audit_native_paths(relocated)
    return command


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--python', type=Path, required=True)
    parser.add_argument('--cli', type=Path, default=ROOT/'target/debug/ryupy')
    parser.add_argument('--baseline-provider', type=Path, help='冻结HEAD旧版provider DLL')
    parser.add_argument('--mode', choices=('jit', 'standalone'), default='jit')
    parser.add_argument('--count', action='append', type=int)
    parser.add_argument('--benchmark', action='store_true', help='诊断通过后独占窗口正式计时')
    parser.add_argument('--repeats', type=int, default=3)
    args = parser.parse_args()
    args.python = args.python.resolve(); args.cli = args.cli.resolve()
    counts = args.count or [10000, 100000, 1000000]
    if any(n <= 0 for n in counts) or args.repeats <= 0:
        raise ValueError('循环/采样次数必须为正数')
    logs = Path.home()/'.cache/ryupy/validation/adr0208-20261117'
    logs.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ')+f'-{os.getpid()}'
    path = logs/f'stock-program-owned-{args.mode}-{stamp}.log'
    print('LOG', path, flush=True)
    with path.open('x') as stream, tempfile.TemporaryDirectory(prefix='ryupy-owned-loop-', dir='/tmp') as temporary:
        work = Path(temporary); env = environment(); recorder = Recorder(stream)
        stream.write('MODE '+args.mode+'\n')
        for command in (["git", "rev-parse", "HEAD"], ["git", "branch", "--show-current"],
                        ["git", "status", "--short"], ["uname", "-a"], ["dotnet", "--version"],
                        [args.python, "-I", "-B", "-c", "import sys; print(sys.version); print(sys.executable)"]):
            require_success(recorder.run(command, ROOT, env), '环境冻结')
        # 固定本次CLI字节，四组产品使用同一编译器；不依赖共享target后续变动。
        frozen_cli = work/'frozen ryupy'
        shutil.copy2(args.cli, frozen_cli); args.cli = frozen_cli
        control = work/'control.py'
        shutil.copy2(ROOT/'tests/cases/stock_thin_compiler/ownership_loop.py', control)
        expected = {}
        for count in counts:
            oracle = measured([args.python, '-I', '-B', control, count], work, env)
            stream.write('ORACLE '+json.dumps(oracle, ensure_ascii=False)+'\n'); stream.flush()
            if oracle['exit'] or oracle['stderr']:
                raise RuntimeError('CPython oracle失败')
            expected[count] = oracle
        runtimes = work/'runtimes'; runtimes.mkdir()
        current = runtimes/'new'
        shutil.copytree(ROOT/'runtime/RyuPy.Runtime/bin/Release/net10.0', current)
        shutil.copy2(ROOT/'runtime/RyuPy.CPython/bin/Release/net10.0/RyuPy.CPython.dll', current)
        products = {}
        if args.baseline_provider:
            old = runtimes/'old'; shutil.copytree(current, old)
            shutil.copy2(args.baseline_provider.resolve(), old/'RyuPy.CPython.dll')
            products['old'] = build(args, recorder, work, env, 'old', old, control)
        products['new'] = build(args, recorder, work, env, 'new', current, control)
        # 控制源码单独保留供CPython；各产品原始输入/venv和AOT build目录均已删除。
        run_env = dict(env, PATH='', PYTHONHOME='/missing-python-home', PYTHONPATH='/missing-python-path')
        for count in counts:
            for label, command in products.items():
                report = work/f'{label}-{count}.json'
                actual = measured(command+[count], work, dict(run_env, RYUPY_CPYTHON_REPORT=str(report)))
                actual['label'] = label; actual['count'] = count
                stream.write('DIAGNOSTIC '+json.dumps(actual, ensure_ascii=False)+'\n'); stream.flush()
                check_output(actual, expected[count])
                host = json.loads(report.read_text())
                stream.write('HOST_REPORT '+json.dumps(host, ensure_ascii=False)+'\n'); stream.flush()
                stats = host['stock_compiled_program']
                for name in ('native_live', 'core_roots', 'owned_h', 'ledger', 'queue', 'late_releases', 'post_finalize_decrefs', 'finalize_result'):
                    if stats[name]:
                        raise RuntimeError('终态未归零: '+name)
                if stats.get('entry_final_records', 0) or stats['native_calls'] != count or stats['destroyed'] != 1:
                    raise RuntimeError('真实业务调用/退账计数错误')
                print('PASS', args.mode, label, count, 'rss', actual['rss_bytes'],
                      'peak_records', stats.get('entry_peak_records', 'old-strong-scope'), flush=True)
        if args.benchmark:
            commands = {'cp': [args.python, '-I', '-B', control], **products}
            for count in counts:
                for repetition in range(args.repeats+1):
                    order = list(commands)
                    # 三个正式轮次轮换起点，各实现各占一次首/中/末位，旧新先后交替。
                    shift = (repetition - 1) % len(order) if repetition else 0
                    order = order[-shift:] + order[:-shift]
                    for label in order:
                        sample = measured(commands[label]+[count], work, run_env)
                        sample.update(label=label, count=count, repetition=repetition)
                        stream.write(('WARMUP ' if repetition == 0 else 'BENCHMARK ')+json.dumps(sample, ensure_ascii=False)+'\n'); stream.flush()
                        check_output(sample, expected[count])
                        print('SAMPLE', label, count, repetition, sample['seconds'], sample['rss_bytes'], flush=True)
        stream.write('CLEANED on context exit\n')


if __name__ == '__main__':
    main()
