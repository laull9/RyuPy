"""同机A/B计划、完整输出、错误拒绝和诊断隔离的轻量自检。"""
from collections import Counter
from pathlib import Path
import tempfile
import unittest

from string_ab_protocol import ARMS, CASES, ROUNDS, collect, schedule, summarize


class Fake:
    def __init__(self, path, bad=False):self.path=path;self.calls=[];self.bad=bad
    def run(self,label,command,**kw):
        self.calls.append((label,command,kw))
        case=self.path/label;case.mkdir()
        # 中间错误而首尾相同必须被完整双流比较拒绝。
        output=b'ab-middle-ab' if not(self.bad and 'base_jit' in label) else b'ab-WRONG-ab'
        return dict(exit=0,stdout=output,stderr=b'',case=str(case),wall_ms=1.0)


class ProtocolTest(unittest.TestCase):
    def test_balanced_fixed_schedule(self):
        rows=list(schedule());self.assertEqual(len(rows),250)
        self.assertEqual(len(set(rows)),250)
        for case in CASES:
            counts=Counter()
            for r in range(ROUNDS):
                order=[a for rr,c,a in rows if rr==r and c==case]
                self.assertEqual(set(order),set(ARMS))
                counts.update((a,position) for position,a in enumerate(order))
            self.assertEqual(set(counts.values()),{2})
        for case in CASES:
            positions=[]
            for r in range(ROUNDS):
                order=list(dict.fromkeys(c for rr,c,a in rows if rr==r))
                positions.append(order.index(case))
            self.assertEqual(set(Counter(positions).values()),{2})

    def test_complete_and_no_stats(self):
        with tempfile.TemporaryDirectory() as path:
            fake=Fake(Path(path));products={c:{a:[a,c] for a in ARMS} for c in CASES};saved=[]
            expected,rows=collect(fake,products,Path(path),{},lambda rows:saved.append(len(rows)))
            self.assertEqual(len(fake.calls),275)
            self.assertEqual(saved,list(range(1,251)))
            self.assertTrue(all('stats' not in kw for _,_,kw in fake.calls))
            self.assertEqual(summarize(rows)['append_local']['base_aot']['median_ms'],1.0)
            for broken in (rows[:-1],rows+[rows[0]],rows[:-1]+[rows[0]]):
                with self.assertRaises(ValueError):summarize(broken)

    def test_full_middle_difference_rejected(self):
        with tempfile.TemporaryDirectory() as path:
            with self.assertRaises(ValueError):
                collect(Fake(Path(path),True),{c:{a:[a] for a in ARMS} for c in CASES},Path(path),{},lambda rows:None)

    def test_stats_inheritance_rejected(self):
        with self.assertRaises(ValueError):collect(None,{},None,{'RYUPY_STATS':'x'},lambda rows:None)


if __name__=='__main__':unittest.main()
