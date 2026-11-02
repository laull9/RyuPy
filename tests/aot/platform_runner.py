"""保留 POSIX Runner；Windows 使用独占 Job 回收整个发布树。"""
import json
import os
import subprocess
import time
from smoke import Runner as PosixRunner


class Runner(PosixRunner):
    def __init__(self, logs, *, deadline=None):
        super().__init__(logs)
        self.deadline = deadline

    def run(self, label, command, *, cwd, env=None, check=False, timeout=180):
        if self.deadline is not None:
            remaining = self.deadline - time.monotonic()
            if remaining <= 0:
                raise TimeoutError(f'{label}开始前已耗尽整组截止时间')
            timeout = min(timeout, remaining)
        if os.name != 'nt':
            return super().run(label, command, cwd=cwd, env=env, check=check, timeout=timeout)
        from windows_job import Job
        prefix = self.logs / label
        prefix.with_suffix('.command.json').write_text(
            json.dumps(command, ensure_ascii=False, indent=2), encoding='utf-8')
        job = Job()
        process = None
        try:
            process = job.start(command, cwd=cwd, env=env)
            try:
                stdout, stderr = process.communicate(timeout=timeout)
            except subprocess.TimeoutExpired:
                termination_error = None
                try:
                    job.terminate()
                except OSError as error:
                    termination_error = repr(error)
                finally:
                    job.close()  # Terminate失败也靠KILL_ON_JOB_CLOSE结束树，之后才读EOF。
                stdout, stderr = process.communicate()
                prefix.with_suffix('.stdout').write_bytes(stdout)
                prefix.with_suffix('.stderr').write_bytes(stderr)
                prefix.with_suffix('.status.json').write_text(json.dumps({
                    'timeout': True, 'exit': process.returncode, 'killed_job_root_pid': process.pid,
                    'termination_error': termination_error}))
                raise
            prefix.with_suffix('.stdout').write_bytes(stdout)
            prefix.with_suffix('.stderr').write_bytes(stderr)
            prefix.with_suffix('.status.json').write_text(json.dumps({'exit': process.returncode}))
            if check and process.returncode:
                raise RuntimeError(f'{label}失败，exit={process.returncode}；见{prefix}.stderr')
            return subprocess.CompletedProcess(command, process.returncode, stdout, stderr)
        finally:
            job.close()  # 成功也关闭本次Job，不能保留脱离根进程的子孙。
            if process is not None and process.poll() is None:
                process.communicate()  # 异常/取消也回收本次根进程。
