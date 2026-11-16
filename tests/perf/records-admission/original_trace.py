"""原records真实守卫命中独立诊断，不纳入性能计时。"""
import hashlib,json,os,secrets,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];sys.path.insert(0,str(ROOT/'tests'))
from benchlib.artifact import verified_native,MANIFEST_NAME
from benchlib.suite import Benchmark
cli,product,out=map(Path,sys.argv[1:]);out.mkdir()
source=product/'tests/benchmarks/records.py'
env=os.environ.copy()
for key in ['RYUPY_RUNTIME_DIR','RYUPY_STATS','RYUPY_RECORDS_GUARD_TRACE','RYUPY_RECORDS_GUARD_TOKEN']:env.pop(key,None)
cp=subprocess.run([sys.executable,'-B',str(source)],capture_output=True,env=env,check=True)
assert not cp.stderr
(out/'cp.stdout').write_bytes(cp.stdout)
for backend in ['jit','aot']:
 d=out/backend;d.mkdir()
 command=[str(cli),'build',str(source),'--native','--experimental-records-increment','-o',str(d)]+(['--aot'] if backend=='aot' else [])
 r=subprocess.run(command,cwd=product,env=env,capture_output=True,timeout=1200)
 (out/(backend+'-build.stdout')).write_bytes(r.stdout);(out/(backend+'-build.stderr')).write_bytes(r.stderr)
 (out/(backend+'-command.json')).write_text(json.dumps(command));assert r.returncode==0
 exe=verified_native(d,Benchmark('records',source),r.stderr.decode(),mode='standalone') if backend=='aot' else d/'records.dll'
 if backend=='aot':
  manifest=json.loads((exe.parent/MANIFEST_NAME).read_text());assert manifest['toolchain']['sdk_version']=='10.0.403'
 token=secrets.token_hex(16);trace=out/(backend+'-trace.json')
 command=[str(exe)] if backend=='aot' else [env['DOTNET_HOST_PATH'],str(exe)]
 r=subprocess.run(command,cwd=product,env=env|{'RYUPY_RECORDS_GUARD_TRACE':str(trace),'RYUPY_RECORDS_GUARD_TOKEN':token},capture_output=True,timeout=120)
 (out/(backend+'.stdout')).write_bytes(r.stdout);(out/(backend+'.stderr')).write_bytes(r.stderr)
 assert (r.returncode,r.stdout,r.stderr)==(0,cp.stdout,b'')
 assert json.loads(trace.read_text())=={'schema':1,'scope':'records-compiled-guard-diagnostic','token':token,'fast':240000,'fallback':0}
