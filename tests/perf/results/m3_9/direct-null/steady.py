"""独立稳态field_reads，和原3轮短负载分表，三变体轮转。"""
import json,os,statistics,subprocess,sys,time
from pathlib import Path
b,c,n=map(Path,sys.argv[1:]);variants=[('baseline',b),('cold',c),('null',n)]
out=n/'steady';out.mkdir(exist_ok=False);report={'runs':{},'warm_calls':20,'warm_iterations_per_call':200000,'sample_iterations':2000000}
for tier in ['default','disabled']:
 env={k:v for k,v in os.environ.items() if not k.startswith(('DOTNET_Jit','COMPlus_Jit','DOTNET_Tiered','COMPlus_Tiered'))}
 if tier=='disabled':env['DOTNET_TieredCompilation']='0'
 for repeat in range(3):
  for label,root in variants[repeat:]+variants[:repeat]:
   start=time.perf_counter();x=subprocess.run(['dotnet',str(n/'steady-harness/bin/Release/net10.0/steady.dll'),str(root/'artifact/dataclass_hot.dll')],env=env,text=True,capture_output=True,check=True);wall=time.perf_counter()-start
   p=out/f'{tier}-{label}-{repeat}';p.with_suffix('.stdout').write_text(x.stdout);p.with_suffix('.stderr').write_text(x.stderr)
   samples=[]
   for line in x.stdout.splitlines():
    if line.startswith('steady '):
     _,idx,seconds,value=line.split();assert int(value)==14000000;assert int(idx)==len(samples);samples.append(float(seconds))
   assert len(samples)==7
   report['runs'].setdefault(tier+'_'+label,[]).append({'samples':samples,'median':statistics.median(samples),'process_wall_seconds':wall})
report['summary']={key:statistics.median(r['median'] for r in runs) for key,runs in report['runs'].items()}
(out/'comparison.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report['summary'],indent=2))
