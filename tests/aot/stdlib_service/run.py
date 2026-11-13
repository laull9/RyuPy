#!/usr/bin/env python3
"""本地 HTTP → 编码压缩 → SQLite 应用的 CP/JIT/Native 搬迁合同。"""
import argparse
import base64
import gzip
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import json
import os
from pathlib import Path
import platform
import shutil
import sqlite3
import subprocess
import sys
import tempfile
import threading
from urllib.parse import parse_qs, urlsplit

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from aot_layout import reported_aot
from runtime_dependencies import MANAGED, NATIVE, validate_runtime_dependencies
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'cpython'))
from support import environment, external_workspace, require_python


class Handler(BaseHTTPRequestHandler):
    """服务只接受预期的真实 query；内容包含中文、引号与 SQL 形状文本。"""
    requests = 0
    payload = base64.b64encode(gzip.compress(json.dumps([
        {'id': 2, 'name': '中文数据'},
        {'id': 1, 'name': "quote'); DROP TABLE records; --"},
    ], ensure_ascii=False).encode('utf-8'), mtime=0))

    def do_GET(self):
        query = parse_qs(urlsplit(self.path).query)
        if query != {'format': ['gzip-base64'], 'label': ['集成']}:
            self.send_error(400)
            return
        type(self).requests += 1
        self.send_response(200)
        self.send_header('Content-Type', 'application/octet-stream')
        self.send_header('Content-Length', str(len(self.payload)))
        self.end_headers()
        self.wfile.write(self.payload)

    def log_message(self, *_):
        pass


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ryupy', type=Path, required=True)
    parser.add_argument('--python', default=sys.executable)
    parser.add_argument('--out', type=Path)
    parser.add_argument('--keep', action='store_true', help='显式保留仓库外调试输出')
    args = parser.parse_args()
    args.ryupy = args.ryupy.resolve()
    root = external_workspace('stdlib-service-', args.out)
    logs = root / 'logs'
    logs.mkdir()
    records = {'status': 'failed', 'platform': platform.platform(), 'steps': []}
    env = environment()
    # 本地合同绕过代理，应用仍走真实 HTTP 栈。
    env['NO_PROXY'] = env['no_proxy'] = '127.0.0.1,localhost'
    dotnet = shutil.which(os.environ.get('DOTNET', 'dotnet'))
    assert dotnet
    server = ThreadingHTTPServer(('127.0.0.1', 0), Handler)
    worker = threading.Thread(target=server.serve_forever, daemon=True)
    worker.start()
    url = 'http://127.0.0.1:' + str(server.server_port) + '/data'

    def run(label, command, *, cwd=root, process_env=env, check=True, timeout=240):
        result = subprocess.run(list(map(str, command)), cwd=cwd, env=process_env,
                                capture_output=True, timeout=timeout)
        (logs / (label + '.stdout')).write_bytes(result.stdout)
        (logs / (label + '.stderr')).write_bytes(result.stderr)
        records['steps'].append({'step': label, 'exit': result.returncode})
        print(label, result.returncode, flush=True)
        if check:
            assert result.returncode == 0, (label, result.stderr.decode(errors='replace'))
        return result

    def inspect_db(path):
        with sqlite3.connect(path) as conn:
            assert conn.execute('SELECT id, name FROM records ORDER BY id').fetchall() == [
                (1, "quote'); DROP TABLE records; --"), (2, '中文数据')]
            assert conn.execute('SELECT COUNT(*) FROM records WHERE id=99').fetchone() == (0,)
            assert conn.execute('PRAGMA integrity_check').fetchone() == ('ok',)

    try:
        require_python(args.python, root, env)
        fixture = Path(__file__).resolve().parent
        Handler.requests = 0
        source = root / '中文 & source'
        source.mkdir()
        shutil.copy2(fixture / 'app.py', source / 'app.py')
        shutil.copytree(fixture / 'appdata', source / 'appdata')
        resource_request = source / 'resources.json'
        resource_request.write_text(json.dumps({'schema_version': 1, 'modules': [
            {'module': 'appdata', 'files': ['schema.sql']}]}), encoding='utf-8')
        baseline_cwd = root / 'cpython-cwd'
        baseline_cwd.mkdir()
        baseline_result = run('cpython', [args.python, source / 'app.py', url, root / 'cp.sqlite'], cwd=baseline_cwd)
        baseline = baseline_result.stdout
        inspect_db(root / 'cp.sqlite')
        jit = run('jit-build', [args.ryupy, 'build', source / 'app.py', '-o', root / 'jit'])
        dlls = [Path(line.removeprefix('已生成 ')) for line in jit.stderr.decode().splitlines()
                if line.startswith('已生成 ')]
        assert len(dlls) == 1
        for dependency in MANAGED | {'RuntimeDependencies.props', 'ThirdPartyNotices.sqlite3.txt'}:
            assert (root / 'jit' / dependency).is_file(), dependency
        jit_cwd = root / 'jit-cwd'
        jit_cwd.mkdir()
        jit_run = run('jit-run', [dotnet, dlls[0], url, root / 'jit.sqlite'], cwd=jit_cwd)
        assert (jit_run.stdout, jit_run.stderr) == (baseline, baseline_result.stderr)
        inspect_db(root / 'jit.sqlite')
        report = json.loads((root / 'jit/app.source-plan.json').read_text())
        for name in ('sqlite3', 'base64', 'gzip', 'urllib.parse', 'urllib.request'):
            assert report['modules'][name]['provider'] == 'csharp_runtime', name
        assert report['static_closure_complete']
        build = run('aot-build', [args.ryupy, 'build', source / 'app.py', '--aot',
                                 '--resources', resource_request, '-o', root / 'publish'])
        machine = platform.machine().lower()
        rid = ('osx' if sys.platform == 'darwin' else 'win' if sys.platform == 'win32' else 'linux')
        assert machine in ('arm64', 'aarch64', 'x86_64', 'amd64'), machine
        assert sys.platform in ('darwin', 'win32', 'linux'), sys.platform
        rid += '-arm64' if machine in ('arm64', 'aarch64') else '-x64'
        layout = reported_aot(build.stderr, root / 'publish', 'app', rid)
        manifest = json.loads(layout.manifest.read_text())
        validate_runtime_dependencies(manifest['artifacts'], rid)
        library = NATIVE[rid]
        assert (layout.dist / library).is_file()
        assert (layout.dist / 'ThirdPartyNotices.sqlite3.txt').is_file()
        native_cwd = root / 'native-cwd'
        native_cwd.mkdir()
        native_run = run('native-original', [layout.native, url, root / 'native.sqlite'], cwd=native_cwd)
        assert (native_run.stdout, native_run.stderr) == (baseline, baseline_result.stderr)
        inspect_db(root / 'native.sqlite')
        moved = root / '中文 & relocated'
        shutil.move(layout.dist, moved)
        shutil.rmtree(source)
        shutil.rmtree(layout.build)
        shutil.rmtree(root / 'jit')
        empty = root / 'empty-cwd'
        empty.mkdir()
        clean = {k: v for k, v in env.items()
                 if not k.startswith(('PYTHON', 'DOTNET', 'LD_', 'DYLD_', 'RYUPY_'))}
        clean['PATH'] = str(empty)
        executable = moved / layout.native.name
        actual = run('native-relocated', [executable, url, root / 'moved.sqlite'],
                     cwd=empty, process_env=clean)
        assert (actual.stdout, actual.stderr) == (baseline, baseline_result.stderr)
        inspect_db(root / 'moved.sqlite')
        # 独立进程删除随包库，确认不能从构建机安装位置偷偷补齐依赖。
        (moved / library).rename(root / (library + '.withheld'))
        missing = run('native-missing-library', [executable, url, root / 'missing.sqlite'],
                      cwd=empty, process_env=clean, check=False)
        assert missing.returncode != 0
        assert b'ImportError' in missing.stderr and b'e_sqlite3' in missing.stderr
        assert Handler.requests == 5, Handler.requests
        records['requests'] = Handler.requests
        records['rid'] = rid
        records['status'] = 'passed'
    finally:
        server.shutdown()
        server.server_close()
        worker.join(timeout=5)
        (root / 'result.json').write_text(json.dumps(records, ensure_ascii=False, indent=2) + '\n')
        if args.keep:
            print('调试临时目录：', root)
        else:
            shutil.rmtree(root)


if __name__ == '__main__':
    main()
