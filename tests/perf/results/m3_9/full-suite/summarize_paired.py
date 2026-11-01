"""逐项轮转主表；只用预先约定三正式样本，不挑样本或按CP漂移归一化。"""
from pathlib import Path
import json,statistics,sys
p=Path(sys.argv[1]);data=json.loads((p/'paired-suite/results.json').read_text()); rows=data['runs_raw']; groups={}
for r in rows:
 if r['recorded']:
  key=tuple(r[x] for x in ['name','tier','variant','which']);groups.setdefault(key,[]).append(r)
values={}
for key,rs in groups.items():
 v={k:statistics.median(r[k] for r in rs) for k in ['wall_ms','maxrss']}
 for k in set().union(*(r['stats'].keys() for r in rs)):
  nums=[r['stats'][k] for r in rs if isinstance(r['stats'].get(k),(int,float))]
  if nums:v[k]=statistics.median(nums)
 values[key]=v
names=list(dict.fromkeys(r['name'] for r in rows));text=['# 逐 benchmark 轮转主比较','', '预编译同runtime；每项每tier含1轮热身+3轮正式，B/C顺序逐轮交替且逐项目反转。原PIC-off独立保留；这是进程main，不是稳态kernel。','']
for tier in ['default','disabled']:
 text += ['## '+tier,'','| 项目/variant | main ms B/C | 变化 | cold wall ms B/C | alloc B/C |','|---|---:|---:|---:|---:|']
 for n in names:
  variants=list(dict.fromkeys(r['variant'] for r in rows if r['name']==n))
  for variant in variants:
   a=values.get((n,tier,variant,'baseline'));b=values.get((n,tier,variant,'candidate'))
   if not a or not b:continue
   def pair(k):return f'{a.get(k,0):.3f} / {b.get(k,0):.3f}'
   change=100*(b['main_ms']/a['main_ms']-1)
   text.append(f"| {n}/{variant} | {pair('main_ms')} | {change:+.1f}% | {pair('wall_ms')} | {pair('alloc_bytes')} |")
 text.append('')
(p/'paired-summary.md').write_text('\n'.join(text)+'\n')
(p/'paired-summary.json').write_text(json.dumps({'|'.join(k):v for k,v in values.items()},indent=2)+'\n')
