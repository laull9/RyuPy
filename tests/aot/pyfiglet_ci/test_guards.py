"""纯合成字节/临时目录/mocks覆盖停止条件；不启动CP、编译器或目标产品。"""
import json
from pathlib import Path
import subprocess
import struct
import sys
import tempfile
import unittest
from unittest.mock import patch

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
import guards as g
import run as h
from manifest_files import ManifestError


class GuardsTest(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)

    def test_three_streams_and_control(self):
        ok = subprocess.CompletedProcess([], 0, b'golden', b'')
        h.positive(ok, b'golden')
        for code, out, err in [(1, b'golden', b''), (0, b'golden', b'x'), (0, b'other', b'')]:
            with self.assertRaises(RuntimeError):
                h.positive(subprocess.CompletedProcess([], code, out, err), b'golden')
        h.missing(subprocess.CompletedProcess([], 1, b'', b'any error'), b'golden')
        for result in (ok, subprocess.CompletedProcess([], 1, b'golden', b'error')):
            with self.assertRaises(RuntimeError):
                h.missing(result, b'golden')

    def test_environment_excludes_injection(self):
        with patch.dict('os.environ', {'PYTHONHOME': 'bad', 'PYTHONPATH': 'bad', 'LD_PRELOAD': 'bad'}):
            env = h.base_environment(Path('/sdk/dotnet'), Path('/rt/RyuPy.Runtime.dll'))
        self.assertFalse({'PYTHONHOME', 'PYTHONPATH', 'LD_PRELOAD'} & set(env))
        self.assertEqual((env['DOTNET'], env['DOTNET_ROOT'], env['RYUPY_RUNTIME_DIR']),
                         ('/sdk/dotnet', '/sdk', '/rt'))

    def test_reported_managed_rejects_guess_and_escape(self):
        dll = self.root / 'actual.dll'
        dll.write_bytes(b'MZsynthetic')
        self.assertEqual(g.reported_managed(('已生成 ' + str(dll)).encode(), self.root), dll)
        for text in ('', '已生成 relative.dll', '已生成 ' + str(dll) + '\n已生成 ' + str(dll)):
            with self.assertRaises(RuntimeError):
                g.reported_managed(text.encode(), self.root)
        with self.assertRaises(RuntimeError):
            g.reported_managed(('已生成 ' + str(dll)).encode(), self.root / 'other')

    def test_graph_feature_not_default_control(self):
        p = self.root / 'graph.json'
        modules = [dict(module=name, compiled=True, execution='LegacySingleton',
                        ordinary_name_handlers=False, relative_from_import=False)
                   for name in ('__main__', 'pyfiglet', 'pyfiglet.version', 'pyfiglet.fonts')]
        data = dict(compiled_feature=True, session_policy='ordinary-dynamic-module-pilot', modules=modules)
        p.write_text(json.dumps(data))
        g.graph(p, True)
        with self.assertRaises(RuntimeError):
            g.graph(p, False)
        modules[-1]['resource_discovery'] = dict(source='pyfiglet', anchor='pyfiglet.fonts', reason='literal-files-empty-direct-package')
        p.write_text(json.dumps(data))
        g.graph(p, False)
        with self.assertRaises(RuntimeError):
            g.graph(p, True)
        data['compiled_feature'] = False
        p.write_text(json.dumps(data))
        with self.assertRaises(RuntimeError):
            g.graph(p, False)

    def test_resource_record_exact_role_consumer_hash(self):
        resource = dict(relative_path='safe/standard.flf', size=28504, sha256=g.FONT_SHA,
                        consumer_module_ids=[3])
        artifact = dict(relative_path=resource['relative_path'], size=28504,
                        sha256=g.FONT_SHA, role='declared_resource')
        manifest = dict(resources=[resource], artifacts=[artifact], module_locations=[
            dict(module_id=3, module_name='pyfiglet.fonts', kind='executable_relative')])
        self.assertEqual(g.resource_record(manifest), resource)
        for key, value in [('role', 'unclassified_sidecar'), ('size', 1), ('sha256', 'bad')]:
            old = artifact[key]
            artifact[key] = value
            with self.assertRaises(RuntimeError):
                g.resource_record(manifest)
            artifact[key] = old
        resource['relative_path'] = artifact['relative_path'] = '../escape'
        with self.assertRaises(ManifestError):
            g.resource_record(manifest)

    def test_remove_only_independent_copy_and_no_links(self):
        source = self.root / 'source'
        source.mkdir()
        font = source / 'standard.flf'
        font.write_bytes(b'x' * 28504)
        original, copy = self.root / 'original', self.root / 'copy'
        for root in (original, copy):
            (root / 'data').mkdir(parents=True)
            (root / 'data/font').write_bytes(font.read_bytes())
        record = dict(relative_path='data/font')
        target = copy / record['relative_path']
        with patch.object(g, 'FONT_SHA', g.sha(font)):
            target.unlink()
            target.hardlink_to(font)
            with self.assertRaises(RuntimeError):
                g.remove_copy_font(copy, original, font, record)
            target.unlink()
            target.symlink_to(font)
            with self.assertRaises(ManifestError):
                g.remove_copy_font(copy, original, font, record)
            target.unlink()
            target.write_bytes(font.read_bytes())
            g.remove_copy_font(copy, original, font, record)
        self.assertFalse(target.exists())
        self.assertTrue(font.exists())
        self.assertTrue((original / 'data/font').exists())

    def test_recorder_precreates_streams_and_prevents_retry(self):
        run = h.Recorder(self.root / 'logs')
        fake = unittest.mock.Mock()
        fake.wait.return_value = 0
        def spawn(*args, **kwargs):
            self.assertTrue((run.logs / 'once.command.json').exists())
            self.assertTrue((run.logs / 'once.stdout').exists())
            self.assertTrue((run.logs / 'once.stderr').exists())
            return fake
        with patch.object(h.subprocess, 'Popen', side_effect=spawn) as call:
            run.run('once', ['fake'], cwd=self.root, env={})
            with self.assertRaises(RuntimeError):
                run.run('once', ['fake'], cwd=self.root)
            self.assertEqual(call.call_count, 1)

    def test_pollution_does_not_remove_host_files(self):
        cwd, shared, entry, source = [self.root / name for name in ('cwd', 'shared', 'entry', 'source')]
        for p in (cwd, shared, entry, source):
            p.mkdir()
        bad = shared / 'standard.flf'
        bad.write_bytes(b'host-owned')
        with self.assertRaises(RuntimeError):
            g.pollution(cwd, entry / 'main.py', source, [], shared)
        self.assertEqual(bad.read_bytes(), b'host-owned')

    def test_deployment_rejects_known_source_managed_and_disguised_pe(self):
        dist = self.root / 'dist'
        dist.mkdir()
        (dist / 'ryupy-artifact.json').write_text('{}')
        native = dist / 'program'
        elf = bytearray(64)
        elf[:7] = b'\x7fELF\x02\x01\x01'
        struct.pack_into('<HHI', elf, 16, 3, 62, 1)
        struct.pack_into('<H', elf, 52, 64)
        native.write_bytes(elf)
        def record(path, role):
            return dict(relative_path=path.name, role=role, size=path.stat().st_size, sha256=g.sha(path))
        manifest = {'artifacts': [record(native, 'native_executable')]}
        self.assertEqual(len(g.deployment_files(dist, manifest)['files']), 1)
        debug = dist / 'RyuPyAotHost_synthetic.dbg'
        debug.write_bytes(elf)
        manifest['artifacts'].append(record(debug, 'unclassified_sidecar'))
        self.assertEqual(len(g.deployment_files(dist, manifest)['files']), 2)
        manifest['artifacts'].pop()
        debug.unlink()
        cases = [('unknown.txt', b'unknown'), ('not-elf.dbg', b'debug'), ('module.py', b'print(1)'), ('module.pyc', b'cache'), ('module.pyo', b'cache'),
                 ('RyuPy.Runtime.dll', b'MZruntime'), ('managed-entry.dll', b'MZentry'),
                 ('program.runtimeconfig.json', b'{}'), ('program.deps.json', b'{}'),
                 ('renamed.bin', b'MZmanaged')]
        for name, data in cases:
            with self.subTest(name=name):
                sidecar = dist / name
                sidecar.write_bytes(data)
                manifest['artifacts'].append(record(sidecar, 'unclassified_sidecar'))
                with self.assertRaises((RuntimeError, ManifestError)):
                    g.deployment_files(dist, manifest)
                manifest['artifacts'].pop()
                sidecar.unlink()
        extra = dist / 'extra'
        extra.write_bytes(b'not declared')
        with self.assertRaises(RuntimeError):
            g.deployment_files(dist, manifest)
        extra.unlink()
        extra.symlink_to(native)
        with self.assertRaises(ManifestError):
            g.deployment_files(dist, manifest)

    def test_pollution_failure_saves_all_observations_before_rejecting(self):
        cwd, shared, entry, source, site = [self.root / name
                                          for name in ('cwd', 'shared', 'entry', 'source', 'site')]
        for path in (cwd, shared, entry, source, site):
            path.mkdir()
        (cwd / 'unexpected').write_text('owned test')
        (shared / 'standard.flf').write_text('host font')
        # 源字体故意缺失，site故意不可读；不是依赖root用户的chmod假权限测试。
        report = self.root / 'pollution.json'
        original = Path.iterdir
        def inaccessible(path):
            if path == site:
                raise PermissionError('synthetic unreadable site')
            return original(path)
        with patch.object(Path, 'iterdir', inaccessible):
            with self.assertRaises(RuntimeError):
                g.pollution(cwd, entry / 'main.py', source, [str(site)], shared, report_path=report)
        data = json.loads(report.read_text())
        self.assertFalse(data['passed'])
        self.assertEqual(data['phase'], 'pollution-preflight')
        rows = {row['path']: row for row in data['observations']}
        self.assertEqual(rows[str(cwd)]['entries'], ['unexpected'])
        self.assertTrue(rows[str(shared / 'standard.flf')]['exists'])
        self.assertIn('PermissionError', rows[str(site)]['error'])
        self.assertIn('FileNotFoundError', rows[str(source / 'pyfiglet/fonts/standard.flf')]['error'])
        self.assertGreaterEqual(len(data['errors']), 4)
        self.assertTrue((shared / 'standard.flf').exists())

    def test_pollution_permission_is_not_reported_as_missing(self):
        cwd = self.root / 'cwd'
        cwd.mkdir()
        shared = self.root / 'shared'
        shared.mkdir()
        report = self.root / 'pollution.json'
        original = g.exists
        def unreadable(path):
            if path == shared / 'standard.tlf':
                raise PermissionError('synthetic unreadable parent')
            return original(path)
        with patch.object(g, 'exists', unreadable):
            with self.assertRaises(RuntimeError):
                g.pollution(cwd, self.root / 'entry/main.py', self.root / 'source', [],
                            shared, report_path=report)
        row = next(row for row in json.loads(report.read_text())['observations']
                   if row['path'] == str(shared / 'standard.tlf'))
        self.assertIn('PermissionError', row['error'])
        self.assertNotIn('exists', row)

    def test_missing_exit_classification_does_not_accept_crash(self):
        for code, expected in [(-11, 'unexpected-crash'), (-6, 'unexpected-crash'),
                               (134, 'unexpected-crash'), (139, 'unexpected-crash'),
                               (137, 'unexpected-crash'), (0, 'unexpected-exit'),
                               (2, 'unexpected-exit'), (127, 'unexpected-exit'),
                               (1, 'expected-refusal')]:
            with self.subTest(exit=code):
                report = self.root / ('exit-' + str(code) + '.json')
                actual = subprocess.CompletedProcess([], code, b'', b'arbitrary exception text')
                if code == 1:
                    self.assertEqual(h.missing(actual, b'golden', report)['classification'], expected)
                else:
                    with self.assertRaisesRegex(RuntimeError, expected):
                        h.missing(actual, b'golden', report)
                self.assertEqual(json.loads(report.read_text())['classification'], expected)
        with self.assertRaisesRegex(RuntimeError, 'unexpected-output'):
            h.missing(subprocess.CompletedProcess([], 1, b'golden', b'error'), b'golden')

    def test_recorder_launch_error_is_environment_and_not_application_exit(self):
        run = h.Recorder(self.root / 'launch-logs')
        error = OSError('synthetic launch failure')
        with patch.object(h.subprocess, 'Popen', side_effect=error):
            with self.assertRaises(OSError) as caught:
                run.run('launch', ['fake'], cwd=self.root, env={})
        self.assertIs(caught.exception, error)
        report = json.loads((run.logs / 'launch.status.json').read_text())
        self.assertEqual(report['status'], 'launch-failed')
        self.assertEqual(report['classification'], 'environment')
        self.assertIsNone(report['exit'])
        self.assertIn('synthetic launch failure', report['error'])
        self.assertIn('finished', report)
        self.assertEqual((run.logs / 'launch.stdout').read_bytes(), b'')
        self.assertEqual((run.logs / 'launch.stderr').read_bytes(), b'')
        with self.assertRaisesRegex(RuntimeError, '禁止重复命令'):
            run.run('launch', ['fake'], cwd=self.root)


if __name__ == '__main__':
    unittest.main()
