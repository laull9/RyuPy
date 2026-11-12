"""仅显式hosted setup安装已锁官方包；成本不混入Native编译/启动。"""
import json
import os
from pathlib import Path
import subprocess
import tarfile
import urllib.request
from measure import require, save, sha

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]


def main():
    require(os.environ.get('GITHUB_ACTIONS') == 'true' and
            os.environ.get('RUNNER_ENVIRONMENT') == 'github-hosted', '只允许显式hosted工具准备')
    out = Path(os.environ['RUNNER_TEMP'])/'standalone-bench-setup'; out.mkdir(exist_ok=False)
    python = Path(os.environ['PYTHON']).resolve()
    lock = json.loads((HERE/'nuitka-lock.json').read_text())
    items = json.loads((HERE/'build-dependencies.json').read_text()) + [lock]
    recipe = json.loads((ROOT/'tests/aot/wcwidth_ci/project-source-recovery.json').read_text())
    package = next(x for x in recipe['packages'] if x['package'] == 'pyfiglet')
    identities = []
    for item in items+[package]:
        path = out/item['filename']
        with urllib.request.urlopen(item['url'], timeout=120) as response: path.write_bytes(response.read())
        require(sha(path) == item['sha256'], '官方包hash不符')
        identities.append({'file': str(path), 'url': item['url'], 'sha256': sha(path)})
        save(out/'downloads.json', identities)
    env = dict(os.environ, PYTHONDONTWRITEBYTECODE='1', PYTHONNOUSERSITE='1')
    def command(label, args):
        save(out/(label+'.argv.json'), args)
        with (out/(label+'.stdout')).open('xb') as stdout, (out/(label+'.stderr')).open('xb') as stderr:
            result = subprocess.run(args, env=env, stdout=stdout, stderr=stderr, timeout=300)
        save(out/(label+'.json'), {'exit': result.returncode}); require(result.returncode == 0, label+'失败')
    command('build-dependencies', [str(python), '-m', 'pip', 'install', '--no-index', '--no-deps']+
            [str(out/x['filename']) for x in items[:-1]])
    command('nuitka-install', [str(python), '-m', 'pip', 'install', '--no-index', '--no-deps',
                             '--no-build-isolation', str(out/lock['filename'])])
    command('nuitka-version', [str(python), '-B', '-m', 'nuitka', '--version'])
    require((out/'nuitka-version.stdout').read_text().splitlines()[0].strip() == lock['version'], '安装版本不符')
    folder = Path(subprocess.check_output([python, '-B', '-c', 'import nuitka, pathlib; print(pathlib.Path(nuitka.__file__).parent)'], env=env, text=True).strip())
    save(out/'nuitka-tree.json', {str(p):sha(p) for p in sorted(folder.rglob('*')) if p.is_file()})
    store = ROOT/'third_party/projects'/package['store_directory']; store.mkdir(parents=True, exist_ok=False)
    with tarfile.open(out/package['filename']) as archive: archive.extractall(store, filter='data')
    with open(os.environ['GITHUB_ENV'], 'a') as f:
        f.write('NUITKA_ARCHIVE='+str(out/lock['filename'])+'\n')
        f.write('NUITKA_TREE_RECEIPT='+str(out/'nuitka-tree.json')+'\n')


if __name__ == '__main__': main()
