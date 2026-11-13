"""固定工具与锁定PyPI真实项目准备；独占SDK根，不改源码/上游/默认策略。"""
import argparse
import json
import os
from pathlib import Path
import platform
import shutil
import sys
import tarfile
import urllib.request
from urllib.parse import urlparse
from common import capture, digest, require, save

REPO = Path(__file__).resolve().parents[3]


def main():
    parser = argparse.ArgumentParser(); parser.add_argument('--base', type=Path, required=True)
    base = parser.parse_args().base.resolve()
    require(os.environ.get('GITHUB_ACTIONS') == 'true', '仅在隔离CI准备')
    require(base == Path(os.environ['RUNNER_TEMP']).resolve()/'distribution-verify', '非预期owned根')
    require(not base.is_relative_to(REPO), '证据不能在仓库内')
    require(platform.python_version() == '3.12.13', '须固定CPython3.12.13')
    base.mkdir(exist_ok=False); tools = base/'tools'; tools.mkdir()
    save(base/'global.json', {'sdk': {'version': '10.0.403', 'rollForward': 'disable', 'allowPrerelease': False}})
    python = Path(sys.executable).resolve(); dotnet = shutil.which('dotnet'); require(dotnet, '缺dotnet')
    env = dict(os.environ, DOTNET=dotnet, PYTHON=str(python), RYUPY_RUNTIME_DIR=str(tools),
               CARGO_TARGET_DIR=str(base/'cargo-target'), RUSTUP_TOOLCHAIN='1.99.1')
    result = {'passed': False, 'base': str(base), 'python': str(python), 'dotnet': dotnet, 'archives': {}}
    def run(label, command, cwd=base, timeout=1800):
        value = capture(base/'logs'/label, command, cwd, env, timeout)
        require(value.returncode == 0, '准备失败：'+label)
        return value.stdout
    try:
        result['commit'] = run('commit', ['git', 'rev-parse', 'HEAD'], REPO).decode().strip()
        run('harness-unit', [python, '-B', '-m', 'unittest', 'discover', '-s', str(REPO/'tests/aot/distribution_verify'), '-p', 'test_contracts.py'], REPO)
        require(run('diff-before', ['git', 'diff', '--binary', 'HEAD'], REPO) == b'', 'checkout不干净')
        require(run('sdk-version', [dotnet, '--version']).decode('utf-8-sig').strip() == '10.0.403', 'SDK漂移')
        require(run('rust-version', ['rustc', '--version'], REPO).decode().startswith('rustc 1.99.1 '), 'Rust漂移')
        run('runtime', [dotnet, 'build', REPO/'runtime/RyuPy.Runtime/RyuPy.Runtime.csproj', '-c', 'Release', '-t:Rebuild', '-nologo'])
        run('cli', ['cargo', 'build', '--release', '--locked', '-p', 'ryupy-cli'], REPO)
        cli = tools/('ryupy.exe' if os.name == 'nt' else 'ryupy')
        shutil.copy2(base/'cargo-target/release'/cli.name, cli)
        runtime = tools/'RyuPy.Runtime.dll'
        shutil.copy2(REPO/'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll', runtime)
        result['cli'] = str(cli)
        result['tools'] = {str(p): {'sha256': digest(p), 'bytes': p.stat().st_size}
                           for p in [cli, runtime, python, Path(dotnet)]}
        locked = {}
        for line in (REPO/'tests/projects/lock.tsv').read_text().splitlines():
            if not line or line.startswith('#'): continue
            name, version, filename, sha, url = line.split('\t')
            locked[name] = {'version': version, 'filename': filename, 'sha256': sha, 'url': url}
        binary = json.loads((REPO/'tests/aot/deployment_binaryornot/inputs.json').read_text())['packages']
        for name, row in binary.items():
            require(row['version'] == locked[name]['version'] and all(row['archive'][k] == locked[name][k]
                    for k in ['filename', 'sha256', 'url']), '上游锁冲突')
        sources = base/'sources'; sources.mkdir(); roots = {}
        for name in ['pyfiglet', 'binaryornot', 'chardet']:
            row = locked[name]; url = urlparse(row['url'])
            require(url.scheme == 'https' and url.hostname == 'files.pythonhosted.org', '非批准上游')
            archive = sources/row['filename']; require(archive.parent == sources, '非法文件名')
            with urllib.request.urlopen(row['url'], timeout=120) as response:
                require(urlparse(response.url).hostname == 'files.pythonhosted.org', '非预期重定向')
                with archive.open('xb') as output: shutil.copyfileobj(response, output)
            require(digest(archive) == row['sha256'], '上游摘要不符')
            dest = sources/name; dest.mkdir()
            with tarfile.open(archive) as tar:
                require(all(m.isfile() or m.isdir() for m in tar.getmembers()), '归档含链接/设备')
                tar.extractall(dest, filter='data')
            roots[name] = dest/(binary[name]['source_root'] if name in binary else name+'-'+row['version'])
            result['archives'][name] = row
        projects = base/'projects'; projects.mkdir()
        run('prepare-pyfiglet', [python, '-B', REPO/'tests/aot/deployment_project/prepare.py',
                                projects/'pyfiglet', '--package', roots['pyfiglet']])
        run('prepare-binaryornot', [python, '-B', REPO/'tests/aot/deployment_binaryornot/prepare.py',
            projects/'binaryornot', '--binaryornot', roots['binaryornot'], '--chardet', roots['chardet']])
        require(run('diff-after', ['git', 'diff', '--binary', 'HEAD'], REPO) == b'', '准备改变checkout')
        result['passed'] = True
    finally:
        save(base/'setup.json', result)


if __name__ == '__main__': main()
