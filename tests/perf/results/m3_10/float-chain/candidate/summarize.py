"""固定单轮预设配对汇总；不删不利样本，不以混杂修正数值。"""
from pathlib import Path
import json,statistics,sys
p=Path(sys.argv[1]);pure=json.loads((p/'float-paired/comparison.json').read_text())['summary'];core=json.loads((p/'core-paired/results.json').read_text())['runs'];resources=json.loads((p/'float-resources/results.json').read_text());lines=['# 时间与分配汇总','','每组3进程×7样本，进程内3轮预热；先每进程中位，再跨进程中位。资源独立进程3样本；这些是kernel/微负载，不是原main冷时间。','']
summary={}
for tier in ['default','disabled']:
 lines += ['## 纯链 '+tier,'','| 场景 | B/C ms（30000轮） | 时间变化 | B/C ns每轮 | B/C分配字节 |','|---|---:|---:|---:|---:|']
 for name,b in pure[tier+'_baseline'].items():
  c=pure[tier+'_candidate'][name]
  alloc={x['which']:statistics.median(r['alloc_bytes'] for r in x['samples'] if r['label']==name) for x in resources if x['tier']==tier}
  summary[name+'|'+tier]={'baseline_ms':b*1000,'candidate_ms':c*1000,'ratio':c/b,'baseline_alloc':alloc['baseline'],'candidate_alloc':alloc['candidate']}
  lines.append(f"| {name} | {b*1000:.4f} / {c*1000:.4f} | {(c/b-1)*100:+.1f}% | {b*1e9/30000:.2f} / {c*1e9/30000:.2f} | {alloc['baseline']} / {alloc['candidate']} |")
 lines += ['','## 实际数值kernel '+tier,'','| 函数 | B/C ms | 时间变化 | B/C分配字节 |','|---|---:|---:|---:|']
 for name in ['matmul','nbody']:
  times={};alloc={}
  for who in ['baseline','candidate']:
   rows=[x for x in core if x['name']==name and x['tier']==tier and x['variant']==who]
   times[who]=statistics.median(statistics.median(s['elapsed_ms'] for s in x['samples']) for x in rows if x['mode']=='time')
   alloc[who]=statistics.median(s['alloc_bytes'] for x in rows if x['mode']=='resources' for s in x['samples'])
  b,c=times['baseline'],times['candidate'];summary[name+'|'+tier]={'baseline_ms':b,'candidate_ms':c,'ratio':c/b,'baseline_alloc':alloc['baseline'],'candidate_alloc':alloc['candidate']}
  lines.append(f"| {name} | {b:.4f} / {c:.4f} | {(c/b-1)*100:+.1f}% | {alloc['baseline']} / {alloc['candidate']} |")
 lines.append('')
(p/'summary.md').write_text('\n'.join(lines)+'\n');(p/'summary.json').write_text(json.dumps(summary,indent=2)+'\n')
