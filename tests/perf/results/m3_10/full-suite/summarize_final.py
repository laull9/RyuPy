"""单冷样本描述表，不做显著性/稳态推断，所有26项不利完整展示。"""
from pathlib import Path
import json,math
root=Path(__file__).resolve().parent;r=json.loads((root/'results.json').read_text());rows=[]
for tier in ['default','disabled']:
 names=list(dict.fromkeys(x['name'] for x in r['samples']))
 for name in names:
  a=next(x for x in r['samples'] if x['tier']==tier and x['name']==name and x['variant']=='A');c=next(x for x in r['samples'] if x['tier']==tier and x['name']==name and x['variant']=='C')
  assert a['status']==c['status']=='ok'
  row={'tier':tier,'name':name,'A':a,'C':c,'main_ratio':c['stats']['main_ms']/a['stats']['main_ms'],'wall_ratio':c['wall_ms']/a['wall_ms'],'allocation_ratio':c['stats']['alloc_bytes']/a['stats']['alloc_bytes']};rows.append(row)
report={'scope':'单冷样本描述性比值，非统计稳定/显著性证据','rows':rows,'geomeans_without_hello':{}}
for tier in ['default','disabled']:
 selection=[x for x in rows if x['tier']==tier and x['name']!='hello'];report['geomeans_without_hello'][tier]={key:math.exp(sum(math.log(x[key]) for x in selection)/len(selection)) for key in ['main_ratio','wall_ratio','allocation_ratio']}
(root/'summary.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
lines=['# 整26单冷样本描述表','', 'A=最初M3.9 CLI+原runtime；C=最终M3.10默认CLI+最终runtime。非稳态，无显著性结论。','']
for tier in ['default','disabled']:
 lines+=['## '+tier,'','|项目|A main ms|C main ms|main变化|A/C wall ms|A/C alloc B|','|---|---:|---:|---:|---|---|']
 for x in rows:
  if x['tier']!=tier:continue
  a,c=x['A'],x['C'];lines.append(f"|{x['name']}|{a['stats']['main_ms']:.3f}|{c['stats']['main_ms']:.3f}|{(x['main_ratio']-1)*100:+.1f}%|{a['wall_ms']:.3f}/{c['wall_ms']:.3f}|{a['stats']['alloc_bytes']}/{c['stats']['alloc_bytes']}|")
 lines+=['','排hello的25项描述性几何比值：'+str(report['geomeans_without_hello'][tier]),'']
(root/'summary.md').write_text('\n'.join(lines)+'\n')
print(report['geomeans_without_hello'])
for tier in ['default','disabled']:
 print(tier,'worst',[(x['name'],round((x['main_ratio']-1)*100,1)) for x in sorted([x for x in rows if x['tier']==tier],key=lambda x:x['main_ratio'],reverse=True)[:6]])
 for x in rows:
  if x['tier']==tier and x['name'] in ['matmul','nbody','mandelbrot','fib']:
   print(x['name'],x['A']['stats']['main_ms'],x['C']['stats']['main_ms'],x['A']['stats']['alloc_bytes'],x['C']['stats']['alloc_bytes'])
