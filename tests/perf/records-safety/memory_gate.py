"""Python产品PSS与独立Runtime活堆分列，四轮独立进程，禁止混为同一指标。"""
import argparse,hashlib,json,os,select,subprocess,sys,time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];sys.path.insert(0,str(ROOT/'tests'))
from benchlib.artifact import verified_native,MANIFEST_NAME
from benchlib.suite import Benchmark
p=argparse.ArgumentParser()
for key in ['cli','product','host','out']:p.add_argument('--'+key,type=Path,required=True)
a=p.parse_args();a.out.mkdir();source=Path(__file__).with_name('memory.py')
env=os.environ.copy()
for key in ['RYUPY_RUNTIME_DIR','RYUPY_STATS','RYUPY_RECORDS_GUARD_TRACE','RYUPY_RECORDS_GUARD_TOKEN']:env.pop(key,None)
executables={}
for arm in ['off','on']:
 d=a.out/arm;d.mkdir()
 dump=[str(a.cli),'dump',str(source),'--native','--emit','mir']+(['--experimental-records-increment'] if arm=='on' else [])
 dm=subprocess.run(dump,cwd=a.product,env=env,capture_output=True,timeout=300)
 (a.out/(arm+'-mir.txt')).write_bytes(dm.stdout);(a.out/(arm+'-mir.stderr')).write_bytes(dm.stderr)
 assert dm.returncode==0 and dm.stdout.count(b'RecordsTryIncrementMaterialized')==(1 if arm=='on' else 0)
 command=[str(a.cli),'build',str(source),'--native','--aot','-o',str(d)]+(['--experimental-records-increment'] if arm=='on' else [])
 r=subprocess.run(command,cwd=a.product,env=env,capture_output=True,timeout=1200)
 (a.out/(arm+'-build.stdout')).write_bytes(r.stdout);(a.out/(arm+'-build.stderr')).write_bytes(r.stderr);assert r.returncode==0
 exe=verified_native(d,Benchmark('memory',source),r.stderr.decode(),mode='standalone')
 m=json.loads((exe.parent/MANIFEST_NAME).read_text());assert m['toolchain']['sdk_version']=='10.0.403' and m['compiler']['options']['experimental_records_increment']==(arm=='on')
 executables[arm]=exe
rows=[]
for round_,order in enumerate([['off','on'],['on','off'],['on','off'],['off','on']]):
 for n in [1024,8192]:
  for phase in ['held','cleared','released']:
   # CP同源码真实握手oracle，所有身份验证在ACK后才完成。
   cp=subprocess.run([sys.executable,'-B',str(source),str(n),phase],input=b'ACK\n',capture_output=True,env=env,timeout=120)
   assert cp.returncode==0 and not cp.stderr
   for arm in order:
    d=a.out/f'{round_}-{n}-{phase}-{arm}';d.mkdir()
    q=subprocess.Popen([str(executables[arm]),str(n),phase],cwd=a.product,env=env,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,bufsize=0)
    prefix=b'';deadline=time.monotonic()+120
    try:
     while not prefix.endswith(b'READY\n'):
      assert time.monotonic()<deadline and q.poll() is None,'READY failed'
      if select.select([q.stdout],[],[],1)[0]: prefix+=os.read(q.stdout.fileno(),1)
     samples=[]
     for i in range(5):
      raw=Path(f'/proc/{q.pid}/smaps_rollup').read_text();(d/f'smaps-{i}.txt').write_text(raw)
      (d/f'status-{i}.txt').write_text(Path(f'/proc/{q.pid}/status').read_text())
      sample={}
      for line in raw.splitlines():
       bits=line.split()
       if len(bits)==3 and bits[2]=='kB': sample[bits[0].rstrip(':')]=int(bits[1])*1024
      samples.append(sample);time.sleep(.15)
     stdout,stderr=q.communicate(b'ACK\n',timeout=120);stdout=prefix+stdout
     (d/'product.stdout').write_bytes(stdout);(d/'product.stderr').write_bytes(stderr);(d/'cp.stdout').write_bytes(cp.stdout)
     assert (q.returncode,stdout,stderr)==(0,cp.stdout,b'')
    except Exception as error:
     if q.poll() is None:q.kill()
     remaining,failed_stderr=q.communicate(timeout=10)
     (d/'failed.stdout').write_bytes(prefix+remaining);(d/'failed.stderr').write_bytes(failed_stderr)
     (d/'failure.json').write_text(json.dumps({'error':str(error),'exit':q.returncode}))
     raise
    finally:
     if q.poll() is None:q.kill();q.wait()
    h=subprocess.run([str(a.host),arm,str(n),phase],env=env,capture_output=True,timeout=120)
    (d/'host.stdout').write_bytes(h.stdout);(d/'host.stderr').write_bytes(h.stderr)
    assert h.returncode==0 and not h.stderr
    lines=h.stdout.decode().splitlines();mem=next(x.split() for x in lines if x.startswith('MEM '))
    assert mem[1:4]==[arm,str(n),phase] and int(mem[-1])==0
    assert int(mem[-2])==(n*2 if phase=='held' else 0)
    batches=[x.split() for x in lines if x.startswith('BATCH ')];assert len(batches)==4 and all(x[-1]=='0' for x in batches)
    rows.append({'round':round_,'size':n,'phase':phase,'arm':arm,'product_pss_samples':samples,'host_mem':mem,'host_batches':batches})
    (a.out/'rows.json').write_text(json.dumps(rows,indent=2))
assert len(rows)==48
