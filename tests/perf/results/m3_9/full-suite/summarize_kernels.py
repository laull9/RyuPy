"""分别汇总同进程预热kernel与独立资源进程；不与进程main混表。"""
from pathlib import Path
import json,statistics,sys
p=Path(sys.argv[1]);r=json.loads((p/'core-kernels/results.json').read_text());rows=r['runs'];result={}
for name in ['fib','mandelbrot','matmul','nbody']:
 for tier in ['default','disabled']:
  for who in ['baseline','candidate','cpython']:
   matches=[x for x in rows if x['name']==name and x['tier']==tier and x['variant']==who and x['mode']=='time' and x['exit_code']==0]
   if not matches:continue
   medians=[statistics.median(s['elapsed_ms'] for s in x['samples']) for x in matches]
   value={'process_count':len(matches),'process_medians_ms':medians,'median_ms':statistics.median(medians),'process_maxrss_median':statistics.median(x['maxrss'] for x in matches)}
   resources=[s for x in rows if x['name']==name and x['tier']==tier and x['variant']==who and x['mode']=='resources' and x['exit_code']==0 for s in x['samples']]
   value['resources_raw']=resources
   for field in ['alloc_bytes','gc0','gc1','gc2','gc_pause_ms','jit_ms','jit_methods']:
    ns=[x[field] for x in resources if field in x]
    if ns:value[field+'_median']=statistics.median(ns)
   result[f'{name}|{tier}|{who}']=value
(p/'kernel-summary.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
text=['# 同进程预热 kernel（独立于原main）','','每进程原main初始化后3次完整kernel预热，再7样本；3进程轮转。先取每进程样本中位数，再取3进程中位数。资源来自独立进程，不混正式时间。JIT差值是全进程而非kernel专属；实际版本见core-jit。','']
for tier in ['default','disabled']:
 text += ['## '+tier,'','| kernel | ms B/C | 变化 | CPython ms | alloc B/C | GC0 B/C |','|---|---:|---:|---:|---:|---:|']
 for name in ['fib','mandelbrot','matmul','nbody']:
  a=result.get(f'{name}|{tier}|baseline');b=result.get(f'{name}|{tier}|candidate');cp=result.get(f'{name}|default|cpython')
  if not a or not b:continue
  ratio=(b['median_ms']/a['median_ms']-1)*100
  def pair(k):return str(a.get(k,'—'))+' / '+str(b.get(k,'—'))
  text.append(f"| {name} | {a['median_ms']:.3f} / {b['median_ms']:.3f} | {ratio:+.1f}% | {cp['median_ms']:.3f} | {pair('alloc_bytes_median')} | {pair('gc0_median')} |" if cp else name)
 text.append('')
(p/'kernel-summary.md').write_text('\n'.join(text)+'\n')
