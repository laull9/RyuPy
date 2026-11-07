#!/usr/bin/env python3
"""仅供官方 hosted Linux CI：核验固定下载后安装隔离工具；不改系统安全设置。"""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import signal
import tarfile

HERE = Path(__file__).resolve().parent
ROOT = Path(os.environ['RUNNER_TEMP']) / 'wcwidth-aot-tools'
ROOT.mkdir(exist_ok=False)


def run(args):
    print(json.dumps(args), flush=True)
    process = subprocess.Popen(args, start_new_session=True)
    try:
        code = process.wait(timeout=300)
    except subprocess.TimeoutExpired:
        try:
            os.killpg(process.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass
        process.wait()
        raise
    if code:
        raise subprocess.CalledProcessError(code, args)


for item in json.loads((HERE / 'downloads.json').read_text()):
    archive = ROOT / item['filename']
    run(['curl', '--fail', '--location', '--show-error', '--max-time', '180',
         '--proto', '=https', '--proto-redir', '=https', item['url'], '-o', str(archive)])
    actual = hashlib.new(item['algorithm'], archive.read_bytes()).hexdigest()
    if actual != item['digest']:
        raise RuntimeError(f"下载哈希不匹配：{item['filename']}")
    folder = ROOT / item.get('component', item['kind'])
    folder.mkdir()
    with tarfile.open(archive) as tar:
        tar.extractall(folder, filter='data')
    if item['kind'] == 'rust':
        installer = next(folder.glob('*/install.sh'))
        run(['sh', str(installer), '--prefix=' + str(ROOT / 'rust'), '--disable-ldconfig'])

with open(os.environ['GITHUB_PATH'], 'a') as f:
    f.write(str(ROOT / 'rust/bin') + '\n' + str(ROOT / 'dotnet') + '\n')
with open(os.environ['GITHUB_ENV'], 'a') as f:
    for key, value in {
        'DOTNET_ROOT': ROOT / 'dotnet', 'DOTNET': ROOT / 'dotnet/dotnet',
        'PYTHON': ROOT / 'cpython/python/bin/python3',
        'WCWIDTH_SOURCE': ROOT / 'wcwidth/wcwidth-0.2.13',
    }.items():
        f.write(f'{key}={value}\n')
