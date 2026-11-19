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

    def test_custom_eval_guard_preserves_atomic_precheck(self):
        # 此门必须覆盖 eval_frame 已安装后的分派；删掉目标条件应在任何落盘前失败。
        from anchors_guards import build_guard_anchors
        edit = next(item for item in build_guard_anchors() if item.name == "guard_custom_eval")
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            target = root / edit.rel_path
            target.parent.mkdir(parents=True)
            target.write_text(edit.old, encoding="utf-8")
            apply_anchors(root, anchors=[edit], headers=[])
            self.assertIn("rp_kernel_get_strict_reject() ||", target.read_text())
            with self.assertRaises(AnchorTransformError):
                apply_anchors(root, anchors=[edit], headers=[])
            self.assertEqual(target.read_text(), edit.new)

    def test_stdio_codecs_anchors_and_header_present(self):
        from anchors_stdio_codecs import (
            RYUPY_UTF8_CODEC_HEADER,
            RYUPY_UTF8_INCREMENTAL_HEADER,
            build_stdio_codecs_anchors,
        )
        from transform import default_headers

        anchors = build_anchors()
        stdio_names = {a.name for a in build_stdio_codecs_anchors()}
        self.assertTrue(stdio_names.issubset({a.name for a in anchors}))
        header_paths = {h.rel_path for h in default_headers()}
        self.assertIn("Python/ryupy_utf8_codec.h", header_paths)
        self.assertIn("Python/ryupy_utf8_incremental.h", header_paths)
        self.assertIn("rp_kernel_register_utf8_codec", RYUPY_UTF8_CODEC_HEADER.content)
        self.assertIn("RpUtf8IncDecoder", RYUPY_UTF8_INCREMENTAL_HEADER.content)

    def test_c4_1_import_router_anchors_and_abi_constraints(self):
        from anchors_eval_import import RYUPY_KERNEL_HEADER
        from anchors_import_router import (
            RYUPY_IMPORT_ABI_HEADER,
            RYUPY_IMPORT_REGISTRY_HEADER,
            build_import_router_anchors,
            build_import_router_headers,
        )
        from transform import default_headers

        anchors = build_anchors()
        router_anchors = build_import_router_anchors()
        router_names = {a.name for a in router_anchors}
        self.assertTrue(router_names.issubset({a.name for a in anchors}))
        # 绝不修改 PyInterpreterState / struct _import_state 既有布局。
        for a in anchors:
            if a.rel_path == "Include/internal/pycore_import.h":
                self.assertNotIn("ryupy_state", a.new)
        header_paths = {h.rel_path for h in default_headers()}
        for h in build_import_router_headers():
            self.assertIn(h.rel_path, header_paths)
        self.assertIn('#include "ryupy_import_abi.h"', RYUPY_KERNEL_HEADER.content)
        self.assertNotIn("pycore_lock.h", RYUPY_IMPORT_REGISTRY_HEADER.content)
        self.assertIn("package_dir", RYUPY_IMPORT_ABI_HEADER.content)
        for rel in (
            "Python/ryupy_import_loader_common.h",
            "Python/ryupy_import_loaders.h",
            "Python/ryupy_import_ext_loader.h",
            "Python/ryupy_import_cil_loader.h",
        ):
            self.assertIn(rel, header_paths)
        for h in build_import_router_headers():
            if "loader" in h.rel_path:
                self.assertNotIn("METH_O", h.content, f"{h.rel_path} 不得残留 METH_O")

    def test_ensure_outside_repo_rejects_repo_paths(self):
        with self.assertRaises(KernelBuildError):
            ensure_outside_repo(KERNEL_DIR)
        with self.assertRaises(KernelBuildError):
            ensure_outside_repo(KERNEL_DIR / "tmp_build")


if __name__ == "__main__":
    unittest.main()
