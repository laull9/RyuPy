"""验证外壳对允许非零退出与失败的处理；不冒充产品测试。"""
import pathlib
import subprocess
import tempfile
import unittest
from unittest.mock import patch
import validate


class EvidenceTest(unittest.TestCase):
    def test_allowed_nonzero_has_quiet_resource_measurement(self):
        with tempfile.TemporaryDirectory() as tmp:
            out = pathlib.Path(tmp)
            def child(command, **kwargs):
                self.assertEqual(command[:4], ["/usr/bin/time", "-q", "-f", "%M"])
                pathlib.Path(command[command.index("-o") + 1]).write_text("46540\n")
                return subprocess.CompletedProcess(command, 1, b"prefix", b"expected error")
            with patch.object(validate.subprocess, "run", child):
                streams, timing = validate.run(["fake"], out, "base", allowed_exit=(0, 1))
            self.assertEqual(streams, (b"prefix", b"expected error"))
            self.assertEqual(timing["rss_kib"], 46540)
            self.assertEqual((out / "base.exit").read_text(), "1")

    def test_unexpected_nonzero_still_fails_and_keeps_evidence(self):
        with tempfile.TemporaryDirectory() as tmp:
            out = pathlib.Path(tmp)
            def child(command, **kwargs):
                pathlib.Path(command[command.index("-o") + 1]).write_text("100\n")
                return subprocess.CompletedProcess(command, 2, b"partial", b"failure")
            with patch.object(validate.subprocess, "run", child):
                with self.assertRaises(AssertionError):
                    validate.run(["fake"], out, "candidate")
            self.assertEqual((out / "candidate.exit").read_text(), "2")
            self.assertEqual((out / "candidate.stderr").read_bytes(), b"failure")


if __name__ == "__main__":
    unittest.main()
