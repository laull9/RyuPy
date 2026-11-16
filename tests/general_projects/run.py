#!/usr/bin/env python3
"""真实项目显式委派验收：同次 oracle、三种产品、删除输入后搬迁执行。

仅源码副本进入构建；每个执行臂用独立数据库。原项目与传入 venv 不变。
FastAPI 需要固定 requirements 环境；SanBudget 必须具备原生桌面会话。
"""
import argparse
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

from oracle import ROOT, copy_project


def invoke(command, cwd, env, timeout=600):
    result = subprocess.run(list(map(str, command)), cwd=cwd, env=env,
                            capture_output=True, timeout=timeout)
    assert result.returncode == 0, (command, result.returncode,
                                   result.stdout.decode(), result.stderr.decode())
    return result


def stderr_contract(data):
    """只归一化固定上游弃用警告的安装根，保留警告正文及其它全部输出。"""
    lines = data.splitlines(keepends=True)
    marker = b'/fastapi/testclient.py:1: StarletteDeprecationWarning:'
    return b''.join(line[line.index(marker):] if marker in line else line for line in lines)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ryupy', type=Path, required=True)
    parser.add_argument('--python', type=Path, required=True)
    parser.add_argument('--fixtures', type=Path, default=ROOT / 'tests/fixtures/projects',
                        help='预先准备的 fastapi-restful 与 SanBudget 原项目所在目录')
    parser.add_argument('--project', choices=['fastapi', 'sanbudget'], required=True)
    parser.add_argument('--compiled-app', action='store_true',
                        help='SanBudget 原业务和探针均编译为 CIL，验收跨域继承桥')
    parser.add_argument('--mode', choices=['jit', 'standalone', 'onefile'], action='append')
    args = parser.parse_args()
    if args.compiled_app and args.project != 'sanbudget':
        parser.error('--compiled-app 当前仅用于 SanBudget 跨域继承验收')
    cli = args.ryupy.absolute()
    python = args.python.absolute()
    env = {k: v for k, v in os.environ.items()
           if not k.startswith(('PYTHON', 'RYUPY_', 'LD_', 'DYLD_', 'VIRTUAL_ENV', 'CONDA_PREFIX'))}
    env.update(PYTHONDONTWRITEBYTECODE='1', PYTHONHASHSEED='0', TZ='UTC')
    with tempfile.TemporaryDirectory(prefix='ryupy-general-product-') as temporary:
        work = Path(temporary).resolve()
        assert not work.is_relative_to(ROOT), '产物必须写仓库外'
        inputs = work / 'inputs'
        inputs.mkdir()
        fixture = 'fastapi-restful' if args.project == 'fastapi' else 'SanBudget'
        source = args.fixtures.absolute() / fixture
        oracle_dir = work / 'oracle'
        oracle_entry = copy_project(source, oracle_dir, args.project)
        expected = invoke([python, '-B', oracle_entry], oracle_dir, env)
        shutil.rmtree(oracle_dir)
        project = inputs / 'project'
        entry = copy_project(source, project, args.project)
        build_env = dict(env)
        # 复制私有 venv 后删除，验证产品运行不会继续从安装环境读取第三方包。
        venv = python.parent.parent
        if (venv / 'pyvenv.cfg').is_file():
            shutil.copytree(venv, inputs / 'venv', symlinks=True)
            build_env['VIRTUAL_ENV'] = str(inputs / 'venv')
        else:
            build_env['PATH'] = str(python.parent) + os.pathsep + env.get('PATH', '')
        options = [] if args.compiled_app else ['--cpython-entry']
        if args.project == 'fastapi':
            modules = ['main', 'routes', 'services', 'databases', 'schemas', 'models']
            for module in modules:
                options.extend(['--cpython-module', module])
            for module in ['aiosqlite', 'httpx', 'greenlet']:
                options.extend(['--cpython-dynamic-module', module])
        elif not args.compiled_app:
            options.extend(['--cpython-module', 'app'])
        products = []
        for mode in args.mode or ['jit', 'standalone', 'onefile']:
            output = work / ('build-' + mode)
            result = invoke([cli, entry, '--mode', mode, '-o', output, *options], work, build_env)
            reported = [Path(line[len('已生成 '):]) for line in result.stderr.decode().splitlines()
                        if line.startswith('已生成 ')]
            assert reported, result.stderr.decode()
            executable = reported[-1]
            relocated = work / ('搬迁 空格 ' + mode)
            # 原生目录与 JIT 的执行依赖均位于所报告入口同目录。
            shutil.copytree(executable.parent, relocated, symlinks=True)
            products.append((mode, relocated / executable.name))
            shutil.rmtree(output)
        shutil.rmtree(inputs)
        empty_path = work / 'empty-path'
        empty_path.mkdir()
        product_env = dict(env, PYTHONHOME='/missing-python-home',
                           PYTHONPATH='/missing-python-path', PATH=str(empty_path))
        dotnet = shutil.which('dotnet')
        for mode, executable in products:
            cwd = work / ('cwd-' + mode)
            cwd.mkdir()
            command = [dotnet, executable] if mode == 'jit' else [executable]
            actual = invoke(command, cwd, product_env, timeout=90)
            assert actual.stdout == expected.stdout, (mode, actual.stdout, expected.stdout)
            assert stderr_contract(actual.stderr) == stderr_contract(expected.stderr), (
                mode, actual.stderr, expected.stderr)
            print(args.project, mode, '删除输入后搬迁合同通过')


if __name__ == '__main__':
    main()
