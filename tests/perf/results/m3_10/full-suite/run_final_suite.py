"""最终26项一次冷覆盖：两版本×两tier×每项1进程，无独立预热或PICoff扩组。"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time

p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--repo',type=Path,required=True)
p.add_argument('--run',type=Path,required=True)
p.add_argument('--old-cli',type=Path,required=True)
p.add_argument('--old-runtime',type=Path,required=True)
p.add_argument('--python',type=Path,required=True)
a=p.parse_args();repo=a.repo.resolve();root=a.run.resolve();root.mkdir(exist_ok=False)
prep=Path(__file__).resolve().parent
shutil.copytree(prep/'expected',root/'expected');shutil.copytree(prep/'benchmark-sources',root/'benchmark-sources')
shutil.copy(prep/'source-hashes.json',root/'source-hashes.json');shutil.copy(__file__,root/'run_final_suite.py')
(root/'raw').mkdir();(root/'compile').mkdir()
(root/'base-commit.txt').write_bytes(subprocess.check_output(['git','rev-parse','HEAD'],cwd=repo))
with (root/'source.patch').open('wb') as patch:
 patch.write(subprocess.check_output(['git','diff','HEAD','--','crates','runtime'],cwd=repo))
 for file in subprocess.check_output(['git','ls-files','--others','--exclude-standard','crates','runtime'],cwd=repo,text=True).splitlines():
  if file.endswith('.pending'):continue
  diff=subprocess.run(['git','diff','--no-index','--binary','/dev/null',file],cwd=repo,capture_output=True)
  assert diff.returncode in [0,1]
  patch.write(diff.stdout)

sys.path.insert(0,str(repo/'tests'))
from benchlib import measure,suite
benches=suite.discover(repo/'tests/benchmarks');assert len(benches)==26
for name,path in [('A-cli',a.old_cli),('C-cli',repo/'target/release/ryupy'),('A-runtime.dll',a.old_runtime),('C-runtime.dll',repo/'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll')]:
 shutil.copy(path,root/name)
manifest={'scope':'26项原规模冷进程单样本；非统计稳态，不做显著性结论','runs_per_item_per_tier':1,'warmups':0,'variants':['A旧M3.9整体','C最终M3.10默认整体'],'hashes':{},'compile':[],'samples':[]}
for name in ['A-cli','C-cli','A-runtime.dll','C-runtime.dll']:
 manifest['hashes'][name]=hashlib.sha256((root/name).read_bytes()).hexdigest()
def save():
 (root/'results.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
for record in json.loads((prep/'source-hashes.json').read_text()):
 source=repo/'tests/benchmarks'/(record['name']+'.py')
 assert hashlib.sha256(source.read_bytes()).hexdigest()==record['current_sha256'],'预备后source改变，必须重新核CP'
 if not record['expected_reused']:
  result=subprocess.run([str(a.python),str(source)],capture_output=True)
  (root/'expected'/(record['name']+'.stdout')).write_bytes(result.stdout)
  (root/'expected'/(record['name']+'.stderr')).write_bytes(result.stderr)
  if result.returncode:raise RuntimeError('固定13失败:'+record['name'])
for bench in benches:
 for variant in ['A','C']:
  dest=root/variant/'bench'/bench.name
  command=[str(root/(variant+'-cli')),'build',str(bench.path),'-o',str(dest)]
  start=time.perf_counter();result=subprocess.run(command,capture_output=True);elapsed=(time.perf_counter()-start)*1000
  prefix=root/'compile'/(bench.name+'-'+variant)
  prefix.with_suffix('.stdout').write_bytes(result.stdout);prefix.with_suffix('.stderr').write_bytes(result.stderr)
  row={'name':bench.name,'variant':variant,'command':command,'compile_ms':elapsed,'exit_code':result.returncode};manifest['compile'].append(row);save()
  if result.returncode:raise RuntimeError('编译失败:'+str(prefix))
  shutil.copy(root/(variant+'-runtime.dll'),dest/'RyuPy.Runtime.dll')
  manifest['hashes'][variant+'/'+bench.name]=hashlib.sha256((dest/(bench.name+'.dll')).read_bytes()).hexdigest()
for i,bench in enumerate(benches):
 expected=(root/'expected'/(bench.name+'.stdout')).read_bytes()
 for tier_index,tier in enumerate(['default','disabled']):
  env={k:v for k,v in os.environ.items() if not k.startswith(('DOTNET_Jit','COMPlus_Jit','DOTNET_Tiered','COMPlus_Tiered'))}
  for key in ['RYUPY_PIC','RYUPY_STATS','DOTNET_EnableHWIntrinsic','COMPlus_EnableHWIntrinsic']:env.pop(key,None)
  env['PYTHONHASHSEED']='0'
  if tier=='disabled':env['DOTNET_TieredCompilation']='0'
  order=['A','C'] if (i+tier_index)%2==0 else ['C','A']
  for variant in order:
   command=['dotnet',str(root/variant/'bench'/bench.name/(bench.name+'.dll'))]
   started=time.time();sample=measure.run_once(command,env,repo,'RYUPY_STATS')
   prefix=root/'raw'/f'{bench.name}-{tier}-{variant}'
   prefix.with_suffix('.stdout').write_bytes(sample.stdout);prefix.with_suffix('.stderr').write_bytes(sample.stderr)
   status='ok' if sample.exit_code==0 and sample.stdout==expected else 'run-failed' if sample.exit_code else 'mismatch'
   row={'name':bench.name,'tier':tier,'variant':variant,'command':command,'utc_epoch_start':started,'status':status,'exit_code':sample.exit_code,'wall_ms':sample.wall_ms,'maxrss':sample.maxrss,'stats':sample.stats}
   prefix.with_suffix('.json').write_text(json.dumps(row,ensure_ascii=False,indent=2)+'\n');manifest['samples'].append(row);save()
   print(bench.name,tier,variant,status,flush=True)
   if status!='ok':raise RuntimeError('首失败立即停止并报父:'+str(prefix))
assert len(manifest['samples'])==104
save();print('全部104冷进程通过；不是稳态性能结论',flush=True)
