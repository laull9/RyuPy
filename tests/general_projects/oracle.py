#!/usr/bin/env python3
"""在仓库外运行两份真实项目合同，并保留原业务输入。

FastAPI 使用指定的 Python 3.13.3 依赖环境；SanBudget 使用指定解释器，
必须具备原生显示会话和 Tk。驱动不安装依赖，所有数据库和副本退出时清理。
"""
import argparse
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent


def copy_project(source, target, probe):
    """忽略开发环境与可变数据，确保原业务数据库从未被测试改写。"""
    shutil.copytree(source, target, ignore=shutil.ignore_patterns(
        '.venv', '.git', '__pycache__', '*.pyc', '*.db', '*.db-wal', '*.db-shm'))
    shutil.copy2(HERE / f'{probe}_probe.py', target / 'probe.py')
    return target / 'probe.py'


def run_contract(python, source, target, probe):
    entry = copy_project(source, target, probe)
    env = {k: v for k, v in os.environ.items()
           if not k.startswith(('PYTHON', 'RYUPY_', 'LD_', 'DYLD_'))}
    env['PYTHONDONTWRITEBYTECODE'] = '1'
    result = subprocess.run([str(python), '-B', str(entry)], cwd=target,
                            env=env, capture_output=True, timeout=90)
    assert result.returncode == 0, (probe, result.returncode,
                                   result.stdout.decode(), result.stderr.decode())
    print(result.stdout.decode(), end='')
    # 上游警告完整保留；产品差分也必须检查 stderr。
    if result.stderr:
        print(result.stderr.decode(), end='', file=sys.stderr)
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--fastapi-python', type=Path, required=True)
    parser.add_argument('--tk-python', type=Path, required=True)
    parser.add_argument('--fixtures', type=Path, default=ROOT / 'tests/fixtures/projects')
    args = parser.parse_args()
    with tempfile.TemporaryDirectory(prefix='ryupy-general-oracle-') as temporary:
        workspace = Path(temporary).resolve()
        assert not workspace.is_relative_to(ROOT), '验收临时目录必须在仓库外'
        # venv 的解释器常为符号链接，保留调用路径才能读取其 pyvenv.cfg。
        run_contract(args.fastapi_python.absolute(), args.fixtures / 'fastapi-restful',
                     workspace / 'fastapi', 'fastapi')
        run_contract(args.tk_python.absolute(), args.fixtures / 'SanBudget',
                     workspace / 'sanbudget', 'sanbudget')


if __name__ == '__main__':
    main()
