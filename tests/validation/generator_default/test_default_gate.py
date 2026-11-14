"""只读CP引用与纯mock阶段合同，不运行Rust/.NET或任何CP fixture。"""
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch, Mock
from types import SimpleNamespace
import default_gate as gate


class DefaultGateTests(unittest.TestCase):
    def test_exact_twelve_independent_cpython_sources(self):
        rows = gate.references(gate.ROOT)
        self.assertEqual(len(rows), 12)
        self.assertEqual(sum(row['exit'] == 0 for row in rows.values()), 9)
        self.assertEqual(rows['unused_identity']['category'], 'legacy-known-trace-debt')
        self.assertEqual(rows['renamed_alias']['category'], 'positive-v1')

    def test_stage_counts_do_not_accept_flag_or_double_lower_as_default(self):
        def marker(event, stage, module='__main__'):
            return f'[ryupy-stage] event={event} stage={stage} module="{module}" elapsed_us=1\n'
        def trace(names):
            lines = []
            for name in names:
                lines += [marker(event, name) for event in ('begin', 'end')]
                if name == 'trace-v1-prepare' and 'mir-build' in names:
                    lines.append(marker('result', 'trace-v1-not-in-domain'))
                if name == 'mir-build':
                    lines += [marker(event, 'final-validate') for event in ('begin', 'end')]
                    lines.append(marker('result', 'trace-legacy'))
            if 'mir-build' in names:
                lines += [marker(event, 'link', '-') for event in ('begin', 'end')]
            return ''.join(lines).encode()
        selected = gate.stages(trace(['trace-v1-prepare', 'trace-v1-lower']), True)
        legacy = gate.stages(trace(['mir-build']), False, True)
        fallback = gate.stages(trace(['trace-v1-prepare', 'mir-build']), False)
        for observed, expected in [(selected, (0, 0)), (legacy, (0, 1)), (fallback, (1, 1))]:
            self.assertEqual(set(observed), {'begin', 'end', 'results'})
            self.assertEqual(observed['results'], dict(zip(('trace-v1-not-in-domain', 'trace-legacy'), expected)))
            self.assertEqual(set(observed['begin']), {'trace-v1-prepare', 'trace-v1-lower', 'mir-build'})
            self.assertEqual(observed['begin'], observed['end'])
        for names, selected, no_need in [(['trace-v1-prepare', 'trace-v1-lower', 'trace-v1-lower'], True, False),
            (['trace-v1-prepare', 'mir-build'], False, True), (['trace-v1-lower', 'mir-build'], False, False),
            (['trace-v1-lower', 'trace-v1-prepare'], True, False)]:
            with self.assertRaises(ValueError): gate.stages(trace(names), selected, no_need)
        with self.assertRaises(ValueError): gate.stages(trace(['mir-build']) + b'[E0310]', False, True)
        with self.assertRaises(ValueError): gate.stages(trace(['mir-build']).replace(b'event=end', b'event=other'), False, True)
        rejected = gate.stages(trace(['trace-v1-prepare']) + b'[E0310]', False, rejected=True)
        self.assertEqual(rejected['results'], {'trace-v1-not-in-domain': 0, 'trace-legacy': 0})
        with self.assertRaises(ValueError): gate.stages(trace(['trace-v1-prepare', 'emit']) + b'[E0310]', False, rejected=True)
        good = trace(['trace-v1-prepare', 'mir-build'])
        for name in ('trace-v1-not-in-domain', 'trace-legacy'):
            row = marker('result', name).encode()
            without = good.replace(row, b'')
            bad = [without, good + row, good.replace(row, marker('result', name, 'helper').encode()),
                   row + without, without + row]
            for event in ('begin', 'end', 'other'):
                wrong = marker(event, name).encode()
                bad += [good.replace(row, wrong), good + wrong]
            for raw in bad:
                with self.subTest(name=name, raw=raw), self.assertRaises(ValueError):
                    gate.stages(raw, False)
            for names, selected, rejected in [(['trace-v1-prepare', 'trace-v1-lower'], True, False),
                                              (['trace-v1-prepare'], False, True)]:
                with self.assertRaises(ValueError):
                    gate.stages(trace(names) + row + (b'[E0310]' if rejected else b''), selected, rejected=rejected)
        with self.assertRaises(ValueError):
            gate.stages(good.replace(b'stage=mir-build module="__main__"', b'stage=mir-build module="helper"'), False)

    def test_real_auto_command_never_has_explicit_flag(self):
        argv = gate.product.build_command('cli', 'entry.py', 'out', False)
        self.assertEqual(argv[:3], ['cli', 'build', 'entry.py'])
        self.assertNotIn('--trace-product-v1', argv)
        self.assertNotIn('--legacy', argv)

    def test_first_compile_or_pe_failure_preserves_exact_existing_product(self):
        for failure in ('compile', 'decode'):
            with self.subTest(failure=failure), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                runner = gate.Runner.__new__(gate.Runner)
                runner.out = root / 'evidence'; runner.out.mkdir()
                runner.work = root / 'work'; runner.work.mkdir()
                source = root / 'source.py'; source.write_bytes(b'print(1)\n')
                runtime = root / 'RyuPy.Runtime.dll'; runtime.write_bytes(b'mock-runtime')
                runner.tools = {'runtime': runtime, 'cli': root / 'ryupy'}
                runner.legacy = root / 'legacy-api'
                events = []
                def preserve(output, stage):
                    events.append('preserve')
                    (stage / 'saved-entry.dll').write_bytes((output / 'entry.dll').read_bytes())
                    return 'before'
                runner.native = SimpleNamespace(preserve_dist=preserve)
                def command(stage, label, argv, *args):
                    output = Path(argv[-1]); output.mkdir()
                    (output / 'entry.dll').write_bytes(b'exact-first-failed-pe')
                    (output / 'RyuPy.Runtime.dll').write_bytes(runtime.read_bytes())
                    if failure == 'compile': raise ValueError('first compile failure')
                    return {'exit': 0}, b'', b''
                def decode(*args):
                    events.append('decode')
                    raise ValueError('first PE contract failure')
                runner.command = command; runner.decode = decode
                reference = {'source': source, 'source_sha256': gate.c.sha(source), 'category': 'legacy-success'}
                with patch.object(gate.product, 'require_plan'), self.assertRaises(ValueError):
                    runner.control('probe', reference)
                self.assertEqual(events, ['preserve'] if failure == 'compile' else ['preserve', 'decode'])
                self.assertEqual((runner.out / 'probe/legacy-api/saved-entry.dll').read_bytes(), b'exact-first-failed-pe')

    def test_domain_compile_failure_survives_a_second_archive_failure(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            runner = gate.Runner.__new__(gate.Runner)
            runner.out = root / 'evidence'; runner.out.mkdir()
            runner.work = root / 'work'; runner.work.mkdir()
            source = root / 'source.py'; source.write_bytes(b'print(1)\n')
            runner.tools = {'cli': root / 'not-executed'}
            runner.legacy = root / 'not-executed-api'
            calls = []
            def command(stage, label, argv, *args):
                calls.append(label)
                output = Path(argv[-1]); output.mkdir()
                (output / 'entry.dll').write_bytes(b'partial')
                raise ValueError('first-domain-compile-failure')
            def archive(*args): raise OSError('second-domain-archive-failure')
            runner.command = command
            runner.native = SimpleNamespace(preserve_dist=archive)
            with self.assertRaisesRegex(ValueError, 'first-domain-compile-failure'):
                runner.control('probe', {'source': source, 'category': 'legacy-success'})
            self.assertEqual(calls, ['compile'])
            import json
            record = json.loads((runner.out / 'probe/legacy-api/preservation-failure.json').read_text())
            self.assertEqual(record['error'], 'second-domain-archive-failure')
            self.assertEqual(record['primary_error'], 'first-domain-compile-failure')


if __name__ == '__main__': unittest.main()
