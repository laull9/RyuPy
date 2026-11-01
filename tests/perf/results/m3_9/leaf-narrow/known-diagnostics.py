"""已知i64两种值来源、成功和身份失败的分配与实际JIT，独立于正式计时。"""
import sys,os,json,subprocess
from pathlib import Path
c=Path(sys.argv[1]).resolve();rows=[]
for tier in ['default','disabled']:
 env={k:v for k,v in os.environ.items() if not k.startswith(('DOTNET_Jit','COMPlus_Jit','DOTNET_Tiered','COMPlus_Tiered'))}
 if tier=='disabled':env['DOTNET_TieredCompilation']='0'
 for repeat in range(3):
  for label in (['baseline','candidate'] if repeat%2==0 else ['candidate','baseline']):
   q=c/f'known-allocation-{tier}-{label}-{repeat}'
   cmd=['dotnet',str(c/'known-harness/bin/Release/net10.0/alloc.dll'),str(c/f'known-{label}/known_i64.dll')]
   r=subprocess.run(cmd,env=env,capture_output=True,text=True,check=True);q.with_suffix('.stdout').write_text(r.stdout);q.with_suffix('.stderr').write_text(r.stderr)
   for line in r.stdout.splitlines():
    if line.startswith('allocation '):
     _,scenario,batch,bytes,n,rate=line.split();rows.append({'tier':tier,'variant':label,'repeat':repeat,'scenario':scenario,'batch':int(batch),'bytes':int(bytes),'iterations':int(n),'bytes_per_iteration':float(rate)})
 for label in ['baseline','candidate']:
  q=c/f'known-jit-{tier}-{label}'
  e=env|{'DOTNET_JitDisasm':'*repeat_*','DOTNET_JitDisasmDiffable':'1','DOTNET_JitStdOutFile':str(q.with_suffix('.asm'))}
  r=subprocess.run(['dotnet',str(c/'known-harness/bin/Release/net10.0/alloc.dll'),str(c/f'known-{label}/known_i64.dll')],env=e,capture_output=True,text=True,check=True)
  q.with_suffix('.stdout').write_text(r.stdout);q.with_suffix('.stderr').write_text(r.stderr)
(c/'known-allocation.json').write_text(json.dumps(rows,indent=2)+'\n')
