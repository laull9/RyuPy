"""全部样本中位数与逐进程中位数；不删除不利样本。"""
from pathlib import Path
import json
import statistics as st
root=Path(__file__).resolve().parent
report={}
for phase in ['time','resources']:
 if not (root/phase/'runs.json').exists(): continue
 grouped={}
 for record in json.loads((root/phase/'runs.json').read_text()):
  stem=f"{record['scope']}-{record['tier']}-{record['variant']}-{record['repeat']}"
  lines=(root/phase/(stem+'.stdout')).read_text().splitlines()
  for line in lines:
   if line.startswith('sample '):
    _,label,value,ok=line.split(); value=float(value)*1000; metric='milliseconds'
   elif line.startswith('{'):
    r=json.loads(line); label=r.get('label','matmul'); metric='alloc_bytes' if phase=='resources' else 'milliseconds'; value=r['alloc_bytes'] if phase=='resources' else r['elapsed_ms']
   else: continue
   k=(record['tier'],record['variant'],label,metric)
   grouped.setdefault(k,{}).setdefault(record['repeat'],[]).append(value)
 report[phase]={'|'.join(k):{'median':st.median(st.median(x) for x in groups.values()),'process_medians':[st.median(x) for x in groups.values()],'raw':list(groups.values())} for k,groups in grouped.items()}
(root/'summary.json').write_text(json.dumps(report,indent=2)+'\n')
for phase, rows in report.items():
 print(phase)
 labels=sorted(set(k.split('|')[2] for k in rows))
 for tier in ['default','disabled']:
  for label in labels:
   metric='alloc_bytes' if phase=='resources' else 'milliseconds'
   vals=[rows.get('|'.join([tier,v,label,metric]),{}).get('median') for v in ['A','B','C']]
   if None in vals: continue
   a,b,c=vals
   print(tier,label,vals,'B/A%',round((b/a-1)*100,2) if a else None,'C/A%',round((c/a-1)*100,2) if a else None,'C/B%',round((c/b-1)*100,2) if b else None)
