import os,json,statistics,subprocess,pathlib
root=pathlib.Path('/tmp/ryupy-m3_7-dataclass-regression')
variants={'m3_6_saved':pathlib.Path('/tmp/ryupy-m3_6-dataclass-accumulator'),'m3_7':root,'m3_6_body_m3_7_runtime':pathlib.Path('/tmp/ryupy-m3_7-dataclass-old-body')}
report={'comparison':'saved M3.6 compiled workload+runtime versus current compiled workload+runtime; same host/time, not compiler-isolated','runs':{}}
expected={'construction':1249975000,'field_reads':1400000,'equality':50000}
for tier in ('default','disabled'):
 env=dict(os.environ); env.pop('DOTNET_TieredCompilation',None)
 if tier=='disabled':env['DOTNET_TieredCompilation']='0'
 for repeat in range(3):
  for name in (list(variants) if repeat%2==0 else list(reversed(variants))):
   output=subprocess.check_output(['dotnet',str(variants[name]/'dataclass_hot.dll')],env=env,text=True)
   (root/f'{tier}-{name}-{repeat}.txt').write_text(output)
   samples={k:[] for k in expected}; semantics=None
   for line in output.splitlines():
    if line.startswith('semantics '):
     semantics=line; assert line=='semantics True Record(x=3, y=4)',line
    else:
     label,elapsed,value=line.split();assert int(value)==expected[label],line;samples[label].append(float(elapsed))
   assert semantics and all(len(v)==7 for v in samples.values())
   report['runs'].setdefault(tier+'_'+name,[]).append({'samples':samples,'medians':{k:statistics.median(v) for k,v in samples.items()},'semantics':semantics})
report['summary']={key:{label:statistics.median(run['medians'][label] for run in runs) for label in expected} for key,runs in report['runs'].items()}
(root/'comparison.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report['summary'],indent=2))
