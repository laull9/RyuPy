"""纯编排/汇总测试；不调用 SDK、CLI 或性能负载。"""
import copy
import json
import hashlib
from pathlib import Path
import unittest
import module_call_schema as s
import perf_schema


def fixture_records():
    stats={'alloc_bytes':10000000,'main_ms':1,'gc0':0,'gc1':0,'gc2':0,'gc_pause_ms':0,'jit_ms':1,'jit_methods':1,'heap_bytes':1}
    return [dict(repeat=r,fixture=n,arm=a,sample_ms=[1.0]*7,stats=dict(stats),wall_ms=10,maxrss_bytes=1000)
            for r,n,a in s.plan()]


def builds():
    return [dict(version=v,fixture=n) for v in ('B','C') for n in s.FIXTURES]


class PairTests(unittest.TestCase):
    def test_plan_exact_36_unique_rotated_pairs(self):
        p=s.plan(); self.assertEqual(len(p),36);self.assertEqual(len(set(p)),36)
        for i in range(0,36,2):
            self.assertEqual(p[i][:2],p[i+1][:2]);self.assertEqual(p[i][2][-1],p[i+1][2][-1])
            self.assertEqual({p[i][2][0],p[i+1][2][0]},{'B','C'})
        self.assertEqual(p[0],(0,'matmul','B-J'));self.assertEqual(p[12],(1,'math_module','C-T'))

    def test_report_preserves_points_and_aot_jit_na(self):
        r=s.report(fixture_records(),builds());self.assertFalse(r['adoption_gate']);self.assertEqual(r['diagnostic_flags'],[])
        self.assertEqual(r['cases']['matmul-J']['sample_ms']['B'],[[1.0]*7]*3)
        self.assertIsNone(r['cases']['math_module-A']['stats']['B'][0]['jit_ms'])

    def test_negative_cost_flags_are_not_adoption_rejection(self):
        records=fixture_records()
        for r in records:
            if r['arm'].startswith('C'):
                r['sample_ms']=[1.2]*7;r['stats']['alloc_bytes']=12000000
        result=s.report(records,builds());self.assertEqual(len(result['diagnostic_flags']),12)
        self.assertFalse(result['adoption_gate'])

    def test_missing_duplicate_reordered_or_nonfinite_points_fail(self):
        valid=fixture_records()
        for bad in [valid[:-1],valid+[valid[0]],list(reversed(valid))]:
            with self.assertRaises(ValueError):s.report(bad,builds())
        for value in (0,float('nan'),float('inf')):
            bad=copy.deepcopy(valid);bad[0]['sample_ms'][0]=value
            with self.assertRaises(ValueError):s.report(bad,builds())
        with self.assertRaises(ValueError):s.report(valid,builds()[:-1])

    def test_frozen_sources_and_every_output_checksum(self):
        root=Path(__file__).parent/'module_call_fixtures'
        expected=json.loads((root/'expected.json').read_text())
        for name,checksum in [('matmul','64345.098'),('math_module','200000')]:
            self.assertEqual(hashlib.sha256((root/f'{name}.py').read_bytes()).hexdigest(),expected['fixtures'][name]['source_sha256'])
            raw=(''.join(f'warm {i} {checksum}\n' for i in range(3))+''.join(f'sample {i} 0.001 {checksum}\n' for i in range(7))).encode()
            self.assertEqual(len(perf_schema.output(raw,checksum,hot=True)),7)
            with self.assertRaises(ValueError):perf_schema.output(raw.replace(b'warm 1 ',b'warm 9 '),checksum,hot=True)


if __name__=='__main__':unittest.main()
