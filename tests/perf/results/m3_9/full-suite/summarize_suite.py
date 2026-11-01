"""只读取完整原始样本；原main含本进程JIT，与后续kernel稳态分表。"""
from pathlib import Path
import json,statistics,sys
root=Path(sys.argv[1]); output={}
for variant in ['e1-default','current-default','e1-disabled','current-disabled']:
 p=root/variant
 resultfile=p/'bench/results/suite.json'
 if not resultfile.exists():continue
 results=json.loads(resultfile.read_text()); groups={}
 for f in sorted((p/'raw').glob('process-*.json')):
  row=json.loads(f.read_text()); cmd=row['command']; kind='cpython' if row['stats_env'] is None else 'ryupy'
  name=Path(cmd[-1]).stem
  pic=row['environment'].get('RYUPY_PIC','default'); key=(name,kind,pic)
  groups.setdefault(key,[]).append(row)
 table={}
 for result in results['results']:
  name=result['name']; values={'status':result['status'],'compile_ms':result['compile_ms'],'series':{}}
  for (bench,kind,pic),rows in groups.items():
   if bench!=name:continue
   rows=rows[1:] # 独立进程热身，原样保留在raw，统计仅3个正式样本。
   metrics={}
   for key in ['wall_ms','maxrss']:
    metrics[key]=statistics.median(x[key] for x in rows)
   for key in sorted(set().union(*(x['stats'].keys() for x in rows))):
    samples=[x['stats'][key] for x in rows if isinstance(x['stats'].get(key),(int,float))]
    if samples:metrics[key]=statistics.median(samples)
   metrics['sample_count']=len(rows)
   values['series'][kind+':'+pic]=metrics
  table[name]=values
 output[variant]=table
(root/'suite-summary.json').write_text(json.dumps(output,ensure_ascii=False,indent=2)+'\n')
lines=['# 原26项进程基准','', '两套CLI profile不同：e1为debug，current为release，compile_ms不可据此推断编译器速度。独立进程warmup不是同进程稳态；main_ms仍含JIT。','']
for tier in ['default','disabled']:
 a=output.get('e1-'+tier);b=output.get('current-'+tier)
 if not a or not b:continue
 lines += ['## '+tier,'','| 项目 | 状态 e1/current | main ms e1/current | main变化 | cold wall ms e1/current | alloc B e1/current |','|---|---|---:|---:|---:|---:|']
 for name in b:
  x=a[name]['series'].get('ryupy:default',{}); y=b[name]['series'].get('ryupy:default',{})
  def pair(k):return f"{x.get(k,0):.3f} / {y.get(k,0):.3f}"
  change=(y['main_ms']/x['main_ms']-1)*100 if x.get('main_ms') and y.get('main_ms') else None
  pct=f'{change:+.1f}%' if change is not None else '—'
  lines.append(f"| {name} | {a[name]['status']}/{b[name]['status']} | {pair('main_ms')} | {pct} | {pair('wall_ms')} | {pair('alloc_bytes')} |")
 lines.append('')
(root/'suite-summary.md').write_text('\n'.join(lines)+'\n')
# CPython在不同整套时段的差异只显示，不以事后归一化擦掉RyuPy回退。
noise=[]
for tier in ['default','disabled']:
 a=output.get('e1-'+tier);b=output.get('current-'+tier)
 if not a or not b:continue
 for name in b:
  x=a[name]['series'].get('cpython:default',{});y=b[name]['series'].get('cpython:default',{})
  noise.append({'tier':tier,'name':name,'e1_period_cpython_main_ms':x.get('main_ms'),'current_period_cpython_main_ms':y.get('main_ms'),'ratio':y.get('main_ms',0)/x['main_ms'] if x.get('main_ms') else None})
(root/'cpython-period-variation.json').write_text(json.dumps(noise,indent=2)+'\n')
