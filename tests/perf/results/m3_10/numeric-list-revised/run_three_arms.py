"""一次三臂验收；资源、时间与JIT进程分离，逐次保存证据，不重采失败样本。"""
import argparse
import json
import os
from pathlib import Path
import subprocess
import time

p=argparse.ArgumentParser(description=__doc__)
p.add_argument('root',type=Path)
p.add_argument('phase',choices=['resources','time','jit'])
a=p.parse_args(); root=a.root.resolve(); out=root/a.phase; out.mkdir(exist_ok=False)
expected=(root/'validation/cpython-diagnostic.stdout').read_text()
matmul=(root/'validation/A-matmul.stdout').read_text()
records=[]
for tier in ['default','disabled']:
 env={k:v for k,v in os.environ.items() if not k.startswith(('DOTNET_Jit','COMPlus_Jit','DOTNET_Tiered','COMPlus_Tiered'))}
 for key in ['RYUPY_STATS','RYUPY_PIC']: env.pop(key,None)
 if tier=='disabled': env['DOTNET_TieredCompilation']='0'
 for repeat in range(3 if a.phase=='time' else 1):
  order=['A','B','C']; order=order[repeat:]+order[:repeat]
  for variant in order:
   for scope in ['micro','matmul']:
    folder=root/variant
    if scope=='micro':
     command=['dotnet',str(folder/'micro/numeric_list.dll')] if a.phase=='time' else ['dotnet',str(folder/'micro-harness/bin/Release/net10.0/measure.dll'),str(folder/'micro/numeric_list.dll')]
    else:
     command=['dotnet',str(folder/'core-harness/bin/Release/net10.0/measure.dll'),str(folder/'bench/matmul/matmul.dll'),'matmul',str(root/'validation/A-matmul.stdout'),'time' if a.phase=='time' else 'resources','3','7' if a.phase=='time' else '3']
    stem=f'{scope}-{tier}-{variant}-{repeat}'; process_env=dict(env)
    if a.phase=='jit':
     process_env.update({'DOTNET_JitDisasm':'m0_produce_consume m0_construct_refs m0_append_refs m0_read_refs GetItem ReadSlot AppendValue MaterializeAll ToIndex' if scope=='micro' else 'm0_matmul CanReadF64 ReadF64 PeekList AppendF64','DOTNET_JitStdOutFile':str(out/(stem+'.asm')),'DOTNET_JitDisasmDiffable':'1'})
    begin=time.time(); start=time.perf_counter()
    r=subprocess.run(command,env=process_env,text=True,capture_output=True)
    wall=time.perf_counter()-start
    (out/(stem+'.stdout')).write_text(r.stdout); (out/(stem+'.stderr')).write_text(r.stderr)
    record={'scope':scope,'tier':tier,'variant':variant,'repeat':repeat,'phase':a.phase,'command':command,'utc_epoch_start':begin,'wall_seconds':wall,'exit_code':r.returncode}
    (out/(stem+'.json')).write_text(json.dumps(record,indent=2)+'\n')
    records.append(record); (out/'runs.json').write_text(json.dumps(records,indent=2)+'\n')
    if r.returncode: raise RuntimeError(stem+' failed')
    if scope=='micro':
     if a.phase=='time':
      semantics='\n'.join(line for line in r.stdout.splitlines() if line.startswith('semantics '))+'\n'
      assert semantics==expected,stem
      samples=[line.split() for line in r.stdout.splitlines() if line.startswith('sample ')]
      assert len(samples)==49 and all(row[-1]=='True' for row in samples),stem
     else:
      assert r.stdout.startswith(expected),stem
      rows=[json.loads(line) for line in r.stdout[len(expected):].splitlines()]
      assert len(rows)==21,stem
    else:
     assert r.stdout.startswith(matmul),stem
     rows=[json.loads(line) for line in r.stdout[len(matmul):].splitlines()]
     assert len(rows)==(7 if a.phase=='time' else 3),stem
    print(stem,'PASS',round(wall,3),flush=True)
