#!/usr/bin/env python3
"""固定大型项目的完整构建门；保留业务源码和依赖环境，明确记录首阻塞与执行归属。

必须提供已按基线锁定准备的 venv；驱动只读使用它，原上游缓存先验证 Git 提交和脏状态。
源码/构建副本放仓库外临时树，完整日志与精简结果持久保留；本门不启动应用或服务。
"""
import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

from inspect_sources import CACHE, CATALOG, REPO, analyze


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', choices=['fba', 'explorerpro'], required=True)
    parser.add_argument('--python', type=Path, required=True, help='已准备原锁定依赖的 CPython 3.12.13 venv')
    parser.add_argument('--ryupy', type=Path, required=True)
    parser.add_argument('--cache', type=Path, default=CACHE)
    parser.add_argument('--mode', choices=['jit', 'standalone', 'onefile'], action='append')
    parser.add_argument('--stock-module-namespaces', action='store_true')
    parser.add_argument('--native-components', type=Path, help='显式原生组件来源/许可/ABI声明')
    parser.add_argument('--evidence', type=Path, default=Path.home() / '.cache/ryupy/validation/github-large-build')
    args = parser.parse_args()
    cache, cli = args.cache.expanduser().resolve(), args.ryupy.resolve(strict=True)
    python = args.python.expanduser().absolute()
    assert (python.parent.parent / 'pyvenv.cfg').is_file(), '必须提供独立依赖 venv，保留解释器符号链接路径'
    evidence = args.evidence.expanduser().resolve() / (
        args.project + '-' + datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ'))
    assert not cache.is_relative_to(REPO) and not evidence.is_relative_to(REPO), '源码缓存和证据必须放仓库外'
    evidence.mkdir(parents=True)
    print(evidence, flush=True)
    project = next(p for p in json.loads(CATALOG.read_text())['projects'] if p['id'] == args.project)
    source = cache / project['repo'].replace('/', '--')
    analyze(project, source)
    env = {k: v for k, v in os.environ.items()
           if not k.startswith(('PYTHON', 'RYUPY_', 'LD_', 'DYLD_', 'VIRTUAL_ENV', 'CONDA'))}
    env.update(PYTHONDONTWRITEBYTECODE='1', VIRTUAL_ENV=str(python.parent.parent))
    version = subprocess.run([python, '-I', '-c', 'import platform; print(platform.python_version())'],
                             capture_output=True, env=env, timeout=30, check=True)
    assert version.stdout.strip() == b'3.12.13', version.stdout
    freeze = subprocess.run(['uv', 'pip', 'freeze', '--python', python], capture_output=True,
                            env=env, timeout=30, check=True)
    (evidence / 'freeze.txt').write_bytes(freeze.stdout)
    (evidence / 'source.json').write_text(json.dumps(project, ensure_ascii=False, indent=2) + '\n')
    shutil.copy2(__file__, evidence / 'build_project.py')
    results = []
    with tempfile.TemporaryDirectory(prefix='ryupy-large-project-build-') as temporary:
        work = Path(temporary).resolve()
        assert not work.is_relative_to(REPO)
        tree = work / 'source'
        shutil.copytree(source, tree, ignore=shutil.ignore_patterns('.git', '.venv', '__pycache__'))
        for mode in args.mode or ['jit']:
            command = [str(cli), str(tree / project['entry']), '--mode', mode, '-o', str(work / mode)]
            if args.stock_module_namespaces:
                command.append('--experimental-stock-module-namespaces')
            if args.native_components:
                command.extend(['--cpython-native-components', str(args.native_components.expanduser().resolve())])
            for root in project['import_roots']:
                command.extend(['-I', str(tree / root)])
            (evidence / (mode + '-command.json')).write_text(json.dumps(command, ensure_ascii=False) + '\n')
            with (evidence / (mode + '-build.log')).open('wb') as log:
                try:
                    result = subprocess.run(command, cwd=work, env=env, stdout=log,
                                            stderr=subprocess.STDOUT, timeout=900)
                    code = result.returncode
                except subprocess.TimeoutExpired:
                    code = 124
            rows = (evidence / (mode + '-build.log')).read_text(errors='replace').splitlines()
            first = next((line for line in rows if '[E' in line or line.startswith('error:')), '')
            item = {'project': args.project, 'mode': mode, 'build_exit': code,
                    'first_diagnostic': first, 'runtime': 'not-run', 'relocation': 'not-run',
                    'business_delegations': [], 'requested_cil_roots': project['ownership']['cil_target']}
            results.append(item)
            print(json.dumps(item, ensure_ascii=False), flush=True)
    (evidence / 'results.json').write_text(json.dumps(results, ensure_ascii=False, indent=2) + '\n')
    (evidence / 'cleanup.json').write_text(json.dumps({'temporary_tree_removed': not work.exists()}))
    raise SystemExit(1 if any(r['build_exit'] for r in results) else 0)


if __name__ == '__main__':
    main()
