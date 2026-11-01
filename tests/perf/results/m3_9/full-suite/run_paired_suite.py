"""复用已编DLL的整26逐项轮转；无重编/删项，main仍含该进程JIT。"""
from pathlib import Path
import argparse,json,os,statistics,sys
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--root',type=Path,required=True);p.add_argument('--run',type=Path,required=True)
a=p.parse_args(); out=a.run/'paired-suite';out.mkdir(exist_ok=False)
sys.path.insert(0,str(a.root/'tests'))
from benchlib import measure,suite
benches=suite.discover(a.root/'tests/benchmarks');expected={}
for f in sorted((a.run/'current-default/raw').glob('process-*.json')):
 r=json.loads(f.read_text())
 if r['stats_env'] is None:
  name=Path(r['command'][-1]).stem;value=f.with_suffix('.stdout').read_bytes()
  if name in expected:assert expected[name]==value
  expected[name]=value
report={'scope':'原26原规模，预编译DLL逐项轮转；非稳态，无额外compile','warmup':1,'runs':3,'runs_raw':[]}
for bench_index, bench in enumerate(benches):
 assert bench.name in expected
 for tier in ['default','disabled']:
  baseenv={k:v for k,v in os.environ.items() if not k.startswith(('DOTNET_Jit','COMPlus_Jit','DOTNET_Tiered','COMPlus_Tiered'))}
  for key in ['RYUPY_PIC','RYUPY_STATS','PYTHONHASHSEED']:baseenv.pop(key,None)
  baseenv['PYTHONHASHSEED']='0'
  if tier=='disabled':baseenv['DOTNET_TieredCompilation']='0'
  for variant,extra in [('default',{})]+[(v.label,v.env) for v in bench.variants]:
   env={**baseenv,**extra};label='pic-off' if extra.get('RYUPY_PIC')=='0' else 'default'
   for iteration in range(4):
    order=['baseline','candidate'] if (iteration+bench_index)%2==0 else ['candidate','baseline']
    for which in order:
     folder='e1-'+tier if which=='baseline' else 'current-'+tier
     dll=a.run/folder/'bench'/bench.name/(bench.name+'.dll')
     if not dll.exists():raise RuntimeError('不隐去缺失产物:'+str(dll))
     command=['dotnet',str(dll)]
     sample=measure.run_once(command,env,a.root,'RYUPY_STATS')
     prefix=out/f'{bench.name}-{tier}-{label}-{iteration}-{which}'
     prefix.with_suffix('.stdout').write_bytes(sample.stdout);prefix.with_suffix('.stderr').write_bytes(sample.stderr)
     status='ok' if sample.exit_code==0 and sample.stdout==expected[bench.name] else 'run-failed' if sample.exit_code else 'mismatch'
     row={'name':bench.name,'tier':tier,'variant':variant,'which':which,'round':iteration,'recorded':iteration>0,'command':command,'exit_code':sample.exit_code,'status':status,'wall_ms':sample.wall_ms,'maxrss':sample.maxrss,'stats':sample.stats}
     prefix.with_suffix('.json').write_text(json.dumps(row,ensure_ascii=False,indent=2)+'\n');report['runs_raw'].append(row)
     (out/'results.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
     if status!='ok':
      print('FAILED',prefix,status,flush=True);raise SystemExit(1)
   print(bench.name,tier,variant,'passed',flush=True)
summary={}
for row in report['runs_raw']:
 if row['recorded']:
  key='|'.join(row[x] for x in ['name','tier','variant','which']);summary.setdefault(key,[]).append(row)
report['summary']={}
for key,rows in summary.items():
 values={x:statistics.median(r[x] for r in rows) for x in ['wall_ms','maxrss']}
 for field in sorted(set().union(*(r['stats'].keys() for r in rows))):
  v=[r['stats'][field] for r in rows if isinstance(r['stats'].get(field),(int,float))]
  if v:values[field]=statistics.median(v)
 report['summary'][key]=values
(out/'results.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
