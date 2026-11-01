"""正式计时结束后独立采真实四kernel JIT与MIR/CIL；绝不与计时进程混跑。"""
from pathlib import Path
import json,os,subprocess,sys,hashlib
p=Path(sys.argv[1]);repo=Path('/workspace/shared/RyuPy');out=p/'core-jit';out.mkdir(exist_ok=False)
patterns={'fib':'*m0_fib*','mandelbrot':'*m0_mandel*','matmul':'*m0_matmul*','nbody':'*m0_advance*'};records=[]
for which,cli,folder in [('baseline','ryupy-e1-debug','e1-default'),('candidate','ryupy-current','current-default')]:
 for name,pattern in patterns.items():
  source=repo/'tests/benchmarks'/f'{name}.py'
  for emit in ['mir','cil']:
   r=subprocess.run([str(p/cli),'dump',str(source),'--emit',emit],cwd=repo,capture_output=True)
   (out/f'{which}-{name}.{emit}').write_bytes(r.stdout);(out/f'{which}-{name}.{emit}.stderr').write_bytes(r.stderr)
   if r.returncode:raise SystemExit('dump失败，不继续伪造JIT证据')
  for tier in ['default','disabled']:
   env={k:v for k,v in os.environ.items() if not k.startswith(('DOTNET_Jit','COMPlus_Jit','DOTNET_Tiered','COMPlus_Tiered'))}
   for key in ['RYUPY_STATS','RYUPY_PIC']:env.pop(key,None)
   if tier=='disabled':env['DOTNET_TieredCompilation']='0'
   stem=f'{which}-{name}-{tier}'; asm=out/(stem+'.asm')
   env.update(DOTNET_JitDisasm=pattern,DOTNET_JitDisasmDiffable='1',DOTNET_JitStdOutFile=str(asm))
   command=['dotnet',str(p/'preparation/core-harness/bin/Release/net10.0/core.dll'),str(p/folder/'bench'/name/(name+'.dll')),name,str(p/'expected'/(name+'.stdout')),'resources','3','1']
   r=subprocess.run(command,env=env,cwd=repo,capture_output=True)
   (out/(stem+'.stdout')).write_bytes(r.stdout);(out/(stem+'.stderr')).write_bytes(r.stderr)
   ref=(p/'expected'/(name+'.stdout')).read_bytes()
   if r.returncode or not r.stdout.startswith(ref):raise SystemExit('JIT进程语义失败:'+stem)
   rows=[json.loads(x) for x in r.stdout[len(ref):].decode().splitlines()];assert len(rows)==1
   records.append({'which':which,'name':name,'tier':tier,'command':command,'resources':rows,'asm_sha256':hashlib.sha256(asm.read_bytes()).hexdigest()})
   (out/'manifest.json').write_text(json.dumps(records,indent=2)+'\n');print(stem,'ok',flush=True)
