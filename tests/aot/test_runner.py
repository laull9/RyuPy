"""无需 SDK 的 Runner 生命周期自测：超时必须终止本次 shell 及其 sleep 子进程。"""
import json
import os
from pathlib import Path
import subprocess
import tempfile
import time
import unittest

from smoke import Runner


class RunnerTimeoutTests(unittest.TestCase):
    @unittest.skipUnless(os.name == "posix" and Path("/proc").is_dir(), "Linux 进程组验收")
    def test_timeout_kills_own_process_group_and_keeps_partial_logs(self):
        with tempfile.TemporaryDirectory(prefix="ryupy-runner-test-") as directory:
            root = Path(directory)
            script = "sleep 60 & child=$!; echo $child; echo partial-error >&2; wait $child"
            with self.assertRaises(subprocess.TimeoutExpired):
                Runner(root).run("timeout", ["/bin/sh", "-c", script], cwd=root, timeout=0.5)
            child = int((root / "timeout.stdout").read_text().strip())
            self.assertEqual((root / "timeout.stderr").read_bytes(), b"partial-error\n")
            state = json.loads((root / "timeout.status.json").read_text())
            self.assertTrue(state["timeout"])
            self.assertLess(state["exit"], 0)
            # 被SIGKILL的孤儿可能等待容器init收尸；Z是已终止，不是仍运行。
            deadline = time.monotonic() + 3
            while True:
                try:
                    status = Path(f"/proc/{child}/stat").read_text().rsplit(")", 1)[1].split()[0]
                except FileNotFoundError:
                    break
                if status == "Z":
                    break
                if time.monotonic() >= deadline:
                    self.fail(f"超时后子进程仍运行：pid={child}, state={status}")
                time.sleep(0.02)
            group = state["killed_process_group"]
            self.assertNotEqual(group, os.getpgrp(), "不能终止父测试进程组")


if __name__ == "__main__":
    unittest.main()
