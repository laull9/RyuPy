"""独立分配与实际JIT诊断，不混正式计时；同一已编译循环/同一回调计数校验。"""
from pathlib import Path
import json,os,subprocess,sys,hashlib
base, candidate = map(Path,sys.argv[1:3]); jit=len(sys.argv)>3 and sys.argv[3]=='jit'
out=candidate/('loop-jit' if jit else 'loop-resources');out.mkdir(exist_ok=False)
reference=(base/'cpython-diagnostic.stdout').read_bytes();records=[]
for tier in ['default','disabled']:
 for name,folder in [('baseline',base),('candidate',candidate)]:
  env={k:v for k,v in os.environ.items() if not k.startswith(('DOTNET_Jit','COMPlus_Jit','DOTNET_Tiered','COMPlus_Tiered'))}
  for k in ['RYUPY_STATS','RYUPY_PIC']:env.pop(k,None)
  if tier=='disabled':env['DOTNET_TieredCompilation']='0'
  stem=f'{tier}-{name}'
  if jit:env.update(DOTNET_JitDisasm='*m0_accumulate* *m0_repeated*',DOTNET_JitDisasmDiffable='1',DOTNET_JitStdOutFile=str(out/(stem+'.asm')))
  command=['dotnet',str(candidate/'loop-harness/bin/Release/net10.0/loop.dll'),str(folder/'loop-artifact/float_loop.dll')]
  r=subprocess.run(command,env=env,capture_output=True)
  (out/(stem+'.stdout')).write_bytes(r.stdout);(out/(stem+'.stderr')).write_bytes(r.stderr)
  if r.returncode or not r.stdout.startswith(reference):raise RuntimeError('诊断失败:'+stem+' '+r.stderr.decode())
  rows=[json.loads(x) for x in r.stdout[len(reference):].decode().splitlines()];assert len(rows)==33
  record={'tier':tier,'which':name,'command':command,'samples':rows}
  if jit:record['asm_sha256']=hashlib.sha256((out/(stem+'.asm')).read_bytes()).hexdigest()
  records.append(record);(out/'results.json').write_text(json.dumps(records,indent=2)+'\n')
  print(stem,'ok',flush=True)
