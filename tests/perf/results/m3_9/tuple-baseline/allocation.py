"""与正式计时分离的tuple分配/JIT诊断；在产物根目录运行。"""
from pathlib import Path
import os,sys,subprocess,json
p=Path(sys.argv[1]).resolve();rows=[]
for tier in ['default','disabled']:
 env={k:v for k,v in os.environ.items() if not k.startswith(('DOTNET_Jit','COMPlus_Jit','DOTNET_Tiered','COMPlus_Tiered'))}
 if tier=='disabled':env['DOTNET_TieredCompilation']='0'
 for repeat in range(3):
  q=p/f'allocation-{tier}-{repeat}'
  r=subprocess.run(['dotnet',str(p/'harness/bin/Release/net10.0/alloc.dll'),str(p/'artifact/tuple_scalar.dll')],env=env,capture_output=True,text=True,check=True)
  q.with_suffix('.stdout').write_text(r.stdout);q.with_suffix('.stderr').write_text(r.stderr)
  for line in r.stdout.splitlines():
   if line.startswith('allocation '):
    _,label,batch,bytes,n,rate=line.split();rows.append({'tier':tier,'repeat':repeat,'label':label,'batch':int(batch),'bytes':int(bytes),'iterations':int(n),'bytes_per_iteration':float(rate)})
 env|={'DOTNET_JitDisasm':'*tuple_*','DOTNET_JitDisasmDiffable':'1','DOTNET_JitStdOutFile':str(p/f'jit-{tier}.asm')}
 r=subprocess.run(['dotnet',str(p/'harness/bin/Release/net10.0/alloc.dll'),str(p/'artifact/tuple_scalar.dll')],env=env,capture_output=True,text=True,check=True)
 (p/f'jit-{tier}.stdout').write_text(r.stdout);(p/f'jit-{tier}.stderr').write_text(r.stderr)
(p/'allocation.json').write_text(json.dumps(rows,indent=2)+'\n')
