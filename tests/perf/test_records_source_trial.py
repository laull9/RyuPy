"""编译守卫诊断门合成负例；不代表已执行JIT/AOT。"""
import unittest
from records_source_trial import trace


class TraceTests(unittest.TestCase):
    def test_exact_token_scope_counts_and_types(self):
        token='a'*32
        payload={'schema':1,'scope':'records-compiled-guard-diagnostic','token':token,'fast':3,'fallback':4}
        trace(payload,token,(3,4))
        for key,bad in [('token','b'*32),('scope','runtime-only'),('fast',0),('fallback',0),('fast',True)]:
            candidate=payload|{key:bad}
            with self.subTest(key=key),self.assertRaises(AssertionError):trace(candidate,token,(3,4))
        with self.assertRaises(AssertionError):trace(payload|{'extra':'stale'},token,(3,4))


if __name__=='__main__':unittest.main()
