"""独立核原始stdout/MIR/trace与完整新进程样本，不以runner的ok声明为证。"""
import argparse
import hashlib
import json
import math
from pathlib import Path

CASES={'contracts':(3,4),'callbacks':(0,3),'known_subclass':(0,1),'performance':(200000,0)}
SOURCE_ROOT=Path(__file__).resolve().parents[2]
ARMS=[f'{a}-{b}' for a in ('control','enabled','diagnostic') for b in ('jit','aot')]
KNOWN=b'custom-get ann buy\ncustom-set 6\nknown-rejection dict layout with non-scalar keys is not supported 0\n'


def verify(root,sha):
    data=json.loads((root/'source-trial.json').read_text())
    assert data['ok'] is True and data['commit']==sha
    assert data['python']=='3.12.13' and data['sdk']=='10.0.403'
    assert data['cli_sha256']!=data['diagnostic_cli_sha256']
    assert set(data['cases'])==set(CASES)
    commands=data['commands'];labels=[r['label'] for r in commands]
    assert len(set(labels))==len(labels)
    rows={r['label']:r for r in commands};expected_labels=set()
    def output(label):
        expected_labels.add(label);r=rows[label]
        assert r['exit']==0 and 'error' not in r
        assert type(r['wall_ms']) in (int,float) and math.isfinite(r['wall_ms']) and r['wall_ms']>0
        return (root/(label+'.stdout')).read_bytes(),(root/(label+'.stderr')).read_bytes()
    for case,counts in CASES.items():
        info=data['cases'][case];assert info['complete'] is True
        fixture=SOURCE_ROOT/'tests/fixtures/records_guard_source'/f'{case}.py'
        assert info['source_sha256']==hashlib.sha256(fixture.read_bytes()).hexdigest()
        assert info['cp_equivalent'] is (case!='known_subclass')
        oracle,err=output(case+'-cp');assert not err
        if case=='known_subclass':assert oracle==b'custom-get ann buy\ncustom-set 6\ncustom-result 6 True\n'
        expected=KNOWN if case=='known_subclass' else oracle
        assert expected
        for enabled in (False,True):
            mir,_=output(case+('-enabled' if enabled else '-control')+'-mir')
            assert mir.count(b'RecordsTryIncrementMaterialized')==int(enabled)
        assert set(info['arms'])==set(ARMS)
        for arm in ARMS:
            label=case+'-'+arm;output(label+'-build')
            assert output(label+'-execute')==(expected,b'')
            item=info['arms'][arm];entry=(root/item['entry_path']).resolve()
            assert entry.is_relative_to(root.resolve()) and entry.is_file()
            assert hashlib.sha256(entry.read_bytes()).hexdigest()==item['entry_sha256']
            diagnostic=arm.startswith('diagnostic-')
            assert item['runtime_audits']
            arm_root=(root/label).resolve()
            assert arm_root.is_relative_to(root.resolve()) and arm_root.is_dir()
            actual_dlls=sorted(str(p.relative_to(root)) for p in (root/label).rglob('RyuPy.Runtime.dll'))
            paths=[a['path'] for a in item['runtime_audits']]
            assert paths==actual_dlls and len(set(paths))==len(paths)
            assert [a['label'] for a in item['runtime_audits']]==[f'{label}-runtime-audit-{i}' for i in range(len(paths))]
            for audit in item['runtime_audits']:
                payload,stderr=output(audit['label']);assert not stderr
                metadata=json.loads(payload);dll=(root/audit['path']).resolve()
                assert dll.is_relative_to(arm_root) and dll.name=='RyuPy.Runtime.dll' and dll.is_file()
                assert metadata['sha256']==audit['sha256']==hashlib.sha256(dll.read_bytes()).hexdigest()
                assert metadata['mode']==('diagnostic' if diagnostic else 'normal')
                for name in ('DiagnosticsReady','_diagnosticFast','_diagnosticFallback'):
                    assert (name in metadata['fields']) is diagnostic
                for name in ('RegisterDiagnostics','Observe'):
                    assert (name in metadata['methods']) is diagnostic
                if not diagnostic:assert set(metadata['methods']) <= {'.cctor','TryMaterialized'}
            assert item['diagnostic'] is diagnostic and item['output_matches_expected'] is True
            if diagnostic:
                token=item['trace_token'];assert len(token)==32 and all(c in '0123456789abcdef' for c in token)
                trace=json.loads((root/(label+'-trace.json')).read_text())
                assert trace==dict(schema=1,scope='records-compiled-guard-diagnostic',token=token,fast=counts[0],fallback=counts[1])
                assert type(trace['fast']) is int and type(trace['fallback']) is int
            else:
                assert item['trace_token'] is None
                assert not (root/(label+'-trace.json')).exists()
        samples=info['samples']
        if case!='performance':assert samples==[];continue
        order=['cp','control-jit','enabled-jit','control-aot','enabled-aot']
        sequence=[(r,a) for r in range(6) for a in (order if r%2==0 else order[::-1])]
        assert [(s['round'],s['arm']) for s in samples]==sequence
        for sample in samples:
            label=f"performance-sample-{sample['round']}-{sample['arm']}"
            assert output(label)==(expected,b'')
            assert sample['wall_ms']==rows[label]['wall_ms']
    assert set(labels)==expected_labels
    return data


if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('root',type=Path);p.add_argument('--expected-sha',required=True)
    a=p.parse_args();verify(a.root,a.expected_sha);print('PASS exact compiled-source evidence; known incompatibility separate; cold wall only')
