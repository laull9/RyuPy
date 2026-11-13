"""采样命令/解析边界负测；不把轻量合同计为真实CPU profile通过。"""
from pathlib import Path
import unittest
from run import profile_command, sample_count, report_rows, WORKLOADS


class ProfileTests(unittest.TestCase):
    def test_profile_is_native_only_and_does_not_change_kernel_settings(self):
        argv = profile_command('/usr/lib/linux-tools/6/perf', Path('/out/perf.data'), Path('/out/app.stderr'),
                               Path('/app/__main__'), ['-f', 'standard', 'hi;echo bad'])
        self.assertEqual(argv[:4], ['sudo', '-n', '--', '/usr/lib/linux-tools/6/perf'])
        self.assertEqual(argv[argv.index('-e')+1], 'cpu-clock:u')
        self.assertEqual(argv[argv.index('-F')+1], '499')
        self.assertEqual(argv[-1], 'hi;echo bad')
        self.assertNotIn('hi;echo bad', argv[argv.index('-c')+1])
        self.assertNotIn('sysctl', ' '.join(argv))
        self.assertNotIn('dotnet', ' '.join(argv))
        self.assertIn('dwarf,16384', argv)

    def test_sample_count_counts_events_not_stack_frames(self):
        self.assertEqual(sample_count('# event header\n  2004008 cpu-clock:u:\n 2004008 cpu-clock:u\n'), 2)
        self.assertEqual(sample_count('cpu-clock:u: 2004008\n'), 1)
        for raw in ['', ' # empty\n', '1 cycles:u:\n', '2004 cpu-clock:u:\n  7ff arbitrary_frame\n']:
            with self.assertRaises(ValueError): sample_count(raw)

    def test_report_keeps_native_and_unresolved_samples_distinct(self):
        rows = report_rows('# header\n 60.00;300;__main__;[.] RyuPy_Runtime_PyText__Length\n 40.00%;200;[unknown];[.] 0x123\n')
        self.assertEqual(sum(row['samples'] for row in rows), 500)
        self.assertEqual(rows[0]['dso'], '__main__')
        self.assertEqual(rows[1]['dso'], '[unknown]')
        with self.assertRaises(ValueError): report_rows('# no results\n')
        with self.assertRaises(ValueError): report_rows('101.0%;2;app;symbol\n')
        with self.assertRaises(ValueError): report_rows('1.0%;-2;app;symbol\n')

    def test_profile_load_is_separate_from_existing_short_workloads(self):
        self.assertEqual(WORKLOADS['hi'], ['-f','standard','hi'])
        self.assertEqual(WORKLOADS['longhi100'][-1], 'hi'*100)
        self.assertEqual(WORKLOADS['profilehi800'], ['-f','standard','-w','20000','hi'*800])


if __name__ == '__main__': unittest.main()
