#!/usr/bin/env python3
"""首构建前冻结总预算；所有runtime/CLI准备复用同一PGID/watchdog/wait4执行器。"""
import argparse
import os
import shutil
from pathlib import Path
import time

from perf_runner import Runner, write_json, digest
import module_call_schema as schema
from perf import environment, executable


def cargo_proxy():
    # rustup按argv[0]选择工具，不能resolve代理符号链接为rustup。
    found = shutil.which("cargo")
    if found is None:
        raise FileNotFoundError("cargo")
    return os.path.abspath(found)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--baseline-root', type=Path, required=True)
    parser.add_argument('--candidate-root', type=Path, required=True)
    parser.add_argument('--out', type=Path, required=True)
    parser.add_argument('--candidate-revision', required=True)
    args = parser.parse_args()
    args.out.mkdir(parents=True, exist_ok=True)
    marker = args.out / 'started-at'
    # 不允许重跑此入口悄悄重置总预算，失败后保留首轮全部日志。
    with marker.open('x') as stream:
        started = time.time()
        stream.write(str(started) + '\n')
    run = Runner(args.out / 'compiler-logs', time.monotonic() + 1200)
    env = environment()
    dotnet, cargo = executable('dotnet'), cargo_proxy()
    env['DOTNET'] = dotnet
    status, failure = 'failed', None
    versions = {}
    write_json(args.out / 'budget.json', {'started_at': started, 'budget_seconds': 1200})
    try:
        if args.baseline_root.resolve() == args.candidate_root.resolve():
            raise ValueError('两版本必须独立checkout')
        for v, root in (('B', args.baseline_root.resolve()), ('C', args.candidate_root.resolve())):
            head = run.run(v + '-head', ['git', 'rev-parse', 'HEAD'], cwd=root, env=env, limit=30)
            revision = head['stdout'].decode().strip()
            if revision != (schema.BASELINE if v == 'B' else args.candidate_revision):
                raise ValueError('固定baseline不符')
            dirty = run.run(v + '-status', ['git', 'status', '--porcelain'], cwd=root, env=env, limit=30)
            if dirty['stdout']:
                raise ValueError('构建来源必须clean')
            versions[v] = {'root': str(root), 'revision': revision}
            run.run(v + '-runtime', [dotnet, 'build', 'runtime/RyuPy.Runtime', '-c', 'Release',
                    '-t:Rebuild', '-nologo', '-v', 'q'], cwd=root, env=env, limit=180)
            run.run(v + '-cli-clean', [cargo, 'clean', '-p', 'ryupy-cli', '--release'],
                    cwd=root, env=env, limit=180)
            run.run(v + '-cli', [cargo, 'build', '-p', 'ryupy-cli', '--release', '--locked'],
                    cwd=root, env=env, limit=180)
            versions[v].update(cli_sha256=digest(root / 'target/release/ryupy'),
                runtime_sha256=digest(root / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'))
        if time.monotonic() >= run.deadline:
            raise TimeoutError('工具构建已耗尽统一预算')
        status = 'prepared'
    except BaseException as error:
        failure = f'{type(error).__name__}: {error}'
        raise
    finally:
        write_json(args.out / 'compiler-summary.json', {'status': status, 'failure': failure,
            'started_at': started, 'elapsed_seconds': time.time() - started,
            'budget_seconds': 1200, 'cli_builds': 2, 'runtime_preparations': 2, 'versions': versions})


if __name__ == '__main__':
    main()
