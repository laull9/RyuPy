#!/usr/bin/env python3
"""显式准备固定PBS安装归档：版本、构建号与平台共同冻结，供shim及运行臂使用同一发行。"""
import argparse
from pathlib import Path
import shutil
import subprocess
import sys
import tarfile
import tempfile
import urllib.request

sys.dont_write_bytecode = True
from support import ROOT, VERSION, rid

ASSETS = {
    'osx-arm64': 'aarch64-apple-darwin',
    'linux-x64': 'x86_64-unknown-linux-gnu',
    'win-x64': 'x86_64-pc-windows-msvc',
}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--destination', required=True, type=Path)
    parser.add_argument('--github-env', type=Path)
    args = parser.parse_args()
    destination = args.destination.resolve()
    assert not destination.is_relative_to(ROOT), '固定发行必须准备在仓库外'
    assert not destination.exists(), f'发行目标已存在：{destination}'
    destination.parent.mkdir(parents=True, exist_ok=True)
    target = rid()
    asset = f'cpython-{VERSION}%2B20260623-{ASSETS[target]}-install_only.tar.gz'
    url = f'https://github.com/astral-sh/python-build-standalone/releases/download/20260623/{asset}'
    with tempfile.TemporaryDirectory(prefix='ryupy-pbs-', dir=destination.parent) as temporary:
        workspace = Path(temporary)
        archive = workspace / 'install.tar.gz'
        with urllib.request.urlopen(url, timeout=60) as response, archive.open('wb') as output:
            shutil.copyfileobj(response, output)
        with tarfile.open(archive, 'r:gz') as package:
            package.extractall(workspace / 'extracted', filter='data')
        source = workspace / 'extracted/python'
        marker = source / 'BUILD'
        if marker.exists():
            actual = marker.read_text(encoding='utf-8').strip()
            assert actual == '20260623', f'PBS构建号不符：期望20260623，实际{actual!r}'
        executable = source / ('python.exe' if target == 'win-x64' else 'bin/python3.12')
        result = subprocess.run([executable, '-I', '-c', 'import platform; print(platform.python_version())'],
                                capture_output=True, timeout=60, check=True)
        assert result.stdout.strip() == VERSION.encode(), result.stdout
        # 官方install_only不含BUILD；仅由确切发行URL获取并验证版本后写入来源凭据。
        marker.write_text('20260623\n', encoding='utf-8')
        shutil.move(source, destination)
    executable = destination / ('python.exe' if target == 'win-x64' else 'bin/python3.12')
    if args.github_env:
        with args.github_env.open('a', encoding='utf-8') as output:
            output.write(f'CPYTHON_HOME={destination}\nPYTHON_BIN={executable}\n')
    print(f'固定发行：{target} CPython{VERSION} PBS20260623 {destination}')


if __name__ == '__main__':
    main()
