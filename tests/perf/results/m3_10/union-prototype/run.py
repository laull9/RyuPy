"""独立实验：同一已构建Release原型，每tier三个新进程，每场景七轮；不触碰生产编译器。"""
import json,os,pathlib,statistics,subprocess
root=pathlib.Path(__file__).resolve().parent
report={}
for tier in ('default','disabled'):
 env=dict(os.environ);env.pop('DOTNET_TieredCompilation',None)
 if tier=='disabled':env['DOTNET_TieredCompilation']='0'
 for repeat in range(3):
  output=subprocess.check_output(['dotnet',str(root/'out/union.dll')],env=env,text=True)
  (root/f'{tier}-{repeat}.jsonl').write_text(output)
  samples={}
  for line in output.splitlines():
   row=json.loads(line)
   if 'check' in row:
    assert row['check']=='pass' and row['size16']==16 and row['size24']==24
    continue
   samples.setdefault(row['name'],[]).append(row)
  assert all(len(rows)==7 for rows in samples.values())
  report.setdefault(tier,[]).append(samples)
summary={tier:{name:{'seconds':statistics.median(statistics.median(v['seconds'] for v in run[name]) for run in runs),
                    'bytes':statistics.median(v['bytes'] for run in runs for v in run[name]),
                    'process_medians':[statistics.median(v['seconds'] for v in run[name]) for run in runs]}
               for name in runs[0]} for tier,runs in report.items()}
(root/'results.json').write_text(json.dumps({'samples':report,'summary':summary},indent=2)+'\n')
print(json.dumps(summary,indent=2))
