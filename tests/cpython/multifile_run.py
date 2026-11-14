#!/usr/bin/env python3
"""普通多文件项目的直接CLI验收：五个CIL模块、第三方包与资源、唯一文件删除输入搬迁。"""
from __future__ import annotations
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
from onefile_support import (ROOT, audit_onefile_output, build_fixture_wheel,
                             external_workspace, make_clean_env, make_polluted_env)
from version_support import create_venv, install


def run(command, cwd, env, data=None):
    process = subprocess.run(list(map(str, command)), cwd=cwd, env=env,
                             input=data, capture_output=True, timeout=1200)
    assert process.returncode == 0, (command, process.returncode, process.stdout, process.stderr)
    return process


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ryupy', type=Path, default=ROOT / 'target/debug' / ('ryupy.exe' if sys.platform == 'win32' else 'ryupy'))
    parser.add_argument('--python', type=Path, default=Path(sys.executable))
    parser.add_argument('--linux-container')
    parser.add_argument('--keep', action='store_true')
    args = parser.parse_args()
    workspace = external_workspace('ryupy-multifile-')
    env = make_clean_env()
    env.pop('VIRTUAL_ENV', None)
    env.pop('CONDA_PREFIX', None)
    try:
        inputs = workspace / '构建 输入'
        project = inputs / '订单 项目'
        shutil.copytree(ROOT / 'tests/cpython/fixtures/multifile_app', project)
        interpreter = create_venv(args.python.resolve(), project / '.venv', workspace, env)
        wheel = build_fixture_wheel(inputs, 'receipt_assets', '1.0.0', {
            'receipt_assets/__init__.py': 'from importlib.resources import files\ndef label():\n    return files(__package__).joinpath("资源.txt").read_text(encoding="utf-8").strip()\n',
            'receipt_assets/资源.txt': '正式收据 · 已打包资源\n'})
        install(interpreter, ['tomli==2.0.1', wheel], workspace, env)
        # 应用只安装业务依赖；构建工具不得注入或改写用户venv。
        absent = 'import importlib.util; assert importlib.util.find_spec("packaging") is None'
        run([interpreter, '-I', '-c', absent], workspace, env)
        entry = project / 'main.py'
        data = b'[order]\nunits = 7\nprice = 19\n'
        name = '中文顾客 & 空格'
        oracle_cwd = workspace / 'oracle'
        oracle_cwd.mkdir()
        oracle = run([interpreter, '-X', 'utf8', '-B', entry, name], oracle_cwd, env, data)
        expected_effect = (oracle_cwd / '收据.txt').read_bytes()
        # 不激活venv，不传provider、版本号、include-module、AOT或资源JSON。
        output = project / 'dist'
        run([args.ryupy.resolve(), entry, '--onefile'], project, env)
        binary = audit_onefile_output(output, 'main')
        manifest = json.loads((output / 'main.dist/ryupy-artifact.json').read_text(encoding='utf-8'))
        expected_modules = {'__main__', 'invoice', 'invoice.service', 'invoice.pricing', 'invoice.labels'}
        assert {source['module_name'] for source in manifest['sources']} == expected_modules
        report = json.loads((output / 'main.dist/cpython-provider.json').read_text(encoding='utf-8'))
        assert 'tomli' in str(report) and 'receipt_assets' in str(report)
        run([interpreter, '-I', '-c', absent], workspace, env)
        print('[multifile] 直接CLI成功：五个本地模块进入CIL，深层第三方依赖和资源随包，用户venv保持原样', flush=True)
        destination = workspace / '唯一二进制 搬迁目录'
        destination.mkdir()
        moved = destination / binary.name
        shutil.copy2(binary, moved)
        shutil.rmtree(inputs)
        assert not inputs.exists()
        empty = workspace / 'empty-path'
        empty.mkdir()
        temporary = workspace / 'runtime-temp'
        temporary.mkdir()
        polluted = make_polluted_env(empty)
        polluted.update({key: str(temporary) for key in ('TMPDIR', 'TMP', 'TEMP')})
        cwd = workspace / '独立 运行目录'
        cwd.mkdir()
        actual = run([moved, name], cwd, polluted, data)
        assert (actual.stdout, actual.stderr) == (oracle.stdout, oracle.stderr)
        assert (cwd / '收据.txt').read_bytes() == expected_effect
        assert not list(temporary.iterdir())
        if args.linux_container:
            clean = workspace / 'clean-target'
            clean.mkdir()
            base = ['docker', 'run', '--rm', '-i', '--network=none', '--user', f'{os.getuid()}:{os.getgid()}']
            probe = 'for t in python python3 dotnet; do if command -v "$t"; then exit 1; fi; done; test ! -d /usr/share/dotnet; test ! -d /usr/local/share/dotnet'
            run(base + [args.linux_container, '/bin/sh', '-c', probe], workspace, env)
            command = base + ['--mount', f'type=bind,source={destination},target=/app,readonly',
                              '--mount', f'type=bind,source={clean},target=/work', '--workdir=/work',
                              '--env=PATH=/empty', '--env=PYTHONHOME=/污染', '--env=PYTHONPATH=/污染',
                              '--env=LC_ALL=C.UTF-8', '--env=TZ=UTC', args.linux_container, '/app/' + moved.name, name]
            actual = run(command, workspace, env, data)
            assert (actual.stdout, actual.stderr) == (oracle.stdout, oracle.stderr)
            assert (clean / '收据.txt').read_bytes() == expected_effect
            print('[multifile] 无Python/.NET/SDK的断网目标容器通过', flush=True)
        print('[multifile] 删除整个多文件项目后，唯一AOT文件空PATH搬迁运行与同次CPython完全一致', flush=True)
    except BaseException:
        print('[multifile] 失败现场：', workspace, flush=True)
        raise
    else:
        if not args.keep:
            shutil.rmtree(workspace)


if __name__ == '__main__':
    main()
