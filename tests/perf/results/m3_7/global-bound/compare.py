import os,subprocess,json,statistics,pathlib
root=pathlib.Path(__file__).resolve().parent
report={}
for tier in ('default','disabled'):
 env=dict(os.environ);env.pop('DOTNET_TieredCompilation',None)
 if tier=='disabled':env['DOTNET_TieredCompilation']='0'
 for repeat in range(3):
  for side in (('before','after') if repeat%2==0 else ('after','before')):
   for variant,file in (('read','global_read'),('dynamic','global_bound')):
    directory=side+'-read' if variant=='read' else side
    output=subprocess.check_output(['dotnet',str(root/'harness/global-diagnostics.dll'),str(root/directory/(file+'.dll'))],text=True,env=env)
    (root/f'{tier}-{side}-{variant}-{repeat}.txt').write_text(output)
    samples={}
    for line in output.splitlines():
     name,seconds,result,alloc=line.split();samples.setdefault(name,[]).append({'seconds':float(seconds),'result':int(result),'allocated':int(alloc.split('=')[1])})
    assert all(len(v)==7 for v in samples.values())
    report.setdefault(tier+'-'+side+'-'+variant,[]).append(samples)
summary={k:{name:statistics.median(statistics.median(x['seconds'] for x in run[name]) for run in runs) for name in runs[0]} for k,runs in report.items()}
(root/'comparison.json').write_text(json.dumps({'samples':report,'summary':summary},indent=2)+'\n');print(json.dumps(summary,indent=2))
