"""仅matmul旧/新成功guard区域的实际JIT，独立于计时且不扩组。"""
from pathlib import Path
import sys,json,os,subprocess,hashlib
root=Path(sys.argv[1]).resolve();out=root/'jit';out.mkdir(exist_ok=False);records=[]
for tier in ['default','disabled']:
 for which in ['before','after']:
  env={k:v for k,v in os.environ.items() if not k.startswith(('DOTNET_Jit','COMPlus_Jit','DOTNET_Tiered','COMPlus_Tiered'))}
  for key in ['RYUPY_STATS','RYUPY_PIC']:env.pop(key,None)
  if tier=='disabled':env['DOTNET_TieredCompilation']='0'
  stem=tier+'-'+which;asm=out/(stem+'.asm')
  env.update(DOTNET_JitDisasm='m0_matmul ReadCheckedF64 ReadF64 CanReadF64',DOTNET_JitDisasmDiffable='1',DOTNET_JitStdOutFile=str(asm))
  expected=root/'validation/matmul.stdout'
  command=['dotnet',str(root/'core-harness/bin/Release/net10.0/measure.dll'),str(root/which/'bench/matmul/matmul.dll'),'matmul',str(expected),'resources','3','1']
  r=subprocess.run(command,env=env,capture_output=True)
  (out/(stem+'.stdout')).write_bytes(r.stdout);(out/(stem+'.stderr')).write_bytes(r.stderr)
  if r.returncode or not r.stdout.startswith(expected.read_bytes()):raise RuntimeError(stem+'语义失败')
  records.append({'tier':tier,'which':which,'command':command,'exit_code':r.returncode,'asm_sha256':hashlib.sha256(asm.read_bytes()).hexdigest()})
  (out/'manifest.json').write_text(json.dumps(records,indent=2)+'\n');print(stem,'ok',flush=True)
