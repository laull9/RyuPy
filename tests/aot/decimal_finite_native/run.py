"""独立有限Decimal真实Native：固定CP、双PIC、唯一ELF、原位/搬迁及源码隔离。"""
import json
import os
import shutil
import sys
import traceback
from pathlib import Path
from contract import require, sha, complete, positive, context, source_index, fixture, PYTHON_SHA

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT/'tests/validation'))
sys.path.insert(0, str(ROOT/'tests/aot'))
import markdown_preflight as capture
from aot_layout import reported_aot
from native_evidence import preserve_dist, snapshot
import manifest_verify as manifest


class Runner:
    def __init__(self):
        self.out = ROOT/'target/decimal-finite-native-evidence'; self.out.mkdir(parents=True, exist_ok=False)
        self.python = Path(os.environ['PYTHON']).resolve(strict=True)
        self.dotnet = Path(os.environ['DOTNET']).resolve(strict=True)
        require(sys.version_info[:3] == (3, 12, 13) and sha(self.python) == PYTHON_SHA, '固定CP身份不同')
        self.env = dict(os.environ, PYTHONDONTWRITEBYTECODE='1', PYTHONHASHSEED='0')
        self.labels = set(); self.frozen = {}; self.result = {'passed': False, 'stages': {}, 'native_executions': 0}
        self.save('result.json', self.result)

    def save(self, name, value): capture.save(self.out/name, value)
    def freeze(self, paths):
        for path in paths: self.frozen[str(path)] = sha(path)
        self.save('frozen.json', self.frozen)
    def barrier(self):
        for p, digest in self.frozen.items(): require(sha(p) == digest, '冻结输入变化')
    def command(self, label, argv, timeout=120, env=None):
        require(label not in self.labels, '禁止重试或重复标签'); self.labels.add(label); self.barrier()
        record = capture.capture(self.out/label, argv, ROOT, timeout, self.env if env is None else env)
        self.barrier(); return record
    def checked(self, label, argv, timeout=120):
        record = self.command(label, argv, timeout); complete(record); return record
    def streams(self, label): return ((self.out/(label+'.stdout')).read_bytes(), (self.out/(label+'.stderr')).read_bytes())
    def good(self, label, argv, golden, env=None):
        record = self.command(label, argv, 30, env); positive(record, *self.streams(label), golden)
    def phase(self, name, fn):
        self.result['stages'][name] = 'started'; self.save('result.json', self.result)
        fn(self); self.result['stages'][name] = 'passed'; self.save('result.json', self.result)


def identity(r, name):
    r.checked(name, ['git', 'ls-files', '-s', '-z'])
    rows = source_index(ROOT, r.streams(name)[0]); r.save(name+'-files.json', rows); return rows


def prepare(r):
    r.checked('head', ['git', 'rev-parse', 'HEAD'])
    require(r.streams('head')[0].decode().strip() == os.environ['GITHUB_SHA'], 'HEAD变化')
    r.before = identity(r, 'source-before'); r.freeze([ROOT/x['path'] for x in r.before])
    r.checked('runtime-build', [r.dotnet, 'build', 'runtime/RyuPy.Runtime/RyuPy.Runtime.csproj', '-c', 'Release', '-nologo'], 300)
    r.checked('cli-build', ['cargo', 'build', '--locked', '-p', 'ryupy-cli'], 1200)
    tools = r.out/'tools'; tools.mkdir(); r.cli = tools/'ryupy'; r.runtime = tools/'RyuPy.Runtime.dll'
    shutil.copy2(ROOT/'target/debug/ryupy', r.cli)
    shutil.copy2(ROOT/'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll', r.runtime)
    r.env.update(RYUPY_RUNTIME_DIR=str(tools), DOTNET=str(r.dotnet), DOTNET_ROOT=str(r.dotnet.parent))
    r.freeze([r.cli, r.runtime, r.python, r.dotnet])
    r.inputs = r.out/'inputs'; shutil.copytree(HERE/'fixtures', r.inputs)
    r.golden = fixture(r.inputs); r.entry = r.inputs/'entry.py'
    r.freeze([p for p in r.inputs.iterdir() if p.is_file()])


def managed(r):
    r.good('cpython', [r.python, '-B', r.entry], r.golden)
    out = r.out/'jit'; r.checked('jit-build', [r.cli, 'build', r.entry, '--out-dir', out], 180)
    pe = out/'entry.dll'
    require(pe.is_file() and sha(out/'RyuPy.Runtime.dll') == sha(r.runtime), 'JIT真实输入变化')
    r.freeze([pe, out/'RyuPy.Runtime.dll'])
    for pic in ('1', '0'): r.good('jit-pic'+pic, [r.dotnet, pe], r.golden, dict(r.env, RYUPY_PIC=pic))
    r.result['jit_executions'] = 2


def native(r):
    target = r.out/'native-build'
    record = r.command('native-publish', [r.cli, 'build', r.entry, '--out-dir', target, '--aot'], 600)
    saved = r.out/'publish-inputs'; saved.mkdir()
    for p in target.rglob('*') if target.exists() else []:
        if not p.is_file() or p.is_symlink() or any(x in {'obj', 'bin', 'publish'} for x in p.relative_to(target).parts): continue
        if p.suffix in ('.dll', '.csproj', '.json', '.rsp') or p.name in ('Program.cs', 'sdk.version', 'publish.stdout.log', 'publish.stderr.log'):
            dest = saved/p.relative_to(target); dest.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(p, dest)
    complete(record)
    layout = reported_aot(r.streams('native-publish')[1], target, 'entry', 'linux-x64')
    archive = r.out/'native-dist'; archive.mkdir(); preserve_dist(layout.dist, archive)
    manifest.image_header(layout.native, 'linux-x64')
    data = json.loads(layout.manifest.read_text())
    actual = manifest.check_artifacts(layout.dist, data['artifacts'], manifest.MANIFEST_NAME)
    executable = [x for x in data['artifacts'] if x['role'] == 'native_executable']
    require(len(executable) == 1 and actual[executable[0]['relative_path']].samefile(layout.native), '非唯一Native入口')
    require(manifest.read_sdk_version(layout.host/'sdk.version') == '10.0.403', 'SDK变化')
    require(sha(layout.managed.parent/'RyuPy.Runtime.dll') == sha(r.runtime), '真实runtime变化')
    inputs = {x['role']: x for x in data['build_inputs']}
    require(inputs['runtime_managed_input']['sha256'] == sha(r.runtime)
            and inputs['compiled_managed_input']['sha256'] == sha(layout.managed), '清单输入身份变化')
    r.freeze([layout.native, layout.managed, layout.manifest])
    hidden = r.inputs.with_name('inputs-hidden'); frozen = dict(r.frozen)
    require(not hidden.exists(), '隔离位置冲突'); r.inputs.rename(hidden)
    r.frozen = {str(hidden/Path(p).relative_to(r.inputs)) if Path(p).is_relative_to(r.inputs) else p: h for p, h in r.frozen.items()}
    r.save('source-isolation.json', {'restored': False})
    try:
        before = snapshot(layout.dist); clean = {'PATH': '/usr/bin:/bin', 'LC_ALL': 'C.UTF-8', 'TZ': 'UTC'}
        r.good('native-original', [layout.native], r.golden, clean)
        require(snapshot(layout.dist) == before, '原位修改dist')
        moved = r.out/'relocated'; shutil.copytree(layout.dist, moved)
        require(snapshot(moved) == before, '搬迁产物改变')
        r.good('native-relocated', [moved/layout.native.name], r.golden, clean)
        require(snapshot(moved) == before, '搬迁运行修改dist')
    finally:
        require(not r.inputs.exists(), '产品重建输入位置'); hidden.rename(r.inputs); r.frozen = frozen
        r.barrier(); r.save('source-isolation.json', {'restored': True})
    r.result['native_executions'] = 2


def main():
    context(os.environ, json.loads(Path(os.environ['GITHUB_EVENT_PATH']).read_text())); r = Runner()
    try:
        r.phase('prepare', prepare); r.phase('managed', managed); r.phase('native', native)
        require(identity(r, 'source-after') == r.before, '完整源码发生改变')
        r.result.update(passed=True, source_after_verified=True, scope='有限Decimal值；非算术/Context/全部decimal')
    except Exception:
        r.result['error'] = traceback.format_exc(); raise
    finally:
        r.save('result.json', r.result)


if __name__ == '__main__': main()
