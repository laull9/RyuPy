"""无统计直接测量协议轻测：命令、平衡次序、结果拒绝、诊断分离。"""
import collections
import copy
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest

from direct_protocol import (ARMS, ROUNDS, collect_direct, diagnostic_stats,
                             direct_commands, direct_record, schedule, summary)

CASES=('hello','calls','str_concat')


class DirectProtocolTests(unittest.TestCase):
    def test_direct_commands_have_no_harness(self):
        commands=direct_commands('python','dotnet','source.py','same.dll','native')
        self.assertEqual(commands,{'cpython':['python','-B','source.py'],
                                  'jit':['dotnet','same.dll'],'aot':['native']})

    def test_each_arm_position_and_case_position_are_balanced(self):
        rows=list(schedule(CASES));self.assertEqual(len(rows),54)
        for case in CASES:
            orders=[tuple(arm for n,c,arm in rows if n==r and c==case) for r in range(ROUNDS)]
            self.assertEqual(len(set(orders)),6)
            for position in range(3):
                self.assertEqual(collections.Counter(x[position] for x in orders),dict.fromkeys(ARMS,2))
        for round_index in range(ROUNDS):
            order=list(dict.fromkeys(case for r,case,_ in rows if r==round_index))
            self.assertEqual(order,list(CASES[round_index%3:]+CASES[:round_index%3]))

    def exercise(self, temp, corrupt=None):
        calls=[];products={case:direct_commands('python','dotnet',case+'.py',case+'.dll',case) for case in CASES}
        def run(label,command,**kwargs):
            calls.append((label,command,kwargs))
            self.assertFalse(kwargs.get('stats',False));self.assertNotIn('RYUPY_STATS',kwargs['env'])
            self.assertEqual(kwargs['limit'],120)
            result={'exit':0,'stdout':b'expected','stderr':b'','case':str(Path(temp)/label),'wall_ms':1.0}
            if corrupt and label.startswith('wall-'):corrupt(result)
            return result
        expected,records=collect_direct(SimpleNamespace(run=run),products,CASES,Path(temp),{})
        return calls,expected,records

    def test_only_direct_processes_feed_wall_summary(self):
        with tempfile.TemporaryDirectory() as temp:
            calls,expected,records=self.exercise(temp)
            self.assertEqual(len(calls),63);self.assertEqual(len(records),54)
            self.assertEqual(len(expected),3)
            data=summary(records,CASES)
            self.assertEqual(data['hello']['aot']['wall_ms'],[1.0]*6)
            records[0]['round']=99
            with self.assertRaises(ValueError):summary(records,CASES)

    def test_bad_output_exit_stats_and_time_fail_instead_of_retry(self):
        for change in (dict(stdout=b'wrong'),dict(stderr=b'wrong'),dict(exit=1),
                       dict(stats_sha256='injected'),dict(wall_ms=float('nan'))):
            with self.subTest(change=change),tempfile.TemporaryDirectory() as temp:
                with self.assertRaises(ValueError):self.exercise(temp,lambda r:r.update(change))
        with self.assertRaises(ValueError):collect_direct(None,{},CASES,Path('/unused'),{'RYUPY_STATS':'x'})

    def test_runner_failure_propagates(self):
        def fail(*args,**kwargs):raise RuntimeError('timeout or child failure')
        products={case:{arm:[arm] for arm in ARMS} for case in CASES}
        with self.assertRaisesRegex(RuntimeError,'timeout'):
            collect_direct(SimpleNamespace(run=fail),products,CASES,Path('/unused'),{})

    def test_stats_file_contamination_and_diagnostic_validation(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);stats=root/'stats.json';stats.write_text('{}')
            with self.assertRaises(ValueError):direct_record({'case':temp,'wall_ms':1},'hello','aot',0)
            stats.write_text(json.dumps({'main_ms':1,'gc0':0,'gc1':0,'gc2':0}))
            self.assertEqual(diagnostic_stats(stats,'cpython')['main_ms'],1)
            for bad in ('{}','{"main_ms":NaN,"gc0":0,"gc1":0,"gc2":0}',
                        '{"main_ms":1,"main_ms":2,"gc0":0,"gc1":0,"gc2":0}'):
                stats.write_text(bad)
                with self.assertRaises(ValueError):diagnostic_stats(stats,'cpython')


if __name__=='__main__':unittest.main()
