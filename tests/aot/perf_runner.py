"""Linux 独占进程组采样：限时、wait4资源统计、原字节证据，不改被测运行时。"""
import hashlib
import json
import os
from pathlib import Path
import signal
import subprocess
import time
import threading


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def write_json(path, value):
    Path(path).write_text(json.dumps(value, ensure_ascii=False, indent=2, allow_nan=False) + '\n')


class Runner:
    def __init__(self, logs, deadline):
        self.logs = Path(logs)
        self.logs.mkdir(parents=True, exist_ok=False)
        self.deadline = deadline

    @staticmethod
    def signal_group(pid, sig):
        try:
            os.killpg(pid, sig)
        except ProcessLookupError:
            pass

    def run(self, label, command, *, cwd, env, limit, stats=False):
        if time.monotonic() >= self.deadline:
            raise TimeoutError('整组1800秒预算已耗尽')
        case = self.logs / label
        case.mkdir(exist_ok=False)
        command = [str(part) for part in command]
        actual_env = dict(env)
        if stats:
            actual_env['RYUPY_STATS'] = str((case / 'stats.json').resolve())
        write_json(case / 'command.json', command)
        write_json(case / 'environment.json', {k: v for k, v in actual_env.items()
                   if k.startswith(('DOTNET_', 'COMPlus_', 'RYUPY_', 'PYTHONHASHSEED'))})
        start = time.monotonic()
        end = min(self.deadline, start + limit)
        status = usage = None
        timeout_event = threading.Event()
        done = threading.Event()
        watchdog = None
        stopped_at = None
        interrupted = False
        process = None
        with (case / 'stdout').open('wb') as out, (case / 'stderr').open('wb') as err:
            try:
                process = subprocess.Popen(command, cwd=cwd, env=actual_env,
                                           stdout=out, stderr=err, start_new_session=True)
                def enforce_deadline():
                    # 此线程只发送信号，不wait子进程；正常计时不再按5ms轮询量化。
                    if done.wait(max(0, end - time.monotonic())):
                        return
                    timeout_event.set()
                    self.signal_group(process.pid, signal.SIGTERM)
                    done.wait(max(0, min(5, self.deadline - time.monotonic())))
                    self.signal_group(process.pid, signal.SIGKILL)

                watchdog = threading.Thread(target=enforce_deadline, daemon=True)
                watchdog.start()
                _, status, usage = os.wait4(process.pid, 0)
                stopped_at = time.monotonic()
                process.returncode = os.waitstatus_to_exitcode(status)
            except BaseException:
                interrupted = True
                if process is not None and process.returncode is None:
                    self.signal_group(process.pid, signal.SIGKILL)
                    _, status, usage = os.wait4(process.pid, 0)
                    process.returncode = os.waitstatus_to_exitcode(status)
                raise
            finally:
                done.set()
                if watchdog is not None:
                    watchdog.join()
                elapsed = (stopped_at if stopped_at is not None else time.monotonic()) - start
                # 正常退出也不允许孙进程遗留在本组继续污染后续计时。
                leaked = False
                if process is not None:
                    try:
                        os.killpg(process.pid, 0)
                        leaked = True
                        self.signal_group(process.pid, signal.SIGKILL)
                    except ProcessLookupError:
                        pass
                record = {'command': command, 'wall_ms': elapsed * 1000,
                          'exit': None if status is None else os.waitstatus_to_exitcode(status),
                          'timeout': timeout_event.is_set(), 'interrupted': interrupted, 'leaked_group': leaked,
                          'maxrss_bytes': None if usage is None else usage.ru_maxrss * 1024,
                          'user_seconds': None if usage is None else usage.ru_utime,
                          'system_seconds': None if usage is None else usage.ru_stime}
                write_json(case / 'status.json', record)
        # 原始stats保存在持久目录，解析失败也不会删掉；从不把坏JSON改成空字典。
        if (case / 'stats.json').exists():
            record['stats_sha256'] = digest(case / 'stats.json')
        record['case'] = str(case)
        record['stdout'] = (case / 'stdout').read_bytes()
        record['stderr'] = (case / 'stderr').read_bytes()
        if timeout_event.is_set() or leaked or record['exit'] != 0:
            raise RuntimeError(f'{label}: 失败，见 {case}/status.json')
        return record
