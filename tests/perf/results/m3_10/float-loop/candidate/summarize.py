"""固定单组FloatLoop配对汇总，保全部不利数据与极小empty开销。"""
from pathlib import Path
import json,statistics,sys
p=Path(sys.argv[1]);times=json.loads((p/'loop-paired/comparison.json').read_text())['summary'];resource=json.loads((p/'loop-resources/results.json').read_text());cores=json.loads((p/'core-paired/results.json').read_text())['runs'];result={};text=['# FloatLoop 单组汇总','','3进程×7样本，两tier；每进程3轮预热，资源/JIT另进程。微负载float含30000次getitem，empty为零轮，reentry含多次内层初始化。','']
for tier in ['default','disabled']:
 text += ['## '+tier,'','| 场景 | B/C ms | 时间变化 | B/C分配字节 |','|---|---:|---:|---:|']
 for name,b in times[tier+'_baseline'].items():
  c=times[tier+'_candidate'][name];alloc={r['which']:statistics.median(s['alloc_bytes'] for s in r['samples'] if s['label']==name) for r in resource if r['tier']==tier}
  result[name+'|'+tier]={'baseline_ms':b*1000,'candidate_ms':c*1000,'ratio':c/b,'alloc':alloc}
  text.append(f"| {name} | {b*1000:.6f} / {c*1000:.6f} | {(c/b-1)*100:+.1f}% | {alloc['baseline']} / {alloc['candidate']} |")
 data={}
 for which in ['baseline','candidate']:
  rs=[r for r in cores if r['variant']==which and r['tier']==tier]
  data[which]={'ms':statistics.median(statistics.median(s['elapsed_ms'] for s in r['samples']) for r in rs if r['mode']=='time'),'alloc':statistics.median(s['alloc_bytes'] for r in rs if r['mode']=='resources' for s in r['samples'])}
 b,c=data['baseline'],data['candidate'];result['matmul|'+tier]=data
 text += ['',f"matmul 原规模kernel：{b['ms']:.6f}→{c['ms']:.6f}ms（{(c['ms']/b['ms']-1)*100:+.1f}%），分配{b['alloc']}→{c['alloc']}B。",'']
(p/'summary.md').write_text('\n'.join(text)+'\n');(p/'summary.json').write_text(json.dumps(result,indent=2)+'\n')
