#!/usr/bin/env python3
"""锚点源码变换与路径安全约束的轻量单元测试（不触发重编译）。"""
from __future__ import annotations

from pathlib import Path
import sys
import tempfile
import unittest

KERNEL_DIR = Path(__file__).resolve().parent
if str(KERNEL_DIR) not in sys.path:
    sys.path.insert(0, str(KERNEL_DIR))

from build_kernel import KernelBuildError, ensure_outside_repo
from transform import (
    AnchorEdit,
    AnchorTransformError,
    NewHeaderFile,
    apply_anchors,
    build_anchors,
)


class TransformTests(unittest.TestCase):
    def test_single_match_succeeds_and_dry_run_leaves_clean(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            target = root / "sample.c"
            target.write_text("alpha\nbeta\ngamma\n", encoding="utf-8")
            edits = [AnchorEdit("sample.c", "one", "beta", "BETA")]
            headers = [NewHeaderFile("Include/test_hdr.h", "#define OK 1\n")]

            dry = apply_anchors(root, anchors=edits, headers=headers, dry_run=True)
            self.assertEqual(dry, {"sample.c": 1})
            self.assertEqual(target.read_text(encoding="utf-8"), "alpha\nbeta\ngamma\n")
            self.assertFalse((root / "Include/test_hdr.h").exists())

            applied = apply_anchors(root, anchors=edits, headers=headers, dry_run=False)
            self.assertEqual(applied, {"sample.c": 1})
            self.assertEqual(target.read_text(encoding="utf-8"), "alpha\nBETA\ngamma\n")
            self.assertTrue((root / "Include/test_hdr.h").is_file())

    def test_zero_match_and_ambiguous_match_fail_atomically(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "a.c").write_text("first\n", encoding="utf-8")
            (root / "b.c").write_text("dup\ndup\n", encoding="utf-8")

            with self.assertRaises(AnchorTransformError):
                apply_anchors(
                    root,
                    anchors=[
                        AnchorEdit("a.c", "ok", "first", "FIRST"),
                        AnchorEdit("b.c", "missing", "absent", "X"),
                    ],
                    headers=[],
                )
            # 确认原子预检生效：a.c 未被部分改写。
            self.assertEqual((root / "a.c").read_text(encoding="utf-8"), "first\n")

            with self.assertRaises(AnchorTransformError):
                apply_anchors(
                    root,
                    anchors=[AnchorEdit("b.c", "ambiguous", "dup", "ONE")],
                    headers=[],
                )
            self.assertEqual((root / "b.c").read_text(encoding="utf-8"), "dup\ndup\n")

    def test_version_anchor_rejects_non_312_13(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "Include").mkdir()
            (root / "Include/patchlevel.h").write_text(
                '#define PY_MAJOR_VERSION        3\n'
                '#define PY_MINOR_VERSION        13\n'
                '#define PY_MICRO_VERSION        0\n'
                '#define PY_VERSION              "3.13.0"\n',
                encoding="utf-8",
            )
            version_anchor = build_anchors()[0]
            with self.assertRaises(AnchorTransformError):
                apply_anchors(root, anchors=[version_anchor], headers=[])

    def test_ensure_outside_repo_rejects_repo_paths(self):
        with self.assertRaises(KernelBuildError):
            ensure_outside_repo(KERNEL_DIR)
        with self.assertRaises(KernelBuildError):
            ensure_outside_repo(KERNEL_DIR / "tmp_build")


if __name__ == "__main__":
    unittest.main()
