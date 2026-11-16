#!/usr/bin/env python3
"""C1 原生方法描述符多版本门：同次oracle、真实shim、小宿主，输出仓库外自动清理。"""
import argparse
from pathlib import Path
import shutil
import sys

sys.dont_write_bytecode = True
from support import ROOT, checked, environment, execute, external_workspace

FIXTURES = ROOT / 'tests/cpython/native_methods'
PACKAGES = ROOT / 'tests/cpython/fixtures'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--python', action='append', required=True, help='显式选中发行，可重复指定3.9–3.15')
    args = parser.parse_args()
    assert sys.platform in ('darwin', 'linux'), '原生小宿主驱动支持macOS/Linux；Windows独立验收'
    root = external_workspace('ryupy-native-methods-')
    env = environment()
    try:
        for index, python in enumerate(args.python):
            probe = checked(execute([python, '-I', '-c',
                                    'import sys,platform; print(sys.base_prefix); print(f"{sys.version_info.major}.{sys.version_info.minor}"); print(platform.python_version())'],
                                    cwd=root, env=env), '选中发行')
            home, series, version = probe.stdout.decode().splitlines()
            home = Path(home)
            output = root / str(index)
            output.mkdir()
            oracle_code = (f'import sys; sys.path[:0] = [{str(FIXTURES)!r}, {str(PACKAGES)!r}]; '
                           'import fixture; fixture.Subject.sample=fixture.target; fixture.test(False)')
            oracle = checked(execute([python, '-I', '-B', '-c', oracle_code], cwd=root, env=env), '同次oracle')
            checked(execute(['sh', ROOT / 'runtime/RyuPy.CPython/native/build.sh', home, output, series],
                            cwd=root, env=env), '真实shim构建')
            library = home / 'lib'
            command = [shutil.which('cc'), '-Wall', '-Wextra', '-Werror',
                       '-I' + str(home / 'include' / ('python' + series)), FIXTURES / 'host.c',
                       '-L' + str(output), '-lryupy_cpython', '-L' + str(library), '-lpython' + series,
                       '-Wl,-rpath,' + str(output), '-Wl,-rpath,' + str(library), '-o', output / 'host']
            checked(execute(command, cwd=root, env=env), '小宿主构建')
            stdlib = library / ('python' + series)
            native = checked(execute([output / 'host', home, stdlib, stdlib / 'lib-dynload', PACKAGES,
                                      FIXTURES / 'fixture.py'], cwd=root, env=env), '真实描述符消费者')
            assert (native.stdout, native.stderr) == (oracle.stdout, oracle.stderr), (version, native, oracle)
            print(version, 'oracle/native binding/metadata/weakref/GC passed', flush=True)
    finally:
        shutil.rmtree(root)


if __name__ == '__main__':
    main()
