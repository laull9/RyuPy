"""组合源与list所有权合同的真实CP/JIT/AOT和搬迁；保留原始双流与完整产物。"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'tests'))
from benchlib.artifact import verified_native, MANIFEST_NAME
from benchlib.suite import Benchmark


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    for key in ('off','on','output'):parser.add_argument('--'+key,type=Path,required=True)
    args=parser.parse_args();out=args.output.resolve()
    assert not out.exists() and not out.is_relative_to(ROOT);out.mkdir(parents=True)
    assert sys.version_info[:3]==(3,12,13)
    assert subprocess.check_output(['dotnet','--version'],text=True).strip()=='10.0.403'
    head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
    env=os.environ.copy()
    for key in ('RYUPY_RUNTIME_DIR','RYUPY_RECORDS_GUARD_TRACE','RYUPY_RECORDS_GUARD_TOKEN','RYUPY_STATS'):env.pop(key,None)
    report={'head':head,'ok':False,'commands':[],'arms':[],'inputs':[],'cli':{}}
    sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
    def save(): (out/'report.json').write_text(json.dumps(report,indent=2))
    def run(label,command,cwd=ROOT,environment=None):
        command=[str(x) for x in command];record={'label':label,'command':command,'cwd':str(cwd)}
        report['commands'].append(record);save()
        try:
            r=subprocess.run(command,cwd=cwd,env=env if environment is None else environment,capture_output=True,timeout=1800)
            (out/(label+'.stdout')).write_bytes(r.stdout);(out/(label+'.stderr')).write_bytes(r.stderr)
            record['exit']=r.returncode;save();assert r.returncode==0,(label,r.returncode)
            return r.returncode,r.stdout,r.stderr
        except subprocess.TimeoutExpired as error:
            (out/(label+'.stdout')).write_bytes(error.stdout or b'');(out/(label+'.stderr')).write_bytes(error.stderr or b'')
            record['error']='timeout';save();raise
    try:
        for arm in ('off','on'):
            cli=getattr(args,arm).resolve();report['cli'][arm]={'path':str(cli),'sha256':sha(cli)}
        for case,relative in [('mixed','tests/fixtures/integrated_optimization/mixed.py'),('slice','tests/e2e/phase2/m2_list_slice_owned_result.py')]:
            source=ROOT/relative;data=source.read_bytes()
            assert data==subprocess.check_output(['git','show',head+':'+relative],cwd=ROOT)
            (out/(case+'-input.py')).write_bytes(data)
            report['inputs'].append({'source':relative,'sha256':sha(source)})
            oracle=run(case+'-cp',[sys.executable,'-B',source]);save()
            for arm in ('off','on'):
                cli=getattr(args,arm).resolve();flags=['--experimental-records-increment'] if arm=='on' else []
                mir=run(case+'-'+arm+'-mir',[cli,'dump',source,'--emit','mir',*flags])[1].decode()
                assert mir.count('RecordsTryIncrementMaterialized')==(1 if case=='mixed' and arm=='on' else 0)
                for backend in ('jit','aot'):
                    label=case+'-'+arm+'-'+backend;directory=out/label;directory.mkdir()
                    command=[cli,'build',source,'-o',directory,'--native',*flags]
                    if backend=='aot':command.append('--aot')
                    built=run(label+'-build',command)
                    assert sha(source)==hashlib.sha256(data).hexdigest() and sha(cli)==report['cli'][arm]['sha256']
                    if backend=='aot':
                        entry=verified_native(directory,Benchmark(source.stem,source),built[2].decode(),mode='standalone')
                        manifest=json.loads((entry.parent/MANIFEST_NAME).read_text())
                        assert manifest['toolchain']['sdk_version']=='10.0.403'
                        assert manifest['compiler']['options']['experimental_records_increment']==(arm=='on')
                        batch=[r for r in manifest['emitted_delegate_references'] if r['role']=='generator_sum_resume']
                        assert len(batch)==(1 if case=='mixed' and arm=='on' else 0)
                        command=[entry]
                    else:entry=directory/(source.stem+'.dll');command=['dotnet',entry]
                    assert run(label+'-execute',command)==oracle
                    row={'label':label,'entry':str(entry.relative_to(out)),'sha256':sha(entry)};report['arms'].append(row);save()
                    if backend=='aot':
                        moved=directory/'中文 空格搬迁';shutil.copytree(entry.parent,moved)
                        original=entry.parent;original.rename(original.with_name(original.name+'.hidden'))
                        assert run(label+'-moved',[moved/entry.name],moved,env|{'PATH':'/nonexistent'})==oracle
                        assert sha(moved/entry.name)==row['sha256']
                        row['relocated']=str((moved/entry.name).relative_to(out));save()
        assert len(report['arms'])==8;report['ok']=True
    finally:save()

if __name__=='__main__':main()
