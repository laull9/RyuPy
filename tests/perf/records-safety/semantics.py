"""实际CLI源码CP/JIT/AOT三臂及独立诊断；仅冷新进程配对wall，不宣称默认或暖态收益。"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import secrets
import subprocess
import sys
import time

ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tests'))
from benchlib.artifact import verified_native, MANIFEST_NAME
from benchlib.suite import Benchmark

CASES={'contracts':(3,4),'callbacks':(0,3),'known_callbacks':(0,3),'known_subclass':(0,1),'performance':(200000,0),'wide':(314,11),'capture':(0,0)}
KNOWN=b'custom-get ann buy\ncustom-set 6\nknown-rejection dict layout with non-scalar keys is not supported 0\n'

KNOWN_CALLBACKS=b'key-hash\nkey-hash\nkey-eq\nkey-hash\nkey-eq\nkey-value [2]\nreceiver-get a 0\nreceiver-set a 6\n'
CP_CALLBACKS=b'key-hash\nkey-hash\nkey-hash\nkey-eq\nkey-hash\nkey-eq\nkey-value [2]\nreceiver-get a 0\nreceiver-set a 6\n'
# 43816599556 已在固定默认 JIT 复现；本轮仍逐次要求默认 AOT 与实验两态重现，不能据此称 CP 兼容。
CP_CAPTURE=b'old-get x 0\nset x 9\ncaptured 2 9\nhash-error\nunchanged hash failed 5 1\n'
KNOWN_CAPTURE=b'new-get x 0\nset x 101\ncaptured 2 101\nhash-error\nunchanged hash failed 5 1\n'
BASELINE_SHA='5d36db1e952f1db07aee6563d7a800cc1df94548'


def trace(payload,token,counts):
    assert payload=={'schema':1,'scope':'records-compiled-guard-diagnostic','token':token,'fast':counts[0],'fallback':counts[1]}
    assert type(payload['fast']) is int and type(payload['fallback']) is int


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    for name in ('cli','diagnostic-cli','baseline-cli','baseline-repo','audit','output'):parser.add_argument('--'+name,type=Path,required=True)
    parser.add_argument('--expected-sha',required=True)
    args=parser.parse_args();out=args.output.resolve()
    assert not out.is_relative_to(ROOT) and not out.exists()
    assert sys.version_info[:3]==(3,12,13)
    assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()==args.expected_sha
    assert subprocess.check_output(['dotnet','--version'],cwd=ROOT,text=True).strip()=='10.0.403'
    assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=args.baseline_repo,text=True).strip()==BASELINE_SHA
    out.mkdir(parents=True);normal=args.cli.resolve();diagnostic=args.diagnostic_cli.resolve()
    sha=lambda path:hashlib.sha256(path.read_bytes()).hexdigest()
    assert sha(normal)!=sha(diagnostic)
    env=os.environ.copy()
    for key in ('RYUPY_RUNTIME_DIR','RYUPY_STATS','RYUPY_RECORDS_GUARD_TRACE','RYUPY_RECORDS_GUARD_TOKEN'):env.pop(key,None)
    report={'ok':False,'commit':args.expected_sha,'python':'3.12.13','sdk':'10.0.403',
            'cli_sha256':sha(normal),'diagnostic_cli_sha256':sha(diagnostic),'cases':{},'commands':[],
            'baseline_commit':BASELINE_SHA,'baseline_cli_sha256':sha(args.baseline_cli),
            'scope':'explicit-feature-source-trial; new-process wall includes startup and IO; shared runner; not warm throughput'}
    def run(label,command,extra=None,timeout=1200):
        started=time.perf_counter();row={'label':label,'command':[str(x) for x in command]};report['commands'].append(row)
        try:
            result=subprocess.run(row['command'],cwd=ROOT,env=env| (extra or {}),capture_output=True,timeout=timeout)
            row.update(exit=result.returncode,wall_ms=(time.perf_counter()-started)*1000)
            (out/(label+'.stdout')).write_bytes(result.stdout);(out/(label+'.stderr')).write_bytes(result.stderr)
            assert result.returncode==0,(label,result.returncode,result.stderr[-3000:])
            return result.stdout,result.stderr,row['wall_ms']
        except subprocess.TimeoutExpired as error:
            row['error']='timeout';row['wall_ms']=(time.perf_counter()-started)*1000
            (out/(label+'.stdout')).write_bytes(error.stdout or b'')
            (out/(label+'.stderr')).write_bytes(error.stderr or b'')
            raise
        except Exception as error:
            row['error']=str(error);raise
    try:
        for case,counts in CASES.items():
            source=(Path(__file__).with_name(case+'.py') if case in ('wide','capture') else ROOT/'tests/fixtures/records_guard_source'/f'{case}.py')
            oracle=run(case+'-cp',[sys.executable,'-B',source]);assert not oracle[1]
            expected=KNOWN if case=='known_subclass' else KNOWN_CALLBACKS if case=='known_callbacks' else KNOWN_CAPTURE if case=='capture' else oracle[0]
            if case=='known_subclass':assert oracle[0]==b'custom-get ann buy\ncustom-set 6\ncustom-result 6 True\n'
            info={'source_sha256':sha(source),'cp_equivalent':case not in ('known_subclass','known_callbacks','capture'),'arms':{},'samples':[]};report['cases'][case]=info
            if case=='capture':
                assert oracle[0]==CP_CAPTURE and expected!=oracle[0]
                info['known_difference']='ordinary-instance callable lookup occurs after argument callback; CP captures old-get, fixed dev resolves new-get; not CP-compatible'
                info['historical_failure_run']=43816599556
            if case=='known_callbacks':
                assert oracle[0]==CP_CALLBACKS and expected!=oracle[0]
            if True:
                info['baseline']={}
                for backend in ('jit','aot'):
                    label=f'{case}-baseline-{backend}';directory=out/label;directory.mkdir()
                    command=[args.baseline_cli,'build',source,'-o',directory,'--native']
                    if backend=='aot':command.append('--aot')
                    build=run(label+'-build',command)
                    entry=verified_native(directory,Benchmark(case,source),build[1].decode(),mode='standalone') if backend=='aot' else directory/(case+'.dll')
                    command=[entry] if backend=='aot' else [os.environ['DOTNET_HOST_PATH'],entry]
                    actual=run(label+'-execute',command)
                    assert actual[:2]==(expected,b''), (label,'historical difference not proven')
                    info['baseline'][backend]={'entry_path':str(entry.relative_to(out)),'entry_sha256':sha(entry)}
            for enabled in (False,True):
                label=case+('-enabled' if enabled else '-control')
                command=[normal,'dump',source,'--emit','mir','--native']+(['--experimental-records-increment'] if enabled else [])
                mir=run(label+'-mir',command)[0].decode()
                assert mir.count('RecordsTryIncrementMaterialized')==(int(enabled) if case!='capture' else 0)
            commands={}
            for arm,cli,enabled in [('control',normal,False),('enabled',normal,True),('diagnostic',diagnostic,True)]:
                for backend in ('jit','aot'):
                    label=f'{case}-{arm}-{backend}';directory=out/label;directory.mkdir()
                    command=[cli,'build',source,'-o',directory,'--native']
                    if enabled:command.append('--experimental-records-increment')
                    if backend=='aot':command.append('--aot')
                    build=run(label+'-build',command)
                    runtime_dlls=sorted(directory.rglob('RyuPy.Runtime.dll'))
                    assert runtime_dlls, (label,'no actual Runtime PE retained for audit')
                    audits=[]
                    for index,dll in enumerate(runtime_dlls):
                        audit_label=f'{label}-runtime-audit-{index}'
                        audit=run(audit_label,[os.environ['DOTNET_HOST_PATH'],args.audit.resolve(),dll,'diagnostic' if arm=='diagnostic' else 'normal'])
                        payload=json.loads(audit[0]);assert payload['sha256']==sha(dll)
                        audits.append({'label':audit_label,'path':str(dll.relative_to(out)),'sha256':sha(dll)})
                    if backend=='aot':
                        entry=verified_native(directory,Benchmark(case,source),build[1].decode(),mode='standalone')
                        manifest=json.loads((entry.parent/MANIFEST_NAME).read_text())
                        assert manifest['toolchain']['sdk_version']=='10.0.403'
                        assert manifest['compiler']['options']['experimental_records_increment'] is enabled
                        command=[entry]
                    else:
                        entry=directory/(case+'.dll');assert entry.is_file();command=[os.environ['DOTNET_HOST_PATH'],entry]
                    extra={};trace_path=out/(label+'-trace.json');token=secrets.token_hex(16)
                    extra={'RYUPY_RECORDS_GUARD_TRACE':str(trace_path),'RYUPY_RECORDS_GUARD_TOKEN':token}
                    actual=run(label+'-execute',command,extra)
                    assert actual[:2]==(expected,b''),(label,'output mismatch')
                    if arm=='diagnostic' and case!='capture':trace(json.loads(trace_path.read_text()),token,counts)
                    elif arm=='diagnostic' and trace_path.exists():trace(json.loads(trace_path.read_text()),token,(0,0))
                    else:assert not trace_path.exists(), 'normal/performance artifact unexpectedly contains diagnostics'
                    info['arms'][arm+'-'+backend]={'entry_sha256':sha(entry),'output_matches_expected':True,'diagnostic':arm=='diagnostic',
                        'runtime_audits':audits, 'entry_path':str(entry.relative_to(out)), 'trace_token':token if arm=='diagnostic' else None}
                    if arm!='diagnostic':commands[arm+'-'+backend]=command
            if case=='performance':
                commands['cp']=[sys.executable,'-B',source]
                order=['cp','control-jit','enabled-jit','control-aot','enabled-aot']
                for round_ in range(6):
                    for arm in (order if round_%2==0 else list(reversed(order))):
                        result=run(f'performance-sample-{round_}-{arm}',commands[arm])
                        assert result[:2]==(expected,b'')
                        info['samples'].append({'round':round_,'arm':arm,'wall_ms':result[2]})
                assert len(info['samples'])==30
            info['complete']=True
        report['ok']=True
    finally:(out/'source-trial.json').write_text(json.dumps(report,ensure_ascii=False,indent=2))


if __name__=='__main__':main()
