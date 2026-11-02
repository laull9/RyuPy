#!/usr/bin/env python3
"""摘要重建候选的有界管道/PTY诊断；不是用户原1.py，不修改终端安全设置。"""
import argparse
import errno
import hashlib
import json
import os
from pathlib import Path
import pty
import platform
import select
import shutil
import signal
import subprocess
import tempfile
import termios
import time


SEQUENCES = {'expected': ['x', 'd', 'x', 'x', 'x', 'y'],
             'reported': ['x', 'd', 'd', 'x', 'x', 'x', 'y']}
FIXTURES = ('candidate', 'instrumented', 'input_branch')
BASELINE = '457886017cd4d32911a09b09307d07c327b87192'


def write_json(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def tool(value):
    found = shutil.which(value)
    if not found:
        raise FileNotFoundError(value)
    return str(Path(found).resolve())


def stop(process):
    if process is not None:
        try:
            os.killpg(process.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass
        process.wait(timeout=2)


class Probe:
    def __init__(self, work, env):
        self.work = work
        self.env = env
        self.deadline = time.monotonic() + 120

    def limit(self):
        value = min(15, self.deadline - time.monotonic())
        if value <= 0:
            raise TimeoutError('总120秒诊断预算已耗尽')
        return value

    def pipe(self, label, command, data=b'', env=None):
        folder = self.work / label
        folder.mkdir()
        write_json(folder / 'command.json', command)
        (folder / 'stdin').write_bytes(data)
        process = None
        error = None
        stdout = stderr = b''
        try:
            limit = self.limit()
            process = subprocess.Popen(command, cwd=self.work, env=env or self.env, stdin=subprocess.PIPE,
                                       stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                       start_new_session=True)
            try:
                stdout, stderr = process.communicate(data, timeout=limit)
            except subprocess.TimeoutExpired as exc:
                error = repr(exc)
                stop(process)
                stdout, stderr = process.communicate()
        except BaseException as exc:
            error = repr(exc)
            stop(process)
        finally:
            (folder / 'stdout').write_bytes(stdout)
            (folder / 'stderr').write_bytes(stderr)
            record = {'exit': None if process is None else process.returncode, 'error': error}
            write_json(folder / 'status.json', record)
        if error:
            raise RuntimeError(f'{label}: {error}')
        return stdout, stderr, record['exit']

    def terminal(self, label, command, answers, prompt, delay, ending):
        folder = self.work / label
        folder.mkdir()
        write_json(folder / 'command.json', command)
        transcript, pending, sent = bytearray(), bytearray(), []
        process = None
        master = slave = None
        error = None
        try:
            end = time.monotonic() + self.limit()
            # 使用默认终端设置；不调整echo/canonical/ICRNL，不申请额外权限。
            master, slave = pty.openpty()
            attributes = termios.tcgetattr(slave)
            write_json(folder / 'termios-initial.json', {
                'iflag': attributes[0], 'oflag': attributes[1], 'cflag': attributes[2],
                'lflag': attributes[3], 'ispeed': attributes[4], 'ospeed': attributes[5],
                'cc': [({'hex': item.hex()} if isinstance(item, bytes) else item) for item in attributes[6]],
                'modified_by_harness': False})
            process = subprocess.Popen(command, cwd=self.work, env=self.env, stdin=slave, stdout=slave,
                                       stderr=slave, start_new_session=True)
            os.close(slave)
            slave = None
            while True:
                if time.monotonic() >= end:
                    raise TimeoutError('PTY单进程15秒或全组预算已耗尽')
                ready, _, _ = select.select([master], [], [], min(0.05, max(0, end-time.monotonic())))
                if ready:
                    try:
                        data = os.read(master, 65536)
                    except OSError as exc:
                        if exc.errno == errno.EIO:
                            break
                        raise
                    if not data:
                        break
                    transcript.extend(data)
                    pending.extend(data)
                    while prompt in pending:
                        _, _, tail = pending.partition(prompt)
                        pending = bytearray(tail)
                        if len(sent) >= len(answers):
                            raise RuntimeError('程序请求超过固定序列的输入')
                        if delay:
                            time.sleep(min(delay, max(0, end-time.monotonic())))
                        if time.monotonic() >= end:
                            raise TimeoutError('投喂前预算耗尽')
                        value = answers[len(sent)].encode() + ending
                        os.write(master, value)
                        sent.append(value)
            process.wait(timeout=max(0.01, min(2, end-time.monotonic())))
            if process.returncode != 0:
                raise RuntimeError(f'应用必须正常退出0，实际 {process.returncode}')
            if len(sent) != len(answers):
                raise RuntimeError('程序提前退出，未消费完整固定序列')
        except BaseException as exc:
            error = repr(exc)
            stop(process)
        finally:
            for fd in (master, slave):
                if fd is not None:
                    os.close(fd)
            (folder / 'transcript').write_bytes(transcript)
            (folder / 'stdin').write_bytes(b''.join(sent))
            record = {'exit': None if process is None else process.returncode, 'error': error,
                      'sent_count': len(sent), 'delay_seconds': delay, 'ending_hex': ending.hex(),
                      'stderr': 'merged into PTY transcript; not a separately observed stream'}
            write_json(folder / 'status.json', record)
        if error:
            raise RuntimeError(f'{label}: {error}')
        return bytes(transcript), record['exit']


def behavior(raw, prompt):
    """仅固定候选：去提示及终端单字符回显；原转录另存，绝不称raw逐字通过。"""
    lines = raw.decode('utf-8', errors='strict').replace('\r\n', '\n').splitlines()
    output, echoes = [], []
    for line in lines:
        if line.startswith(prompt):
            line = line[len(prompt):]
        if line in ('x', 'd', 'y'):
            echoes.append(line)
        elif line:
            output.append(line)
    return {'program_lines': output, 'echoes': echoes}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--cli', required=True)
    parser.add_argument('--python', required=True)
    parser.add_argument('--runtime-dir', type=Path, required=True)
    parser.add_argument('--out', type=Path, required=True)
    parser.add_argument('--expected-ref', default=BASELINE,
                        help='待验独立checkout的完整40位Git SHA（默认原main基线）')
    args = parser.parse_args()
    if len(args.expected_ref) != 40 or any(c not in '0123456789abcdefABCDEF' for c in args.expected_ref):
        parser.error('--expected-ref必须是完整40位十六进制Git SHA')
    args.expected_ref = args.expected_ref.lower()
    cli, python = tool(args.cli), tool(args.python)
    runtime = args.runtime_dir.resolve() / 'RyuPy.Runtime.dll'
    args.out.mkdir(parents=True, exist_ok=True)
    work = Path(tempfile.mkdtemp(prefix='stdio-probe-', dir=args.out.resolve()))
    probe = Probe(work, {**os.environ, 'RYUPY_RUNTIME_DIR': str(runtime.parent)})
    records = []
    summary = {'scope': 'summary-derived candidates, NOT original user 1.py',
               'diagnostic_harness_sha256': sha(Path(__file__)),
               'expected_workflow_baseline': args.expected_ref, 'cli_sha256': sha(Path(cli)),
               'host': {'os': platform.system(), 'arch': platform.machine(),
                        'platform': platform.platform(), 'uname': platform.uname()._asdict()},
               'runtime_sha256': sha(runtime), 'records': records, 'status': 'failed'}
    print(f'原始交互证据：{work}', flush=True)
    try:
        version = probe.pipe('python-version', [python, '-c',
            "import sys; print(sys.implementation.name); print('.'.join(map(str,sys.version_info[:3])))"])
        if version != (b'cpython\n3.12.13\n', b'', 0):
            raise ValueError('需要固定CPython3.12.13')
        info = probe.pipe('dotnet-info', [tool(os.environ.get('DOTNET', 'dotnet')), '--info'])
        if info[2]:
            raise RuntimeError('无法记录实际.NET信息')
        main_ref = probe.pipe('main-ref', ['git', '-C', str(Path(cli).parent), 'rev-parse', 'HEAD'])
        summary['actual_checkout_ref'] = main_ref[0].decode().strip()
        if main_ref[2] or summary['actual_checkout_ref'] != args.expected_ref:
            raise ValueError('诊断checkout与--expected-ref指定的冻结SHA不一致')
        sources = work / 'source'
        sources.mkdir()
        for name in FIXTURES:
            source = Path(__file__).with_name('fixtures') / (name + '.py')
            shutil.copy2(source, sources / source.name)
            source = sources / source.name
            # 用户入口：每个Ryu进程都真实CLI run，非预编DLL替代。
            commands = ([python, str(source)], [cli, 'run', str(source)])
            prompt = 'answer: ' if name == 'input_branch' else 'answer y/d/x: '
            for sequence, answers in SEQUENCES.items():
                prefix = name + '-' + sequence
                data = ('\n'.join(answers) + '\n').encode()
                expected = probe.pipe(prefix + '-pipe-python', commands[0], data)
                actual = probe.pipe(prefix + '-pipe-ryupy', commands[1], data)
                if expected[2] != 0 or actual[2] != 0:
                    raise RuntimeError(f'{prefix}: CP/Ryu应用必须各自exit0')
                records.append({'case': prefix, 'transport': 'pipe', 'source_sha256': sha(source),
                                'raw_equal': expected == actual, 'behavior_equal': expected == actual})
                for timing, delay in [('immediate', 0), ('delayed', 0.1)]:
                    for newline, ending in [('LF', b'\n'), ('CR', b'\r')]:
                        label = prefix + '-' + timing + '-' + newline
                        expected = probe.terminal(label + '-python', commands[0], answers, prompt.encode(), delay, ending)
                        actual = probe.terminal(label + '-ryupy', commands[1], answers, prompt.encode(), delay, ending)
                        cp, ry = behavior(expected[0], prompt), behavior(actual[0], prompt)
                        records.append({'case': label, 'transport': 'pty', 'source_sha256': sha(source),
                                        'raw_equal': expected == actual,
                                        'behavior_equal': cp['program_lines'] == ry['program_lines'] and expected[1] == actual[1],
                                        'python_observation': cp, 'ryupy_observation': ry})
        if len(records) != 30:
            raise AssertionError('固定30对子/60应用进程矩阵未完成')
        summary['application_pairs'] = len(records)
        summary['application_processes'] = len(records) * 2
        summary['status'] = 'diagnostic-complete'
        summary['raw_differences'] = sum(not row['raw_equal'] for row in records)
        summary['behavior_differences'] = sum(not row['behavior_equal'] for row in records)
        # 显示差异只是记录；真正行为差异让CI明确失败，但不丢原转录。
        if summary['behavior_differences']:
            summary['status'] = 'behavior-difference'
            raise AssertionError('候选控制流/输出差异，需核原用户文件，不自动修复')
    except BaseException as error:
        summary['failure'] = repr(error)
        raise
    finally:
        write_json(work / 'summary.json', summary)


if __name__ == '__main__':
    main()
