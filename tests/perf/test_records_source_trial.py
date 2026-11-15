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




# 离线伪证据用于证明checker拒绝缺臂、重复样本和篡改工件，非执行结果。
class EvidenceTests(unittest.TestCase):
    def test_reject_incomplete_or_forged_evidence(self):
        import copy
        import hashlib
        import json
        import tempfile
        from pathlib import Path
        from check_records_source_trial import verify, CASES, ARMS, KNOWN
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory);sha='a'*40
            data=dict(ok=True,commit=sha,python='3.12.13',sdk='10.0.403',cli_sha256='a',diagnostic_cli_sha256='b',cases={},commands=[])
            def row(label,out=b'ok\n'):
                data['commands'].append(dict(label=label,exit=0,wall_ms=1.0))
                (root/(label+'.stdout')).write_bytes(out);(root/(label+'.stderr')).write_bytes(b'')
            for case,counts in CASES.items():
                oracle=b'custom-get ann buy\ncustom-set 6\ncustom-result 6 True\n' if case=='known_subclass' else b'ok\n'
                row(case+'-cp',oracle);expected=KNOWN if case=='known_subclass' else oracle
                row(case+'-control-mir',b'none');row(case+'-enabled-mir',b'RecordsTryIncrementMaterialized')
                info=dict(complete=True,cp_equivalent=case!='known_subclass',arms={},samples=[]);data['cases'][case]=info
                for arm in ARMS:
                    label=case+'-'+arm;row(label+'-build');row(label+'-execute',expected)
                    (root/label).write_bytes(b'assembly');diagnostic=arm.startswith('diagnostic-')
                    info['arms'][arm]=dict(entry_path=label,entry_sha256=hashlib.sha256(b'assembly').hexdigest(),output_matches_expected=True,diagnostic=diagnostic,trace_token='a'*32 if diagnostic else None)
                    if diagnostic:(root/(label+'-trace.json')).write_text(json.dumps(dict(schema=1,scope='records-compiled-guard-diagnostic',token='a'*32,fast=counts[0],fallback=counts[1])))
                if case=='performance':
                    order=['cp','control-jit','enabled-jit','control-aot','enabled-aot']
                    for r in range(6):
                        for arm in (order if r%2==0 else order[::-1]):
                            row(f'performance-sample-{r}-{arm}');info['samples'].append(dict(round=r,arm=arm,wall_ms=1.0))
            def check(value):
                (root/'source-trial.json').write_text(json.dumps(value));verify(root,sha)
            check(data)
            variants=[]
            x=copy.deepcopy(data);x['commands'].append(x['commands'][0]);variants.append(x)
            x=copy.deepcopy(data);x['cases']['performance']['samples'][1]=x['cases']['performance']['samples'][0];variants.append(x)
            x=copy.deepcopy(data);x['cases']['contracts']['arms'].pop('enabled-aot');variants.append(x)
            x=copy.deepcopy(data);x['cases']['known_subclass']['cp_equivalent']=True;variants.append(x)
            x=copy.deepcopy(data);x['commands'][0]['wall_ms']=float('nan');variants.append(x)
            x=copy.deepcopy(data);x['cases']['contracts']['arms']['control-aot']['entry_sha256']='wrong';variants.append(x)
            for value in variants:
                with self.assertRaises((AssertionError,KeyError)):check(value)
            (root/'contracts-enabled-aot-execute.stdout').write_bytes(b'wrong')
            with self.assertRaises(AssertionError):check(data)

if __name__=='__main__':unittest.main()
