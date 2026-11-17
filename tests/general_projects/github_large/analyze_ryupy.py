#!/usr/bin/env python3
"""逐个真实上游源码调用 RyuPy dump；只跑静态前端，不启动应用或服务。

覆盖选中项目全部非测试、非生成 UI 源码；诊断按文件记录，失败仍继续发现后续文件。
产物只写仓库外临时目录并自动清理；可选外部报告由调用者清理。此门针对 --native
的单文件 AST/HIR 支持，完整项目链接、provider 兼容与运行门由 README 单列。
"""
import argparse
from collections import Counter
import json
import re
from pathlib import Path
import subprocess
import tempfile

from inspect_sources import CACHE, CATALOG, REPO, analyze


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ryupy', type=Path, required=True)
    parser.add_argument('--cache', type=Path, default=CACHE)
    parser.add_argument('--emit', choices=['ast', 'hir', 'types', 'mir'], action='append')
    parser.add_argument('--output', type=Path)
    parser.add_argument('--timeout', type=int, default=30)
    args = parser.parse_args()
    if args.output and args.output.resolve().is_relative_to(REPO):
        parser.error('报告必须放仓库外')
    cli = args.ryupy.resolve(strict=True)
    cache = args.cache.expanduser().resolve()
    if cache.is_relative_to(REPO):
        parser.error('源码缓存必须放仓库外')
    rows = []
    with tempfile.TemporaryDirectory(prefix='ryupy-github-large-analysis-') as temporary:
        for project in json.loads(CATALOG.read_text())['projects']:
            if not project['selected']:
                continue
            source = cache / project['repo'].replace('/', '--')
            inventory = analyze(project, source)
            for name, kind in inventory['classifications'].items():
                if kind != 'source':
                    continue
                for emit in args.emit or ['ast', 'hir']:
                    # IR 可能很大，直接落临时文件，进程结束后立即删除；仅保留诊断文本。
                    output = Path(temporary) / 'ir.txt'
                    with output.open('w') as stream:
                        try:
                            result = subprocess.run(
                                [str(cli), 'dump', str(source / name), '--native', '--emit', emit],
                                cwd=temporary, stdout=stream, stderr=subprocess.PIPE,
                                text=True, timeout=args.timeout)
                            code, error = result.returncode, result.stderr
                        except subprocess.TimeoutExpired:
                            code, error = 124, '单文件分析超时'
                    output.unlink()
                    # dump 某些阶段即使输出 Error 也退出 0，必须同时检查诊断。
                    failed = code != 0 or bool(re.search(r'\[E\d+\]|panicked at', error))
                    rows.append({'id': project['id'], 'file': name, 'emit': emit,
                                 'exit': code, 'failed': failed, 'stderr': error})
            summary = Counter((r['emit'], 'failed' if r['failed'] else 'clean') for r in rows if r['id'] == project['id'])
            print(project['id'], json.dumps({f'{emit}:{code}': count
                                             for (emit, code), count in sorted(summary.items())}))
    if args.output:
        args.output.write_text(json.dumps(rows, ensure_ascii=False, indent=2) + '\n')
    raise SystemExit(1 if any(r['failed'] for r in rows) else 0)


if __name__ == '__main__':
    main()
