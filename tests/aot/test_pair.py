"""配对编排轻自测：只验证计划/schema/阈值，不启动SDK或被测性能进程。"""
import unittest

import pair_schema as schema


def guard_text():
    lines = []
    for name, value in schema.GUARDS.items():
        lines += [f'warm {name} {i} {value}' for i in range(3)]
        lines += [f'sample {name} {i} 0.1 {value}' for i in range(7)]
    return ('\n'.join(lines) + '\n').encode()


class PairTests(unittest.TestCase):
    def test_fixed_matrix_and_order(self):
        plan = schema.plan()
        self.assertEqual(len(plan), 180)
        self.assertEqual(len(set(plan)), 180)
        self.assertEqual([sum(p[0] == phase for p in plan) for phase in ('cold', 'hot', 'guard')], [90, 72, 18])
        self.assertEqual([p[3] for p in plan[:6]], list(schema.ORDERS[0]))
        for phase in ('cold', 'hot', 'guard'):
            for repeat in range(3):
                arms = [p[3] for p in plan if p[0] == phase and p[1] == repeat]
                for offset in range(0, len(arms), 6):
                    self.assertEqual(arms[offset:offset + 6], list(schema.ORDERS[repeat]))

    def test_guard_parser_exact_counts_checksums_and_finiteness(self):
        raw = guard_text()
        self.assertEqual(sum(map(len, schema.guard_output(raw).values())), 28)
        for broken in [raw + b'\nextra\n', raw.replace(b'100000, 100000', b'100000, 99999', 1),
                       raw.replace(b'sample float 0 0.1', b'sample float 0 nan', 1),
                       raw.replace(b'sample bool 1', b'sample bool 0', 1),
                       raw.replace(b'9223372036854775807', b'9223372036854775808', 1)]:
            with self.assertRaises(ValueError):
                schema.guard_output(broken)

    def test_threshold_report_never_approves_or_repeats(self):
        records = []
        for phase, repeat, kernel, arm in schema.plan():
            candidate = arm.startswith('C-')
            duration = 0.9 if candidate and kernel == 'calls' else 1.0
            records.append({'phase': phase, 'repeat': repeat, 'kernel': kernel, 'arm': arm,
                            'samples': [duration] * 7 if phase == 'hot' else [],
                            'guard_samples': {name: [1.0] * 7 for name in schema.GUARDS} if phase == 'guard' else {},
                            'stats': {'alloc_bytes': 75 if candidate and kernel == 'calls' else 100},
                            'wall_ms': 1.0})
        builds = [{'version': version, 'kernel': kernel, 'native_file_bytes': 100}
                  for version in ('B', 'C') for kernel in (*schema.KERNELS, 'guard')]
        report = schema.report(records, builds)
        self.assertEqual(report['blocking_thresholds'], [])
        self.assertEqual(report['code_size_review'], 'pending-static-disassembly')
        self.assertIn('no-automatic', report['decision'])
        for row in records:
            if row['phase'] == 'guard' and row['arm'] == 'C-A':
                row['guard_samples']['bool'] = [1.06] * 7
        self.assertIn('hot:bool-A', schema.report(records, builds)['blocking_thresholds'])
        with self.assertRaises(ValueError):
            schema.report(records[:-1], builds)


if __name__ == '__main__':
    unittest.main()
