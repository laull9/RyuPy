#!/usr/bin/env python3
"""用 LaunchServices 启动已删除源码的 macOS NativeAOT 应用包。"""
import argparse
from pathlib import Path
import plistlib
import subprocess
import sys
from tempfile import TemporaryDirectory


def main():
    root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--cli', type=Path, default=root / 'target/debug/ryupy')
    args = parser.parse_args()
    assert sys.platform == 'darwin', '该用例需要真实 macOS 桌面'
    cli = args.cli.resolve(strict=True)
    with TemporaryDirectory(prefix='ryupy-macos-app-') as temporary:
        work = Path(temporary).resolve()
        assert not work.is_relative_to(root)
        source = work / 'hello.py'
        source.write_text(
            'import sys\nwith open(sys.argv[1], "w", encoding="utf-8") as out:\n'
            '    out.write("LaunchServices NativeAOT")\n', encoding='utf-8')
        command = [cli, source, '--native', '--mode', 'app', '--app-name',
                   '窗口 & 编辑', '-o', work / 'out']
        subprocess.run(command, check=True, cwd=work, timeout=180)
        bundle = work / 'out/窗口 & 编辑.app'
        plist = plistlib.loads((bundle / 'Contents/Info.plist').read_bytes())
        assert plist['CFBundleExecutable'] == 'hello' and plist['CFBundleName'] == '窗口 & 编辑'
        assert (bundle / 'Contents/MacOS/internal/.ryupy-layout.json').is_file()
        collision = subprocess.run(command, cwd=work, capture_output=True, timeout=15)
        assert collision.returncode != 0 and bundle.is_dir(), collision.stderr
        moved = work / '搬迁 空格/窗口测试.app'
        moved.parent.mkdir()
        bundle.rename(moved)
        source.unlink()
        marker = work / 'startup.txt'
        subprocess.run(['/usr/bin/open', '-W', '-n', '-a', moved, '--args', marker],
                       check=True, cwd=work, timeout=30)
        assert marker.read_text(encoding='utf-8') == 'LaunchServices NativeAOT'
        print('LaunchServices 启动、中文路径搬迁及同名保护通过')


if __name__ == '__main__':
    main()
