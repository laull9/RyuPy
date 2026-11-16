"""冻结CLI真实MIR和AOT构建，清单核SDK。"""
import hashlib,json,os,pathlib,shutil,subprocess,sys
ROOT=pathlib.Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tests'))
from benchlib import suite
from benchlib.artifact import verified_native
cli,work,out=map(pathlib.Path,sys.argv[1:])
source=pathlib.Path(__file__).with_name('probe.py')
out.mkdir(parents=True,exist_ok=True)
mir=subprocess.run([str(cli),'dump',str(source),'--emit','mir'],capture_output=True,check=True)
(out/'probe.mir').write_bytes(mir.stdout);(out/'mir.stderr').write_bytes(mir.stderr)
body=mir.stdout.decode().split('fn produce(',1)[1].split('\nfn ',1)[0]
for marker in ('NumericListCreate','NumericListAppendF64','NumericListReadCheckedF64'):assert marker in body
command=[str(cli),'build',str(source),'-o',str(work),'--native','--aot']
(out/'build-command.json').write_text(json.dumps(command))
p=subprocess.run(command,capture_output=True)
(out/'build.stdout').write_bytes(p.stdout);(out/'build.stderr').write_bytes(p.stderr)
assert p.returncode==0
exe=verified_native(work,suite.Benchmark(name='probe',path=source),p.stderr.decode(),mode='standalone')
manifest=exe.parent/'ryupy-artifact.json'
assert json.loads(manifest.read_text())['toolchain']['sdk_version']=='10.0.403'
shutil.copyfile(manifest,out/'manifest.json')
(out/'executable').write_text(str(exe))
(out/'identity.json').write_text(json.dumps({'source':hashlib.sha256(source.read_bytes()).hexdigest(),'cli':hashlib.sha256(cli.read_bytes()).hexdigest(),'exe':hashlib.sha256(exe.read_bytes()).hexdigest()}))
