"""轻测试：不需SDK；真实子孙超时及参数传递。"""
import json
import os
import platform
import shutil
import time
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest import mock
from platform_runner import Runner
from test_runner import RunnerTimeoutTests  # 保留既有Linux组杀回归，不重复复制。


class DeadlineTests(unittest.TestCase):
    def test_remaining_budget_shrinks_and_exhaustion_never_starts(self):
        # 不需SDK/子进程；父Runner只记录调用，证明预算不是每步骤重置。
        with mock.patch('platform_runner.os.name', 'posix'), \
             mock.patch('platform_runner.time.monotonic', side_effect=[10, 15, 20]), \
             mock.patch('platform_runner.PosixRunner.run') as launch:
            runner = Runner(None, deadline=20)
            runner.run('first', ['unused'], cwd='.', timeout=1800)
            runner.run('second', ['unused'], cwd='.', timeout=1800)
            with self.assertRaises(TimeoutError):
                runner.run('exhausted', ['unused'], cwd='.', timeout=1800)
            self.assertEqual(launch.call_count, 2)
            self.assertEqual([call.kwargs['timeout'] for call in launch.call_args_list], [10, 5])


class PublishedPathTests(unittest.TestCase):
    def test_samefile_owned_root_and_sibling_rejection(self):
        from platform_smoke import published_platform_native
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            output = root/'owned output'
            other = root/'other output'
            for parent in (output, other):
                publish = parent/'ryupy-aot-test'/'publish'
                publish.mkdir(parents=True)
                native = publish/'host.exe'
                native.write_bytes(b'path-validator-only')
                report = ('已生成 ' + str(native.resolve()) + '\n').encode('utf-8')
                if parent == output:
                    self.assertEqual(published_platform_native(report, output, 'win-x64'), native.resolve())
                else:
                    with self.assertRaises(AssertionError):
                        published_platform_native(report, output, 'win-x64')
        # 仅路径归属测试；不声称假文件是PE或已验Windows扩展前缀。


@unittest.skipUnless(platform.system() == 'Darwin', '真实macOS进程组验收')
class MacProcessGroupTests(unittest.TestCase):
    def test_timeout_terminates_child_and_preserves_partial_output(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            script = "sleep 60 & child=$!; echo $child; echo partial-error >&2; wait $child"
            with self.assertRaises(subprocess.TimeoutExpired):
                Runner(root).run('timeout', ['/bin/sh', '-c', script], cwd=root, timeout=1)
            child = int((root/'timeout.stdout').read_text().strip())
            self.assertEqual((root/'timeout.stderr').read_bytes(), b'partial-error\n')
            state = json.loads((root/'timeout.status.json').read_text())
            self.assertTrue(state['timeout'])
            self.assertNotEqual(state['killed_process_group'], os.getpgrp())
            self.assertLess(state['exit'], 0)
            ps = shutil.which('ps')
            self.assertIsNotNone(ps)
            deadline = time.monotonic() + 3
            while True:
                observed = subprocess.run([ps, '-o', 'state=', '-p', str(child)],
                    stdout=subprocess.PIPE, stderr=subprocess.PIPE, timeout=2)
                if observed.returncode == 1 and not observed.stdout.strip():
                    break
                self.assertEqual(observed.returncode, 0, observed.stderr)
                if observed.stdout.strip().startswith(b'Z'):
                    break  # init尚未回收，但子进程已经终止。
                if time.monotonic() >= deadline:
                    self.fail(f'超时后子进程仍活：{child}: {observed.stdout!r}')
                time.sleep(.02)


@unittest.skipUnless(os.name == 'nt', '真实 Windows Job 验收')
class WindowsJobTests(unittest.TestCase):
    def test_timeout_terminates_child_and_preserves_partial_output(self):
        from windows_job import api
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            child = 'import time; time.sleep(60)'
            script = ('import subprocess,sys,time; '
                      f'p=subprocess.Popen([sys.executable,"-c",{child!r}]); '
                      'print(p.pid,flush=True); print("partial-error",file=sys.stderr,flush=True); time.sleep(60)')
            with self.assertRaises(subprocess.TimeoutExpired):
                Runner(root).run('timeout', [sys.executable, '-c', script], cwd=root, timeout=2)
            pid = int((root/'timeout.stdout').read_text().strip())
            self.assertEqual((root/'timeout.stderr').read_bytes(), b'partial-error\r\n')
            state = json.loads((root/'timeout.status.json').read_text())
            self.assertTrue(state['timeout'])
            self.assertNotEqual(state['killed_job_root_pid'], os.getpid())
            kernel = api()
            handle = kernel.OpenProcess(0x100000, False, pid)  # SYNCHRONIZE
            if handle:
                try:
                    self.assertEqual(kernel.WaitForSingleObject(handle, 3000), 0)
                finally:
                    kernel.CloseHandle(handle)
            else:
                import ctypes
                self.assertEqual(ctypes.get_last_error(), 87)  # 已退出且PID不再存在。

    def test_assignment_failure_does_not_execute_child(self):
        from windows_job import Job
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            marker = root/'must-not-exist'
            job = Job()
            # 模拟Job拒绝；初始线程应仍挂起，绝不能写出marker。
            job.kernel.AssignProcessToJobObject = lambda *_: False
            with self.assertRaises(OSError):
                job.start([sys.executable, '-c',
                    f"from pathlib import Path; Path({str(marker)!r}).write_text('ran')"], cwd=root, env=None)
            self.assertFalse(marker.exists())
            self.assertIsNone(job.handle)

    def test_real_success_unicode_and_empty_arguments(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            result = Runner(root).run('args', [sys.executable, '-c',
                'import sys,json; print(json.dumps(sys.argv[1:]))', '', 'a & b', '中文'], cwd=root, check=True)
            self.assertEqual(json.loads(result.stdout), ['', 'a & b', '中文'])


if __name__ == '__main__':
    unittest.main()
