#!/usr/bin/env python3
"""原版 CPython 3.12.13 基线：临时源码/venv/私有服务，持久证据，逐项目失败隔离。"""
import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import shutil
import signal
import subprocess
import sys
import tempfile
import traceback

from inspect_sources import CACHE, CATALOG, analyze
from private_services import services

HERE = Path(__file__).resolve().parent
EVIDENCE = Path.home() / '.cache/ryupy/validation/adr0208-20261117/github-large'


def invoke(command, cwd, env, log, timeout=600):
    """记录原输出；所有子命令有超时，失败证据永久保留。"""
    with log.open('w') as stream:
        stream.write(json.dumps(list(map(str, command)), ensure_ascii=False) + '\n')
        stream.flush()
        process = subprocess.Popen(list(map(str, command)), cwd=cwd, env=env,
                                   stdout=stream, stderr=subprocess.STDOUT, start_new_session=True)
        try:
            code = process.wait(timeout=timeout)
        except BaseException:
            # 只终止本命令的新进程组，包含 GUI 的容量查询子进程。
            os.killpg(process.pid, signal.SIGTERM)
            try:
                process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                os.killpg(process.pid, signal.SIGKILL)
                process.wait(timeout=5)
            raise
    if code:
        raise RuntimeError(f'命令失败 exit={code}，见 {log}')


def baseline(project, evidence):
    source = CACHE / project['repo'].replace('/', '--')
    analyze(project, source)
    with tempfile.TemporaryDirectory(prefix='ryupy-large-cp-') as temporary:
        work = Path(temporary).resolve()
        (evidence / 'temporary.json').write_text(json.dumps({'work': str(work)}))
        tree, venv, profile = work / 'source', work / 'venv', work / 'profile'
        shutil.copytree(source, tree, ignore=shutil.ignore_patterns('.git', '__pycache__', '.venv'))
        profile.mkdir()
        env = {k: v for k, v in os.environ.items()
               if not k.startswith(('PYTHON', 'RYUPY_', 'VIRTUAL_ENV', 'CONDA', 'QT_', 'PG', 'REDIS', 'DATABASE', 'CELERY'))}
        env.update(PYTHONDONTWRITEBYTECODE='1', PYTHONUNBUFFERED='1', HOME=str(profile),
                   XDG_CONFIG_HOME=str(profile / 'config'), XDG_CACHE_HOME=str(profile / 'cache'),
                   UV_CACHE_DIR=str(CACHE / 'uv-cache'))
        invoke(['uv', 'venv', '--python', sys.executable, venv], tree, env, evidence / 'venv.log')
        python = venv / 'bin/python'
        if project['id'] == 'fba':
            requirements = work / 'requirements.txt'
            invoke(['uv', 'export', '--frozen', '--no-hashes', '--no-dev', '--no-emit-project',
                    '-o', requirements], tree, env, evidence / 'export.log')
            invoke(['uv', 'pip', 'install', '--python', python, '-r', requirements], tree, env,
                   evidence / 'install.log')
            # 插件依赖不在上游 uv.lock：首次解析后独立锁定，再安装精确版本。
            plugin = work / 'plugins.in'
            plugin.write_text('aiosmtplib==5.0.0\nfastapi-oauth20==0.0.2\n')
            invoke(['uv', 'pip', 'install', '--python', python, '-r', plugin], tree, env,
                   evidence / 'plugins.log')
            invoke(['uv', 'pip', 'freeze', '--python', python], tree, env, evidence / 'freeze.log')
            config = dict(ENVIRONMENT='dev', TOKEN_SECRET_KEY='private-baseline-only-token-key',
                          IP_LOCATION_PARSE='false', GRAFANA_METRICS_ENABLE='false')
            with services(work, evidence) as private:
                config.update(private)
                config.update(OAUTH2_GITHUB_CLIENT_ID='unused', OAUTH2_GITHUB_CLIENT_SECRET='unused',
                              OAUTH2_GOOGLE_CLIENT_ID='unused', OAUTH2_GOOGLE_CLIENT_SECRET='unused',
                              EMAIL_USERNAME='', EMAIL_PASSWORD='', CELERY_BROKER='redis',
                              CELERY_RABBITMQ_HOST='127.0.0.1', CELERY_RABBITMQ_PORT='1',
                              CELERY_RABBITMQ_USERNAME='unused', CELERY_RABBITMQ_PASSWORD='unused')
                env.update(config, VIRTUAL_ENV=str(venv))
                (tree / 'backend/.env').write_text('\n'.join(f'{k}={json.dumps(v)}' for k, v in config.items()))
                invoke([python, '-B', HERE / 'fba_probe.py'], tree, env, evidence / 'probe.log', 150)
        else:
            invoke(['uv', 'pip', 'install', '--python', python, '-r', HERE / 'explorerpro-requirements.txt'],
                   tree, env, evidence / 'install.log')
            invoke(['uv', 'pip', 'freeze', '--python', python], tree, env, evidence / 'freeze.log')
            env.update(QT_QPA_PLATFORM='cocoa')
            invoke([python, '-B', HERE / 'explorerpro_probe.py', str(profile), str(evidence)],
                   tree, env, evidence / 'probe.log', 120)
        invoke(['uv', 'pip', 'freeze', '--python', python], tree, env, evidence / 'freeze.log')
    (evidence / 'cleanup.json').write_text(json.dumps({'temporary_tree_removed': not work.exists()}))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', choices=['fba', 'explorerpro'], required=True)
    args = parser.parse_args()
    assert sys.version_info[:3] == (3, 12, 13), sys.version
    project = next(p for p in json.loads(CATALOG.read_text())['projects'] if p['id'] == args.project)
    evidence = EVIDENCE / (args.project + '-' + datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ'))
    evidence.mkdir(parents=True)
    print(evidence, flush=True)
    (evidence / 'source.json').write_text(json.dumps(project, ensure_ascii=False, indent=2))
    # 保存同次手写驱动以便后续核对日志；不计算普通文件摘要。
    driver = evidence / 'driver'
    driver.mkdir()
    for file in HERE.glob('*'):
        if file.suffix in ('.py', '.json', '.txt'):
            shutil.copy2(file, driver / file.name)
    try:
        baseline(project, evidence)
    except BaseException:
        (evidence / 'failure.txt').write_text(traceback.format_exc())
        temporary = evidence / 'temporary.json'
        if temporary.exists():
            removed = not Path(json.loads(temporary.read_text())['work']).exists()
            (evidence / 'cleanup.json').write_text(json.dumps({'temporary_tree_removed': removed}))
        raise
    print('PASS', args.project, evidence, flush=True)


if __name__ == '__main__':
    main()
