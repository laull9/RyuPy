#!/usr/bin/env python3
"""C0 原生逐帧审计门：真实启动/运行/关闭链、禁用及错误版本负例，输出在仓库外清理。"""
import argparse
from pathlib import Path
import shutil
import sys

sys.dont_write_bytecode = True
from support import ROOT, checked, environment, execute, external_workspace, require_python

FIXTURES = ROOT / 'tests/cpython/execution_audit'


def build(python, root, env):
    """shim 与手写原生宿主始终链接同一选中发行，禁止宿主 Python 版本混用。"""
    probe = checked(execute([python, '-I', '-c',
                            'import sys; print(sys.base_prefix); print(f"{sys.version_info.major}.{sys.version_info.minor}")'],
                            cwd=root, env=env), '选中发行')
    home, version = probe.stdout.decode().splitlines()
    home = Path(home)
    output = root / ('native-' + version)
    output.mkdir()
    checked(execute(['sh', ROOT / 'runtime/RyuPy.CPython/native/build.sh', home, output, version],
                    cwd=root, env=env), '审计 shim 构建')
    library = home / 'lib'
    command = [shutil.which('cc'), '-Wall', '-Wextra', '-Werror',
               '-I' + str(home / 'include' / ('python' + version)),
               FIXTURES / 'host.c', '-L' + str(output), '-lryupy_cpython',
               '-L' + str(library), '-lpython' + version,
               '-Wl,-rpath,' + str(output), '-Wl,-rpath,' + str(library),
               '-o', output / 'host']
    checked(execute(command, cwd=root, env=env), '原生审计宿主构建')
    return [output / 'host', home, library / ('python' + version),
            library / ('python' + version) / 'lib-dynload', FIXTURES / 'fixture.py']


def read_frames(stdout):
    """原生快照按阶段/文件/函数聚合，独立复核总计与逐行计数一致。"""
    lines = stdout.decode().splitlines()
    totals = next(line for line in lines if line.startswith('totals '))
    startup, runtime, shutdown, installed, finalized, dropped, failures = map(int, totals.split()[1:])
    frames = []
    for line in lines:
        if not line.startswith('frame '):
            continue
        tag, phase, count, location = line.split(' ', 3)
        function, file = location.split('\t', 1)
        frames.append((int(phase), int(count), function, file))
    for phase, count in enumerate((startup, runtime, shutdown)):
        assert count == sum(row[1] for row in frames if row[0] == phase), (phase, count)
    return (startup, runtime, shutdown, installed, finalized, dropped, failures), frames


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--python', required=True, help='固定 CPython 3.12.13 可执行文件')
    parser.add_argument('--unsupported-python', help='其他版本发行，验证开启时初始化前拒绝')
    args = parser.parse_args()
    assert sys.platform in ('darwin', 'linux'), '本原生宿主驱动当前支持 macOS/Linux；Windows 需独立验收'
    env = environment()
    root = external_workspace('ryupy-execution-audit-')
    try:
        require_python(args.python, root, env)
        command = build(args.python, root, env)
        observed = checked(execute(command + ['enabled'], cwd=root, env=env), '真实逐帧执行')
        totals, frames = read_frames(observed.stdout)
        assert all(count > 0 for count in totals[:3]), totals
        assert totals[3:] == (1, 1, 0, 0), totals
        expected = {(0, 'bootstrap_helper'): 1, (1, 'direct_outer'): 1,
                    (1, 'direct_leaf'): 2, (1, 'callback_leaf'): 1,
                    # 3.12 的 RETURN_GENERATOR 建立对象一次，随后两次yield与最终StopIteration各进入一次。
                    (1, 'raising_leaf'): 1, (1, 'resumed_generator'): 4,
                    (1, 'thread_leaf'): 1, (2, 'shutdown_leaf'): 1}
        for (phase, function), count in expected.items():
            found = sum(row[1] for row in frames if row[0] == phase and row[2] == function)
            assert found == count, (phase, function, count, found)
        assert any(row[0] == 0 and row[3].startswith('<frozen ') for row in frames), frames[:5]
        assert b'audit shutdown observed\n' in observed.stdout, observed.stdout
        for mode in ('disabled', 'late-enable'):
            result = checked(execute(command + [mode], cwd=root, env=env), mode)
            disabled, disabled_frames = read_frames(result.stdout)
            assert disabled == (0, 0, 0, 0, 0, 0, 0) and not disabled_frames, disabled
            assert b'audit shutdown observed\n' in result.stdout, result.stdout
            if mode == 'late-enable':
                assert b'must be enabled once before initialization' in result.stdout, result.stdout
        if args.unsupported_python:
            other = build(args.unsupported_python, root, env)
            result = execute(other + ['enabled'], cwd=root, env=env)
            assert result.returncode == 3, result
            assert b'execution audit requires exact CPython 3.12.13 release/GIL shim' in result.stderr, result.stderr
            # 同一不支持审计的发行继续走默认宿主，证明新布局门只作用于显式审计。
            checked(execute(other + ['disabled'], cwd=root, env=env), '其他发行默认宿主')
        print('C0 native frame audit passed:', dict(zip(('startup', 'runtime', 'shutdown'), totals[:3])))
        print('覆盖直接调用、原生反调/异常、生成器恢复、未来线程、atexit；禁用/迟开负例通过。')
        if args.unsupported_python:
            print('错误版本显式审计拒绝、默认宿主继续可用通过。')
        print('startup_complete=false、complete=false：首个hook之前及直接default evaluator仍待独立插桩。')
    finally:
        shutil.rmtree(root)


if __name__ == '__main__':
    main()
