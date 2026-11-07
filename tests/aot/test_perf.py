"""性能工具轻自测：统计负例与Linux超时清理，不构建或运行RyuPy基准。"""
import errno
import json
import os
from pathlib import Path
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

from perf_runner import Runner
from perf_schema import FIELDS, output, stats


def descendant_running(proc):
    """单次读取避免 exists/read 竞态；只把内核报告的消失或僵尸算作退出。"""
    try:
        text = proc.read_text()
    except OSError as error:
        if error.errno in (errno.ENOENT, errno.ESRCH):
            return False
        raise
    state = text.rsplit(') ', 1)[1].split()[0]
    return state != 'Z'


def wait_descendant_stopped(proc):
    for _ in range(100):
        if not descendant_running(proc):
            return
        time.sleep(0.01)
    raise AssertionError('本组孙进程仍在运行')


class ProcStateTests(unittest.TestCase):
    def test_disappearance_and_zombie(self):
        proc = Path('/proc/not-a-real-test-pid/stat')
        for code in (errno.ENOENT, errno.ESRCH):
            with patch.object(Path, 'read_text', side_effect=OSError(code, 'gone')):
                self.assertFalse(descendant_running(proc))
        with patch.object(Path, 'read_text', return_value='123 (name) Z 1'):
            self.assertFalse(descendant_running(proc))

    def test_alive_exhausts_original_bound(self):
        for state in ('R', 'S'):
            with patch.object(Path, 'read_text', return_value=f'123 (name) {state} 1') as read:
                with patch('time.sleep') as sleep:
                    with self.assertRaisesRegex(AssertionError, '仍在运行'):
                        wait_descendant_stopped(Path('/unused'))
                    self.assertEqual(read.call_count, 100)
                    self.assertEqual(sleep.call_count, 100)
                    sleep.assert_called_with(0.01)

    def test_other_errors_and_malformed_stat_propagate(self):
        for code in (errno.EACCES, errno.EPERM, errno.EIO):
            with patch.object(Path, 'read_text', side_effect=OSError(code, 'error')):
                with self.assertRaises(OSError) as caught:
                    descendant_running(Path('/unused'))
                self.assertEqual(caught.exception.errno, code)
        with patch.object(Path, 'read_text', return_value='malformed'):
            with self.assertRaises(IndexError):
                descendant_running(Path('/unused'))


class SchemaTests(unittest.TestCase):
    def test_all_samples_and_checksums(self):
        text = ''.join(f'warm {i} ok\n' for i in range(3))
        text += ''.join(f'sample {i} 0.01 ok\n' for i in range(7))
        self.assertEqual(output(text.encode(), 'ok', hot=True), [0.01] * 7)
        for bad in (text.replace('warm 1 ok', 'warm 1 bad'),
                    text.replace('sample 2 0.01 ok', 'sample 2 0.01 bad'),
                    text.replace('sample 3 0.01', 'sample 2 0.01'),
                    text.replace('sample 0 0.01', 'sample 0 nan'),
                    text + 'extra\n', text.split('sample 6')[0]):
            with self.assertRaises(ValueError):
                output(bad.encode(), 'ok', hot=True)
        self.assertEqual(output(b'result (1, 2)\n', '(1, 2)'), [])
        self.assertEqual(output(b'hello\n', 'hello', hello=True), [])

    def test_stats_exact_schema_and_raw_retained(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / 'stats.json'
            good = {key: 0 for key in FIELDS}
            path.write_text(json.dumps(good))
            self.assertEqual(stats(path), good)
            failures = [b'', b'{', b'{"main_ms":0,"main_ms":0}']
            for key, bad in (('alloc_bytes', 0.5), ('main_ms', True), ('gc0', -1),
                             ('jit_ms', float('nan')), ('heap_bytes', '0')):
                value = {**good, key: bad}
                failures.append(json.dumps(value).encode())
            failures.extend((json.dumps({k: v for k, v in good.items() if k != 'gc0'}).encode(),
                             json.dumps({**good, 'extra': 1}).encode()))
            for raw in failures:
                path.write_bytes(raw)
                with self.assertRaises((ValueError, UnicodeError)):
                    stats(path)
                self.assertEqual(path.read_bytes(), raw)
            path.unlink()
            with self.assertRaises(FileNotFoundError):
                stats(path)


@unittest.skipUnless(sys.platform == 'linux', 'wait4/进程组专项仅Linux')
class RunnerTests(unittest.TestCase):
    def test_wait4_and_persistent_stats(self):
        with tempfile.TemporaryDirectory() as tmp:
            run = Runner(Path(tmp) / 'logs', time.monotonic() + 10)
            script = "import os; open(os.environ['RYUPY_STATS'],'wb').write(b'{bad raw'); print('ok')"
            item = run.run('ok', [sys.executable, '-c', script], cwd=tmp,
                           env=os.environ, limit=5, stats=True)
            self.assertEqual(item['stdout'], b'ok\n')
            self.assertEqual(item['exit'], 0)
            self.assertGreater(item['maxrss_bytes'], 0)
            self.assertEqual((Path(item['case']) / 'stats.json').read_bytes(), b'{bad raw')
            self.assertIn('stats_sha256', item)
            with self.assertRaises(FileExistsError):
                run.run('ok', [sys.executable], cwd=tmp, env=os.environ, limit=1)

    def test_deadline_kills_descendant_without_double_wait(self):
        with tempfile.TemporaryDirectory() as tmp:
            run = Runner(Path(tmp) / 'logs', time.monotonic() + 1)
            script = ("import subprocess,sys,time; p=subprocess.Popen([sys.executable,'-c',"
                      "'import signal,time; signal.signal(signal.SIGTERM,signal.SIG_IGN); time.sleep(60)']); "
                      "print(p.pid,flush=True); time.sleep(60)")
            with self.assertRaises(RuntimeError):
                run.run('timeout', [sys.executable, '-c', script], cwd=tmp,
                        env=os.environ, limit=0.2)
            case = Path(tmp) / 'logs' / 'timeout'
            status = json.loads((case / 'status.json').read_text())
            self.assertTrue(status['timeout'])
            self.assertIsNotNone(status['exit'])
            self.assertGreater(status['maxrss_bytes'], 0)
            child = int((case / 'stdout').read_text())
            # 被init尚未回收的僵尸不是运行进程；不能为了断言再杀不属于本组的PID。
            wait_descendant_stopped(Path(f'/proc/{child}/stat'))


if __name__ == '__main__':
    unittest.main()
