# 同负载成对复测工具：输出目录必须新建；各变体交错顺序避免固定先后偏差。
import os,sys,json,statistics,subprocess,pathlib,time
out=pathlib.Path(sys.argv[1]);out.mkdir(exist_ok=False)
variants={item.split("=",1)[0]:pathlib.Path(item.split("=",1)[1]).resolve() for item in sys.argv[2:]}
expected={"construction":1249975000,"field_reads":1400000,"equality":50000}
report={"variants":{k:str(v) for k,v in variants.items()},"runs":{},"methodology":"每模式每变体3新进程，负载每进程3轮热身+7轮计时；函数体时间与进程wall时间分开；每次校验语义"}
for tier in ("default","disabled"):
 env=dict(os.environ)
 for key in list(env):
  if key.startswith(("DOTNET_Jit","COMPlus_Jit","DOTNET_Tiered","COMPlus_Tiered")):env.pop(key)
 if tier=="disabled":env["DOTNET_TieredCompilation"]="0"
 for repeat in range(3):
  for name in (list(variants) if repeat%2==0 else list(reversed(variants))):
   t=time.perf_counter();run=subprocess.run(["dotnet",str(variants[name]/"dataclass_hot.dll")],env=env,text=True,capture_output=True,check=True);wall=time.perf_counter()-t
   (out/f"{tier}-{name}-{repeat}.stdout").write_text(run.stdout);(out/f"{tier}-{name}-{repeat}.stderr").write_text(run.stderr)
   samples={k:[] for k in expected};semantics=None
   for line in run.stdout.splitlines():
    if line.startswith("semantics "):semantics=line;assert line=="semantics True Record(x=3, y=4)",line
    else:
     label,elapsed,value=line.split();assert int(value)==expected[label],line;samples[label].append(float(elapsed))
   assert semantics and all(len(v)==7 for v in samples.values())
   report["runs"].setdefault(tier+"_"+name,[]).append({"samples":samples,"medians":{k:statistics.median(v) for k,v in samples.items()},"process_wall_seconds":wall,"semantics":semantics})
report["summary"]={key:{label:statistics.median(run["medians"][label] for run in runs) for label in expected} for key,runs in report["runs"].items()}
(out/"comparison.json").write_text(json.dumps(report,ensure_ascii=False,indent=2)+"\n");print(json.dumps(report["summary"],indent=2))
