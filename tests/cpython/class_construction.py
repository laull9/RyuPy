#!/usr/bin/env python3
"""C1 三版本真实类桥：同次 oracle、选中 shim、30项类协议与候选/默认两条方法路径。"""
import argparse
from pathlib import Path
import shutil
import sys

sys.dont_write_bytecode = True
from support import ROOT, checked, environment, execute, external_workspace

CASES = ('native_class_construction', 'native_class_construction_failure', 'native_class_metadata', 'native_class_prepare_metadata', 'native_class_construction_reentry')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--python', action='append', required=True, help='选中 CPython 可执行文件，可重复指定')
    args = parser.parse_args()
    assert sys.platform in ('darwin', 'linux'), '本多版本驱动支持macOS/Linux，Windows由独立平台门验收'
    root = external_workspace('ryupy-class-construction-')
    env = environment()
    try:
        artifacts = root / 'artifacts'
        checked(execute(['dotnet', 'build', ROOT / 'runtime/RyuPy.CPython.Tests/RyuPy.CPython.Tests.csproj',
                         '-c', 'Release', '--artifacts-path', artifacts, '-nologo', '-v', 'q'],
                        cwd=ROOT, env=env), '真实类桥构建')
        dll = artifacts / 'bin/RyuPy.CPython.Tests/release/RyuPy.CPython.Tests.dll'
        assert dll.is_file(), dll
        for index, python in enumerate(args.python):
            probe = checked(execute([python, '-I', '-c',
                                    'import sys,platform; print(sys.base_prefix); print(platform.python_version())'],
                                    cwd=root, env=env), '选中发行')
            home, version = probe.stdout.decode().splitlines()
            series = '.'.join(version.split('.')[:2])
            selected = root / ('python-' + str(index))
            selected.mkdir()
            shim_name = 'libryupy_cpython.dylib' if sys.platform == 'darwin' else 'libryupy_cpython.so'
            checked(execute(['sh', ROOT / 'runtime/RyuPy.CPython/native/build.sh', home, selected, series],
                            cwd=root, env=env), '真实shim构建')
            oracle_env = dict(env, PYTHONPATH=str(ROOT / 'tests/cpython/fixtures'))
            for name in CASES:
                checked(execute([python, '-B', ROOT / 'tests/cpython/cases' / (name + '.py')],
                                cwd=selected, env=oracle_env), version + ' oracle ' + name)
            for mode in ('0', '1'):
                run_env = dict(env, RYUPY_CPYTHON_TEST_HOME=home, RYUPY_CPYTHON_TEST_VERSION=version,
                               RYUPY_CPYTHON_TEST_REPOSITORY=str(ROOT), RYUPY_CPYTHON_TEST_SHIM=str(selected / shim_name),
                               RYUPY_CPYTHON_NATIVE_METHODS=mode)
                result = checked(execute(['dotnet', 'vstest', dll,
                                          '--TestCaseFilter:FullyQualifiedName~CPythonClassBridgeTests'],
                                         cwd=selected, env=run_env), version + ' real bridge mode=' + mode)
                print(version, 'mode=' + mode, result.stdout.decode().strip().splitlines()[-1], flush=True)
    finally:
        shutil.rmtree(root)


if __name__ == '__main__':
    main()
