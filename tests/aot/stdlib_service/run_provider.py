#!/usr/bin/env python3
"""CP2 原联合应用切 CPython provider；保持 app.py 和 appdata 的业务输入。"""
import argparse
from http.server import ThreadingHTTPServer
import json
from pathlib import Path
import shutil
import sqlite3
import sys

# 验收驱动本身也不在源码目录留下字节码缓存。
sys.dont_write_bytecode = True
import threading

from run import Handler

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'cpython'))
from support import (audit_native_paths, checked, declaration, environment, execute,
                     external_workspace, inspect_host_report, private_distribution,
                     require_python, rid, write_json)
from run import reported_aot

MODULES = ['urllib', 'base64', 'gzip', 'json', 'sqlite3', 'importlib', 'sys', 'appdata']


def inspect_database(path):
    """独立连接核验全部业务值、SQL注入防护、异常回滚与文件完整性。"""
    with sqlite3.connect(path) as connection:
        assert connection.execute('SELECT id, name FROM records ORDER BY id').fetchall() == [
            (1, "quote'); DROP TABLE records; --"), (2, '中文数据')]
        assert connection.execute('SELECT COUNT(*) FROM records WHERE id=99').fetchone() == (0,)
        assert connection.execute('PRAGMA integrity_check').fetchone() == ('ok',)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--python', default=sys.executable)
    parser.add_argument('--ryupy', type=Path)
    parser.add_argument('--distribution', type=Path)
    parser.add_argument('--distribution-metadata', type=Path)
    parser.add_argument('--out', type=Path)
    parser.add_argument('--keep', action='store_true')
    parser.add_argument('--baseline-only', action='store_true')
    parser.add_argument('--jit-only', action='store_true')
    args = parser.parse_args()
    if not args.baseline_only:
        assert args.ryupy and args.distribution, '产品验收需 --ryupy 和 --distribution'
        args.ryupy = args.ryupy.resolve()
    root = external_workspace('ryupy-cp2-', args.out)
    env = environment()
    server = ThreadingHTTPServer(('127.0.0.1', 0), Handler)
    worker = threading.Thread(target=server.serve_forever, daemon=True)
    worker.start()
    url = 'http://127.0.0.1:' + str(server.server_port) + '/data'
    steps = []

    def run(label, command, *, cwd=root, process_env=env, check=True):
        result = execute(command, cwd=cwd, env=process_env, timeout=600)
        (root / (label + '.stdout')).write_bytes(result.stdout)
        (root / (label + '.stderr')).write_bytes(result.stderr)
        steps.append({'step': label, 'exit': result.returncode})
        print(label, result.returncode, flush=True)
        return checked(result, label) if check else result

    def compare(label, command, expected, *, cwd=root, process_env=env):
        Handler.requests = 0
        result = run(label, command, cwd=cwd, process_env=process_env)
        assert (result.stdout, result.stderr, result.returncode) == (
            expected.stdout, expected.stderr, expected.returncode), label
        assert Handler.requests == 1, (label, Handler.requests)
        inspect_database(Path(command[-1]))

    try:
        require_python(args.python, root, env)
        distribution = private_distribution(args.distribution, root, args.distribution_metadata) if args.distribution else None
        fixture = Path(__file__).resolve().parent
        source = root / '中文 & source'
        source.mkdir()
        shutil.copy2(fixture / 'app.py', source / 'app.py')
        shutil.copytree(fixture / 'appdata', source / 'appdata')
        Handler.requests = 0
        baseline_cwd = root / 'cpython-cwd'
        baseline_cwd.mkdir()
        expected = run('cpython', [args.python, '-B', source / 'app.py', url, baseline_cwd / 'data.sqlite'], cwd=baseline_cwd)
        assert Handler.requests == 1
        inspect_database(baseline_cwd / 'data.sqlite')
        if args.baseline_only:
            return
        request = source / 'provider.json'
        write_json(request, declaration(distribution, [
            {'source': str(source / 'appdata'), 'destination': 'appdata'}], MODULES))
        command = [args.ryupy, '--cpython-provider', request, 'build', source / 'app.py']
        jit = root / 'jit'
        result = run('jit-build', command + ['-o', jit])
        dlls = [Path(line.removeprefix('已生成 ')) for line in result.stderr.decode().splitlines()
                if line.startswith('已生成 ')]
        assert len(dlls) == 1, dlls
        jit_cwd = root / 'jit-cwd'
        jit_cwd.mkdir()
        jit_env = dict(env, RYUPY_CPYTHON_REPORT=str(jit_cwd / 'host.json'))
        compare('jit', [shutil.which('dotnet'), dlls[0], url, jit_cwd / 'data.sqlite'], expected,
                cwd=jit_cwd, process_env=jit_env)
        inspect_host_report(jit_cwd / 'host.json', jit)
        report = json.loads((jit / 'app.source-plan.json').read_text())
        for name in ('base64', 'gzip', 'json', 'sqlite3', 'importlib.resources', 'urllib.parse', 'urllib.request', 'appdata', 'sys'):
            assert report['modules'][name]['provider'] == 'cpython', (name, report['modules'].get(name))
        if args.jit_only:
            return
        publish = root / 'publish'
        result = run('aot-build', command + ['--aot', '-o', publish])
        layout = reported_aot(result.stderr, publish, 'app', rid())
        native_cwd = root / 'native-cwd'
        native_cwd.mkdir()
        native_env = dict(env, RYUPY_CPYTHON_REPORT=str(native_cwd / 'host.json'))
        compare('native', [layout.native, url, native_cwd / 'data.sqlite'], expected,
                cwd=native_cwd, process_env=native_env)
        inspect_host_report(native_cwd / 'host.json', layout.dist)
        moved = root / '中文 & relocated'
        shutil.move(layout.dist, moved)
        shutil.rmtree(source)
        shutil.rmtree(layout.build)
        shutil.rmtree(jit)
        shutil.rmtree(distribution)
        empty = root / 'empty-cwd'
        empty.mkdir()
        clean = environment()
        clean['PATH'] = str(empty)
        clean['PYTHONHOME'] = clean['PYTHONPATH'] = str(empty / '不存在')
        clean['RYUPY_CPYTHON_REPORT'] = str(empty / 'host.json')
        executable = moved / layout.native.name
        audit_native_paths(moved)
        compare('relocated', [executable, url, root / 'relocated.sqlite'], expected, cwd=empty, process_env=clean)
        inspect_host_report(empty / 'host.json', moved)
        # 包资源缺失和stdlib包缺失各在独立进程内验证，并恢复以保留错误发现。
        for label, pattern, expected_error in (
                ('missing-resource', 'appdata/schema.sql', b'FileNotFoundError'),
                ('missing-sqlite-package', 'sqlite3/__init__.py', b'ModuleNotFoundError')):
            found = list(moved.rglob(pattern))
            assert len(found) == 1, (label, found)
            original = found[0].parent if label == 'missing-sqlite-package' else found[0]
            hidden = empty / (label + '.withheld')
            original.rename(hidden)
            try:
                failed = run(label, [executable, url, root / (label + '.sqlite')],
                             cwd=empty, process_env=clean, check=False)
                assert failed.returncode != 0 and expected_error in failed.stderr, failed.stderr
                expected_detail = b'schema.sql' if label == 'missing-resource' else b"No module named 'sqlite3'"
                assert expected_detail in failed.stderr, failed.stderr
            finally:
                hidden.rename(original)
    finally:
        server.shutdown()
        server.server_close()
        worker.join(timeout=5)
        write_json(root / 'result.json', {'steps': steps, 'rid': rid()})
        if args.keep:
            print('调试临时目录：', root)
        else:
            shutil.rmtree(root)


if __name__ == '__main__':
    main()
