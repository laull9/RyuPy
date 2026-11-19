"""Mach-O部署审计的独立布局与破坏合同；手写映像事实，不依赖编译器的计划结果。"""
from pathlib import Path
import tempfile
import os
import unittest

from macho_audit import Image, Library, audit_images, parse_image


def library(name, current=(4, 2, 0), compatibility=(4, 0, 0)):
    return Library(name, compatibility, current)


def image(identity=None, dependencies=(), rpaths=(), architectures=('arm64',)):
    return Image(identity, tuple(dependencies), tuple(rpaths), frozenset(architectures))


class MachoAuditTests(unittest.TestCase):
    def file(self, root, name):
        path = root / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(b'handwritten image fact')
        return path.resolve()

    def test_two_independent_local_layouts_and_ordered_first_hit(self):
        layouts = [('internal/python/extensions/consumer', 'internal/python/extensions/libs/harbor.4',
                    ['@loader_path/empty', '@loader_path/libs'], '@rpath/harbor.4'),
                   ('runtime/addons/driver', 'frameworks/Photon.framework/Versions/A/Photon',
                    ['@loader_path/../../frameworks'], '@rpath/Photon.framework/Versions/A/Photon')]
        for caller_name, target_name, rpaths, name in layouts:
            with self.subTest(name=name), tempfile.TemporaryDirectory(prefix='ryupy-macho-audit-') as tmp:
                root = Path(tmp).resolve()
                caller, target = self.file(root, caller_name), self.file(root, target_name)
                # 第一候选可以是尚不存在的内部目录，dyld继续匹配第二候选。
                for value in rpaths:
                    if not value.endswith('/empty'):
                        (caller.parent / value[len('@loader_path/'):]).mkdir(parents=True, exist_ok=True)
                images = {caller: image(dependencies=[library(name)], rpaths=rpaths),
                          target: image(library(name))}
                audit_images(root, images)
                # 原严格loader路径仍走同一拥有/ABI合同。
                relative = os.path.relpath(target, caller.parent)
                images[caller] = image(dependencies=[library('@loader_path/' + relative)])
                audit_images(root, images)

    def test_escape_missing_conflicting_ancestor_identity_and_abi_are_rejected(self):
        for fault in ('escape', 'absolute', 'missing', 'unowned', 'conflict', 'identity', 'abi', 'architecture'):
            with self.subTest(fault=fault), tempfile.TemporaryDirectory(prefix='ryupy-macho-audit-') as tmp:
                outer = Path(tmp).resolve()
                root = outer / 'product'
                root.mkdir()
                caller, target = self.file(root, 'module/consumer'), self.file(root, 'module/libs/copper.4')
                name = '@rpath/copper.4'
                images = {caller: image(dependencies=[library(name)], rpaths=['@loader_path/libs']),
                          target: image(library(name))}
                if fault == 'escape':
                    images[caller] = image(dependencies=[library(name)], rpaths=['@loader_path/../..'])
                elif fault == 'absolute':
                    images[caller] = image(dependencies=[library(name)], rpaths=[str(target.parent)])
                elif fault == 'missing':
                    target.unlink()
                elif fault == 'unowned':
                    del images[target]
                elif fault == 'conflict':
                    ancestor = self.file(root, 'other/ancestor')
                    alternate = self.file(root, 'other/libs/copper.4')
                    images[ancestor] = image(rpaths=['@loader_path/libs'])
                    images[alternate] = image(library(name))
                elif fault == 'identity':
                    images[target] = image(library('@rpath/other.4'))
                elif fault == 'abi':
                    images[target] = image(library(name, current=(3, 0, 0)))
                elif fault == 'architecture':
                    images[target] = image(library(name), architectures=['x86_64'])
                with self.assertRaises((AssertionError, FileNotFoundError)):
                    audit_images(root, images)

    def test_parser_retains_real_versions_spaces_rpath_order_and_self_load(self):
        name = '@rpath/with spaces/harbor.4'
        row = name + ' (compatibility version 4.0.0, current version 4.2.0)'
        commands = 'cmd LC_RPATH\npath @loader_path/first path (offset 12)\ncmd LC_LOAD_DYLIB\npath ignored (offset 24)\ncmd LC_RPATH\npath @loader_path/second (offset 12)\n'
        parsed = parse_image('image:\n' + row + '\n' + row + '\n', 'image:\n' + name + '\n', commands, ['arm64'])
        self.assertEqual(parsed.identity.name, name)
        self.assertEqual(len(parsed.dependencies), 1)
        self.assertEqual(parsed.dependencies[0].compatibility, (4, 0, 0))
        self.assertEqual(parsed.rpaths, ('@loader_path/first path', '@loader_path/second'))


if __name__ == '__main__':
    unittest.main()
