"""成对门只恢复已锁官方SDK；不重复安装Rust/CP、不改变系统配置。"""
import hashlib
import json
import os
from pathlib import Path
import sys
import tarfile

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parents[2] / 'tests/aot/wcwidth_ci'))
from prepare import run, selected_downloads


def prepare():
    root = Path(os.environ['RUNNER_TEMP']) / 'wcwidth-paired-sdk'
    root.mkdir(exist_ok=False)
    evidence = HERE.parents[2] / 'target/wcwidth-paired-prepare'
    evidence.mkdir(parents=True, exist_ok=False)
    items = [i for i in selected_downloads() if i['kind'] == 'dotnet']
    if len(items) != 1 or items[0]['version'] != '10.0.403':
        raise RuntimeError('固定SDK清单改变')
    item = items[0]
    archive = root / item['filename']
    run(['curl', '--fail', '--location', '--show-error', '--max-time', '180', '--proto', '=https',
         '--proto-redir', '=https', item['url'], '-o', str(archive)])
    digest = hashlib.new(item['algorithm'], archive.read_bytes()).hexdigest()
    (evidence / 'download.json').write_text(json.dumps(dict(item, actual_digest=digest), indent=2) + '\n')
    if digest != item['digest']:
        raise RuntimeError('官方SDK hash不符')
    sdk = root / 'dotnet'
    sdk.mkdir()
    with tarfile.open(archive) as tar:
        tar.extractall(sdk, filter='data')
    with open(os.environ['GITHUB_ENV'], 'a') as f:
        f.write(f'DOTNET_ROOT={sdk}\nDOTNET={sdk}/dotnet\n')
    with open(os.environ['GITHUB_PATH'], 'a') as f:
        f.write(str(sdk) + '\n')


if __name__ == '__main__':
    prepare()
