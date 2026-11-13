"""准备隔离的同源直接/薄入口工具与锁定项目；不新增产品开关。"""
import argparse
import difflib
import hashlib
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys
import tarfile
import urllib.request
from urllib.parse import urlparse

REPO = Path(__file__).resolve().parents[3]
SDK = '10.0.403'
NEEDLE = b'if source_aot && platform != HostPlatform::WindowsX64 {'
CONTROL = b'if false && source_aot && platform != HostPlatform::WindowsX64 {'


def require(value, message):
    if not value:
        raise ValueError(message)


def record(path):
    path = Path(path)
    return {'path': str(path), 'size': path.stat().st_size,
            'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--base', type=Path, required=True)
    args = parser.parse_args()
    base = args.base.resolve()
    require(os.environ.get('GITHUB_ACTIONS') == 'true', 'temporary source control patch is CI-only')
    require(platform.system() == 'Linux' and platform.machine() == 'x86_64', 'Linux x64 only')
    require(platform.python_version() == '3.12.13', 'fixed CPython 3.12.13 required')
    require(base == Path(os.environ['RUNNER_TEMP']).resolve()/'direct-entry-build-perf', 'unexpected owned root')
    require(not base.is_relative_to(REPO), 'owned root must be outside checkout')
    base.mkdir(exist_ok=False)
    logs = base/'logs'; logs.mkdir()
    evidence = base/'source-evidence'; evidence.mkdir()
    write(base/'global.json', {'sdk': {'version': SDK, 'rollForward': 'disable', 'allowPrerelease': False}})
    (base/'NuGet.Config').write_text('<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources></configuration>\n', encoding='utf-8')
    python = Path(sys.executable).resolve()
    dotnet = Path(shutil.which('dotnet') or '').resolve()
    require(dotnet.is_file(), 'dotnet unavailable')
    env = dict(os.environ, PYTHON=str(python), DOTNET=str(dotnet),
               RUSTUP_TOOLCHAIN='1.99.1', RYUPY_RUNTIME_DIR=str(base/'tools'),
               CARGO_TARGET_DIR=str(base/'cargo-target'), PYTHONDONTWRITEBYTECODE='1',
               NUGET_PACKAGES=str(base/'nuget'), NuGetAudit='false')
    state = {'schema_version': 1, 'passed': False, 'base': str(base), 'repo': str(REPO),
             'python': str(python), 'dotnet': str(dotnet), 'nuget_packages': str(base/'nuget'), 'tools': {}, 'archives': {},
             'environment': {k: env[k] for k in ('PYTHON','DOTNET','RUSTUP_TOOLCHAIN',
                 'RYUPY_RUNTIME_DIR','CARGO_TARGET_DIR','PYTHONDONTWRITEBYTECODE','NUGET_PACKAGES','NuGetAudit')},
             'source_restored': False}

    def run(label, argv, cwd=base, timeout=1500):
        command = list(map(str, argv))
        with (logs/(label+'.stdout')).open('wb') as out, (logs/(label+'.stderr')).open('wb') as err:
            result = subprocess.run(command, cwd=cwd, env=env, stdout=out, stderr=err, timeout=timeout)
        write(logs/(label+'.status.json'), {'argv': command, 'cwd': str(cwd), 'exit': result.returncode})
        require(result.returncode == 0, label+' failed; see saved streams')
        return (logs/(label+'.stdout')).read_bytes()

    try:
        state['commit'] = run('commit', ['git','rev-parse','HEAD'], REPO).decode().strip()
        require(run('initial-diff', ['git','diff','--binary','HEAD'], REPO) == b'', 'checkout must be clean')
        require(run('sdk-version', [dotnet,'--version']).decode().strip() == SDK, 'SDK drift')
        require(run('rust-version', ['rustc','--version'], REPO).decode().startswith('rustc 1.99.1 '), 'Rust drift')
        run('python-version', [python,'-c','import platform; print(platform.python_version())'])
        tools = base/'tools'; tools.mkdir()
        run('runtime-build', [dotnet,'build',REPO/'runtime/RyuPy.Runtime/RyuPy.Runtime.csproj',
                              '-c','Release','-t:Rebuild','-nologo','--configfile',base/'NuGet.Config'])
        runtime = tools/'RyuPy.Runtime.dll'
        shutil.copy2(REPO/'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll', runtime)
        state['tools']['runtime'] = record(runtime)
        source = REPO/'crates/ryupy-driver/src/aot.rs'
        original = source.read_bytes()
        require(original.count(NEEDLE) == 1 and CONTROL not in original, 'control patch anchor must be unique')
        (evidence/'aot.before.rs').write_bytes(original)
        state['source_before'] = record(evidence/'aot.before.rs')
        try:
            for kind in ('direct','thin'):
                if kind == 'thin':
                    patched = original.replace(NEEDLE, CONTROL, 1)
                    source.write_bytes(patched)
                    (evidence/'aot.control.rs').write_bytes(patched)
                    (evidence/'control.patch').write_text(''.join(difflib.unified_diff(
                        original.decode().splitlines(True), patched.decode().splitlines(True),
                        fromfile='aot.rs/direct', tofile='aot.rs/thin-control')), encoding='utf-8')
                    state['control_patch'] = record(evidence/'control.patch')
                run(kind+'-cli-build', ['cargo','build','--release','--locked','-p','ryupy-cli'], REPO)
                dest = tools/kind/'ryupy'; dest.parent.mkdir()
                shutil.copy2(base/'cargo-target/release/ryupy', dest)
                state['tools'][kind] = record(dest)
        finally:
            source.write_bytes(original)
            (evidence/'aot.after.rs').write_bytes(source.read_bytes())
            state['source_after'] = record(evidence/'aot.after.rs')
            state['source_restored'] = source.read_bytes() == original
            write(base/'setup.json', state)
        require(state['source_restored'], 'source restore failed')
        require(run('final-diff', ['git','diff','--binary','HEAD'], REPO) == b'', 'unexpected checkout change')
        require(record(runtime) == state['tools']['runtime'], 'runtime changed between tools')
        # 编译器已冻结，仅回收本轮独占 Cargo 缓存；NuGet 是测量条件，必须保留。
        cache = base/'cargo-target'
        require(cache.parent == base and cache.is_dir() and not cache.is_symlink(), 'unexpected Cargo cache root')
        files = [p for p in cache.rglob('*') if p.is_file() and not p.is_symlink()]
        state['cargo_cache_cleanup'] = {'path':str(cache), 'files':len(files),
                                        'bytes':sum(p.stat().st_size for p in files), 'removed':False}
        shutil.rmtree(cache)
        state['cargo_cache_cleanup']['removed'] = not cache.exists()
        require(state['cargo_cache_cleanup']['removed'], 'owned Cargo cache cleanup failed')
        for name, path in [('python',python),('dotnet',dotnet)]:
            state['tools'][name] = record(path)
        # 只下载摘要锁定的 PyPI 上游包；解包拒绝链接与设备成员。
        locked = {}
        for line in (REPO/'tests/projects/lock.tsv').read_text().splitlines():
            if not line or line.startswith('#'): continue
            name, version, filename, digest, url = line.split('\t')
            locked[name] = {'version':version,'filename':filename,'sha256':digest,'url':url}
        binary = json.loads((REPO/'tests/aot/deployment_binaryornot/inputs.json').read_text())['packages']
        for name, row in binary.items():
            require(all(row['archive'][key] == locked[name][key] for key in ('filename','sha256','url'))
                    and row['version'] == locked[name]['version'], 'input locks disagree: '+name)
        sources = base/'sources'; sources.mkdir()
        roots = {}
        for name in ('pyfiglet','binaryornot','chardet'):
            row = locked[name]
            require(urlparse(row['url']).scheme == 'https' and urlparse(row['url']).hostname == 'files.pythonhosted.org', 'unapproved upstream')
            archive = sources/row['filename']
            require(archive.parent == sources, 'invalid archive filename')
            with urllib.request.urlopen(row['url'], timeout=120) as response:
                require(urlparse(response.url).hostname == 'files.pythonhosted.org', 'unexpected download redirect')
                with archive.open('xb') as output: shutil.copyfileobj(response, output)
            require(record(archive)['sha256'] == row['sha256'], 'archive digest mismatch: '+name)
            dest = sources/name; dest.mkdir()
            with tarfile.open(archive) as tar:
                members = tar.getmembers()
                require(all(m.isdir() or m.isfile() for m in members), 'archive contains non-regular member')
                tar.extractall(dest, filter='data')
            roots[name] = dest/(binary[name]['source_root'] if name in binary else name+'-'+row['version'])
            state['archives'][name] = dict(row, **{'download':record(archive),'source_root':str(roots[name])})
        projects = base/'projects'; projects.mkdir()
        run('prepare-pyfiglet', [python,'-B',REPO/'tests/aot/deployment_project/prepare.py',
                                projects/'pyfiglet','--package',roots['pyfiglet']])
        run('prepare-binaryornot', [python,'-B',REPO/'tests/aot/deployment_binaryornot/prepare.py',
                                   projects/'binaryornot','--binaryornot',roots['binaryornot'],'--chardet',roots['chardet']])
        state['projects'] = {name:str(projects/name) for name in ('pyfiglet','binaryornot')}
        state['passed'] = True
    finally:
        write(base/'setup.json', state)
    print(base)


if __name__ == '__main__':
    main()
