"""区间基线分配与实际JIT；不执行正式配对计时。"""
import os,sys,json,subprocess
from pathlib import Path
p=Path(sys.argv[1]).resolve();rows=[]
for tier in ['default','disabled']:
 env={k:v for k,v in os.environ.items() if not k.startswith(('DOTNET_Jit','COMPlus_Jit','DOTNET_Tiered','COMPlus_Tiered'))}
 if tier=='disabled':env['DOTNET_TieredCompilation']='0'
 cmd=['dotnet',str(p/'harness/bin/Release/net10.0/alloc.dll'),str(p/'artifact/interval_diamond.dll')]
 for repeat in range(3):
  q=p/f'allocation-{tier}-{repeat}';r=subprocess.run(cmd,env=env,capture_output=True,text=True,check=True)
  q.with_suffix('.stdout').write_text(r.stdout);q.with_suffix('.stderr').write_text(r.stderr)
  for line in r.stdout.splitlines():
   if line.startswith('allocation '):
    _,label,batch,bytes,n,rate=line.split();rows.append({'tier':tier,'repeat':repeat,'label':label,'batch':int(batch),'bytes':int(bytes),'iterations':int(n),'bytes_per_iteration':float(rate)})
 for label,pattern in [('choose','*choose*'),('loop','*loop_diamond*')]:
  q=p/f'jit-{tier}-{label}';e=env|{'DOTNET_JitDisasm':pattern,'DOTNET_JitDisasmDiffable':'1','DOTNET_JitStdOutFile':str(q.with_suffix('.asm'))}
  r=subprocess.run(cmd,env=e,capture_output=True,text=True,check=True);q.with_suffix('.stdout').write_text(r.stdout);q.with_suffix('.stderr').write_text(r.stderr)
(p/'allocation.json').write_text(json.dumps(rows,indent=2)+'\n')
