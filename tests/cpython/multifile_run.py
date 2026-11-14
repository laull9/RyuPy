#!/usr/bin/env python3
"""普通多文件项目的直接CLI验收：单文件CLI独立构建、冷缓存并发互斥、五个CIL模块、删除输入后搬迁运行。"""
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
        inputs.mkdir(parents=True)

        # 将CLI私有运行时缓存重定向至inputs内部，证明不复用开发机已解包缓存。
        cache_var = 'LOCALAPPDATA' if sys.platform == 'win32' else 'XDG_CACHE_HOME'
        cli_cache = inputs / '用户私有 缓存 & 目录'
        cli_cache.mkdir(parents=True)
        env[cache_var] = str(cli_cache)

        # 唯一CLI单文件复制到含中文与空格的仓库外独立目录，禁止复制任何runtime/shim/源码或旁置DLL。
        compiler = inputs / '单文件 编译器 & 空格'
        compiler.mkdir(parents=True)
        packaged_cli = compiler / args.ryupy.name
        shutil.copy2(args.ryupy.resolve(), packaged_cli)
        assert list(compiler.iterdir()) == [packaged_cli]
        if sys.platform != 'win32':
            packaged_cli.chmod(0o755)

        # 验证单文件CLI在空PATH、污染Python变量及无缓存路径下，--version/--help/--licenses不依赖外部环境与缓存。
        empty_probe_dir = workspace / 'empty-path-probe'
        empty_probe_dir.mkdir()
        empty_env = make_polluted_env(empty_probe_dir)
        no_cache_env = dict(empty_env)
        no_cache_env[cache_var] = str(workspace / '不存在的虚假缓存')
        v = subprocess.run([str(packaged_cli), '--version'], env=no_cache_env, capture_output=True, text=True, timeout=60)
        assert v.returncode == 0 and 'ryupy' in v.stdout, f'空PATH下--version失败：{v.stderr}'
        h = subprocess.run([str(packaged_cli), '--help'], env=no_cache_env, capture_output=True, text=True, timeout=60)
        assert h.returncode == 0 and 'ryupy' in h.stdout, f'空PATH下--help失败：{h.stderr}'
        lic = subprocess.run([str(packaged_cli), '--licenses'], env=no_cache_env, capture_output=True, text=True, timeout=60)
        assert lic.returncode == 0 and len(lic.stdout) > 0, f'空PATH下--licenses失败：{lic.stderr}'
        assert not Path(no_cache_env[cache_var]).exists(), '信息查询不得创建运行时缓存'

        # 在空PATH且指向全新冷缓存下执行四路并发--native --mode jit，验证冷缓存fs2锁与按需部署。
        empty_env[cache_var] = str(cli_cache)
        probe_jit = inputs / '并发 JIT 验证'
        probe_jit.mkdir()
        probe_entry = probe_jit / 'entry.py'
        probe_entry.write_text('print("native jit in empty path")\n', encoding='utf-8')
        outs = [probe_jit / f'out_{idx}' for idx in range(4)]
        procs = [subprocess.Popen([str(packaged_cli), str(probe_entry), '--native', '--mode', 'jit', '-o', str(out)],
                                  env=empty_env, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
                 for out in outs]
        try:
            native_sqlite = {'darwin': 'libe_sqlite3.dylib', 'linux': 'libe_sqlite3.so', 'win32': 'e_sqlite3.dll'}[sys.platform]
            for idx, proc in enumerate(procs):
                stdout, stderr = proc.communicate(timeout=120)
                assert proc.returncode == 0, f'并发JIT分支 {idx} 失败：{stderr.decode(errors="replace")}'
                files = {path.name for path in outs[idx].iterdir()}
                assert 'entry.dll' in files, f'产物缺失entry.dll：{files}'
                assert 'RyuPy.Runtime.dll' in files, f'产物缺失RyuPy.Runtime.dll：{files}'
                assert 'RyuPy.CPython.dll' not in files, f'纯原生JIT产物不应包含CPython桥：{files}'
                assert native_sqlite in files, f'产物缺失SQLite原生依赖：{files}'
        finally:
            for proc in procs:
                if proc.poll() is None:
                    proc.kill()
                    proc.wait()
        shutil.rmtree(empty_probe_dir)
        print('[multifile] 单文件CLI空PATH验证通过：--version/--help/--licenses免环境，四路并发JIT冷缓存互斥锁成功且无CPython桥', flush=True)

        # 普通多文件项目：包含五个手写模块调用链、第三方包tomli及第三方中文资源。
        project = inputs / '订单 项目'
        shutil.copytree(ROOT / 'tests/cpython/fixtures/multifile_app', project)
        interpreter = create_venv(args.python.resolve(), project / '.venv', workspace, env)
        wheel = build_fixture_wheel(inputs, 'receipt_assets', '1.0.0', {
            'receipt_assets/__init__.py': 'from importlib.resources import files\ndef label():\n    return files(__package__).joinpath("资源.txt").read_text(encoding="utf-8").strip()\n',
            'receipt_assets/资源.txt': '正式收据 · 已打包资源\n'})
        install(interpreter, ['tomli==2.0.1', wheel], workspace, env)

        # 应用只安装业务依赖；构建工具在私有缓存准备，不得注入或改写用户venv。
        absent = 'import importlib.util; assert importlib.util.find_spec("packaging") is None'
        run([interpreter, '-I', '-c', absent], workspace, env)
        entry = project / 'main.py'

        # 同次CPython Oracle基准：取得标准输入输出、错误流、退出码与文件副作用。
        data = b'[order]\nunits = 7\nprice = 19\n'
        name = '中文顾客 & 空格'
        oracle_cwd = workspace / 'oracle'
        oracle_cwd.mkdir()
        oracle = run([interpreter, '-X', 'utf8', '-B', entry, name], oracle_cwd, env, data)
        expected_effect = (oracle_cwd / '收据.txt').read_bytes()

        # 普通命令只表达入口与发布形态：不激活venv，自动探测项目环境，直接输出单一AOT文件。
        output = project / 'dist'
        run([packaged_cli, entry, '--onefile'], project, env)
        assert list(compiler.iterdir()) == [packaged_cli], '编译器不得依赖或生成旁置runtime目录'
        binary = audit_onefile_output(output, 'main')
        manifest = json.loads((output / 'main.dist/ryupy-artifact.json').read_text(encoding='utf-8'))
        expected_modules = {'__main__', 'invoice', 'invoice.service', 'invoice.pricing', 'invoice.labels'}
        assert {source['module_name'] for source in manifest['sources']} == expected_modules
        report = json.loads((output / 'main.dist/cpython-provider.json').read_text(encoding='utf-8'))
        assert 'tomli' in str(report) and 'receipt_assets' in str(report)
        run([interpreter, '-I', '-c', absent], workspace, env)
        print('[multifile] 直接CLI成功：五个本地模块进入CIL，深层第三方依赖和资源随包，用户venv保持原样', flush=True)

        # 搬走唯一生成AOT二进制，随后彻底删除整个inputs（包含复制CLI、缓存、项目源码、venv、wheel及所有中间产物）。
        destination = workspace / '唯一二进制 搬迁目录'
        destination.mkdir()
        moved = destination / binary.name
        shutil.copy2(binary, moved)
        if sys.platform != 'win32':
            moved.chmod(0o755)
        shutil.rmtree(inputs)
        assert not inputs.exists()

        # 在空PATH、受污染Python环境变量及独立临时目录运行搬迁后的唯一AOT二进制。
        empty = workspace / 'empty-path'
        empty.mkdir()
        temporary = workspace / 'runtime-temp'
        temporary.mkdir()
        polluted = make_polluted_env(empty)
        polluted.update({key: str(temporary) for key in ('TMPDIR', 'TMP', 'TEMP')})
        cwd = workspace / '独立 运行目录'
        cwd.mkdir()
        actual = run([moved, name], cwd, polluted, data)
        assert actual.returncode == oracle.returncode
        assert (actual.stdout, actual.stderr) == (oracle.stdout, oracle.stderr)
        assert (cwd / '收据.txt').read_bytes() == expected_effect
        assert not list(temporary.iterdir()), f'执行后遗留解包目录：{list(temporary.iterdir())}'

        # Linux干净容器验证：无Python/.NET/SDK环境、断网隔离。
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
            assert actual.returncode == oracle.returncode
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
