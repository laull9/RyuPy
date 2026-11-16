#!/usr/bin/env python3
"""仅供官方 hosted Linux CI：核验固定下载后安装隔离工具；不改系统安全设置。"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import signal
import tarfile

HERE = Path(__file__).resolve().parent


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


def selected_downloads(rust_only=False):
    """复用唯一固定下载清单；默认保留全部既有组件和原顺序。"""
    items = json.loads((HERE / 'downloads.json').read_text())
    return [item for item in items if not rust_only or item['kind'] == 'rust']


def prepare(rust_only=False):
    ROOT = Path(os.environ['RUNNER_TEMP']) / ('wcwidth-rust-tools' if rust_only else 'wcwidth-aot-tools')
    ROOT.mkdir(exist_ok=False)
    identities = []
    for item in selected_downloads(rust_only):
        archive = ROOT / item['filename']
        run(['curl', '--fail', '--location', '--show-error', '--max-time', '180',
             '--proto', '=https', '--proto-redir', '=https', item['url'], '-o', str(archive)])
        actual = hashlib.new(item['algorithm'], archive.read_bytes()).hexdigest()
        identities.append(dict(item, actual_digest=actual, verified=actual == item['digest']))
        (ROOT / 'download-identity.json').write_text(json.dumps(identities, indent=2) + '\n')
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
        f.write(str(ROOT / 'rust/bin') + '\n')
        if not rust_only:
            f.write(str(ROOT / 'dotnet') + '\n')
    with open(os.environ['GITHUB_ENV'], 'a') as f:
        values = {'TRACE_RUST_ROOT': ROOT / 'rust'} if rust_only else {
            'DOTNET_ROOT': ROOT / 'dotnet', 'DOTNET': ROOT / 'dotnet/dotnet',
            'PYTHON': ROOT / 'cpython/python/bin/python3',
            'WCWIDTH_SOURCE': ROOT / 'wcwidth/wcwidth-0.2.13',
        }
        for key, value in values.items():
            f.write(f'{key}={value}\n')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--rust-only', action='store_true')
    prepare(parser.parse_args().rust_only)
