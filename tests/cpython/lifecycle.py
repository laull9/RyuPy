#!/usr/bin/env python3
"""在已生成分发上独立运行生命周期门；每个测试使用全新进程与临时目录。"""
import argparse
from pathlib import Path
import shutil
import sys

# 验收驱动本身也不在源码目录留下字节码缓存。
sys.dont_write_bytecode = True

from support import ROOT, checked, environment, execute, external_workspace


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dist', type=Path, required=True, help='含ryupy-cpython.json和bridge_fixture的已生成JIT目录')
    parser.add_argument('--runtime', type=Path, help='默认使用同一分发内匹配版本的Runtime')
    parser.add_argument('--keep', action='store_true')
    args = parser.parse_args()
    distribution = args.dist.resolve()
    runtime = args.runtime.resolve() if args.runtime else distribution
    root = external_workspace('ryupy-cpython-lifetime-')
    env = environment()
    try:
        dotnet = shutil.which('dotnet')
        output = root / 'host'
        checked(execute([dotnet, 'build', ROOT / 'tests/cpython/lifecycle/Lifecycle.csproj',
                         '-c', 'Release', '-o', output,
                         '-p:BaseIntermediateOutputPath=' + str(root / 'obj') + '/',
                         '-p:RuntimeDirectory=' + str(runtime),
                         '-p:BridgeDirectory=' + str(distribution)], cwd=root, env=env), '生命周期宿主构建')
        # 配置的相对分发根仍以原dist解析；不会复制开发机venv。
        failures = []
        for case in ('steady-state', 'exception-release', 'numpy-lifetime', 'disposal', 'late-release', 'mixed-limit'):
            cwd = root / case
            cwd.mkdir()
            result = execute([dotnet, output / 'Lifecycle.dll', distribution / 'ryupy-cpython.json', case], cwd=cwd, env=env)
            print(case, result.returncode, result.stdout.decode(), result.stderr.decode(), flush=True)
            if result.returncode != 0:
                failures.append(case)
        assert not failures, failures
    finally:
        if args.keep:
            print('调试临时目录：', root)
        else:
            shutil.rmtree(root)


if __name__ == '__main__':
    main()
