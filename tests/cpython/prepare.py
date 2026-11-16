#!/usr/bin/env python3
"""显式准备仓库外的固定官方发行metadata/许可与NumPy wheel；产品运行不下载依赖。"""
import argparse
from pathlib import Path
import shutil
import subprocess
import sys

# 验收驱动本身也不在源码目录留下字节码缓存。
sys.dont_write_bytecode = True
import urllib.request

from support import ROOT, environment, require_python, rid

ARCHIVE = 'cpython-3.12.13-20260623-aarch64-apple-darwin-pgo-lto-full.tar.zst'
URL = ('https://github.com/astral-sh/python-build-standalone/releases/download/20260623/'
       'cpython-3.12.13%2B20260623-aarch64-apple-darwin-pgo%2Blto-full.tar.zst')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--python', default=sys.executable)
    parser.add_argument('--cache', type=Path, default=Path.home() / '.cache/ryupy/cpython-provider')
    parser.add_argument('--offline', action='store_true')
    args = parser.parse_args()
    assert rid() == 'osx-arm64', '其他平台须单独冻结官方资产与wheel'
    cache = args.cache.resolve()
    assert not cache.is_relative_to(ROOT), '依赖缓存必须位于仓库外'
    cache.mkdir(parents=True, exist_ok=True)
    require_python(args.python, cache, environment())
    archive = cache / ARCHIVE
    if not archive.is_file():
        assert not args.offline, '离线缓存缺固定PBS full archive'
        download = cache / (ARCHIVE + '.download')
        try:
            urllib.request.urlretrieve(URL, download)
            download.replace(archive)
        finally:
            download.unlink(missing_ok=True)
    metadata = cache / 'pbs-20260623-metadata'
    metadata.mkdir(exist_ok=True)
    subprocess.run(['tar', '-xf', archive, '-C', metadata, 'python/PYTHON.json', 'python/licenses'], check=True)
    numpy = cache / 'numpy-2.3.5-cp312-osx-arm64'
    if not (numpy / 'numpy-2.3.5.dist-info/METADATA').is_file():
        assert not args.offline, '离线缓存缺固定NumPy wheel安装输入'
        subprocess.run([shutil.which('uv'), 'pip', 'install', '--python', args.python,
                        '--target', numpy, '--only-binary', ':all:', '--no-deps',
                        '-r', ROOT / 'tests/cpython/requirements.txt'], check=True)
    print('distribution-metadata:', metadata / 'python')
    print('numpy-root:', numpy)


if __name__ == '__main__':
    main()
