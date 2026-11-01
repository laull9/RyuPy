"""matmul/nbody真实compiled kernel的独立JIT诊断；不增加正式计时轮次。"""
from pathlib import Path
import sys,json,os,subprocess,hashlib
base,candidate=map(Path,sys.argv[1:3]);out=candidate/'numeric-jit';out.mkdir(exist_ok=False);records=[]
for name,pattern in [('matmul','*m0_matmul*')]:
 for tier in ['default','disabled']:
  for which,folder in [('baseline',base),('candidate',candidate)]:
   env={k:v for k,v in os.environ.items() if not k.startswith(('DOTNET_Jit','COMPlus_Jit','DOTNET_Tiered','COMPlus_Tiered'))}
   for k in ['RYUPY_STATS','RYUPY_PIC']:env.pop(k,None)
   if tier=='disabled':env['DOTNET_TieredCompilation']='0'
   stem=f'{name}-{tier}-{which}';asm=out/(stem+'.asm')
   env.update(DOTNET_JitDisasm=pattern,DOTNET_JitDisasmDiffable='1',DOTNET_JitStdOutFile=str(asm))
   expected=candidate/'expected'/(name+'.stdout')
   command=['dotnet',str(candidate/'core-harness/bin/Release/net10.0/core.dll'),str(folder/'bench'/name/(name+'.dll')),name,str(expected),'resources','3','1']
   r=subprocess.run(command,env=env,capture_output=True)
   (out/(stem+'.stdout')).write_bytes(r.stdout);(out/(stem+'.stderr')).write_bytes(r.stderr)
   if r.returncode or not r.stdout.startswith(expected.read_bytes()):raise RuntimeError('JIT进程语义失败:'+stem)
   records.append({'name':name,'tier':tier,'which':which,'command':command,'asm_sha256':hashlib.sha256(asm.read_bytes()).hexdigest()})
   (out/'manifest.json').write_text(json.dumps(records,indent=2)+'\n');print(stem,'ok',flush=True)
