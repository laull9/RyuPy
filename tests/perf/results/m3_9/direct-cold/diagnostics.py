"""分配和汇编独立于正式三变体计时；不修改生产。"""
import os,subprocess,sys
from pathlib import Path
b,f,c=map(Path,sys.argv[1:]);variants=[('baseline',b),('first',f),('cold',c)]
h=f/'harness/bin/Release/net10.0/alloc.dll'
for tier in ['default','disabled']:
 env={k:v for k,v in os.environ.items() if not k.startswith(('DOTNET_Jit','COMPlus_Jit','DOTNET_Tiered','COMPlus_Tiered'))}
 if tier=='disabled':env['DOTNET_TieredCompilation']='0'
 for repeat in range(3):
  for label,root in variants[repeat:]+variants[:repeat]:
   p=c/f'allocation-{tier}-{label}-{repeat}'
   x=subprocess.run(['dotnet',str(h),str(root/'artifact/dataclass_hot.dll')],env=env,capture_output=True,text=True,check=True)
   p.with_suffix('.stdout').write_text(x.stdout);p.with_suffix('.stderr').write_text(x.stderr)
 for label,root in variants:
  for method in ['field_reads','accumulate']:
   p=c/f'jit-{method}-{tier}-{label}'
   e=env|{'DOTNET_JitDisasm':f'*{method}*','DOTNET_JitDisasmDiffable':'1','DOTNET_JitStdOutFile':str(p.with_suffix('.asm'))}
   dll=root/('artifact/dataclass_hot.dll' if method=='field_reads' else 'guard-artifact/accumulator_guards.dll')
   command=['dotnet',str(h),str(dll)] if method=='field_reads' else ['dotnet',str(dll)]
   x=subprocess.run(command,env=e,capture_output=True,text=True,check=True)
   p.with_suffix('.stdout').write_text(x.stdout);p.with_suffix('.stderr').write_text(x.stderr)
