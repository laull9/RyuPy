"""原records准入仅观察，不放宽现有matcher。"""
import argparse,json,subprocess
from pathlib import Path
p=argparse.ArgumentParser()
for key in ['default','feature','product','out']: p.add_argument('--'+key,type=Path,required=True)
a=p.parse_args();a.out.mkdir(parents=True)
source=a.product/'tests/benchmarks/records.py'; rows={}
for label,cli,flag in [('default',a.default,False),('off',a.feature,False),('on',a.feature,True)]:
 for emit in ['mir','cil']:
  command=[str(cli),'dump',str(source),'--native','--emit',emit]+(['--experimental-records-increment'] if flag else [])
  r=subprocess.run(command,cwd=a.product,capture_output=True,timeout=300)
  (a.out/f'{label}-{emit}.txt').write_bytes(r.stdout);(a.out/f'{label}-{emit}.stderr').write_bytes(r.stderr)
  (a.out/f'{label}-{emit}-command.json').write_text(json.dumps({'command':command,'exit':r.returncode}))
  assert r.returncode==0
  rows[label+'-'+emit]=r.stdout.decode()
assert 'RecordsTryIncrementMaterialized' not in rows['default-mir']+rows['off-mir']
assert rows['default-mir']==rows['off-mir'] and rows['default-cil']==rows['off-cil']
count=rows['on-mir'].count('RecordsTryIncrementMaterialized')
result={'admitted':False,'guard_count':count,'scope':'fixed 5d36 native original records; no guard relaxation'}
if count==0:
 result['reason']='原records在当前保守计划器零命中；保留三臂MIR/CIL供定位，后续语义/性能未运行'
else:
 assert count==1
 for label in ['off','on']:
  region=rows[label+'-mir'].split('fn summarize(',1)[1].split('\nfn ',1)[0]
  assert region.count('MakeTuple(')>=1 and region.count('RuntimeFn::SetItem(')==2
  assert region.count('dyn.Add(')==2 and region.count('"get",')>=2
 assert 'TryMaterialized' in rows['on-cil'] and 'TryMaterialized' not in rows['off-cil']
 result['admitted']=True
(a.out/'gate.json').write_text(json.dumps(result,ensure_ascii=False,indent=2))
