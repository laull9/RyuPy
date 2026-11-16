"""路线 A 显式本地整模块与入口 CPython 委派真实端到端验收（JIT / Standalone / Onefile / 搬迁 / 负例）。"""
import argparse
import json
import os
from pathlib import Path
import shutil
import sys

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tests/cpython'))
from support import checked, environment, execute, external_workspace  # noqa: E402


def make_polluted_env(empty_path):
    env = environment()
    env['PATH'] = str(empty_path)
    if sys.platform == 'win32':
        env['PYTHONHOME'] = r'C:\不可访问\Python'
        env['PYTHONPATH'] = r'C:\不可访问\包'
    else:
        env['PYTHONHOME'] = '/不可访问 Python'
        env['PYTHONPATH'] = '/不可访问 包'
    return env


def test_compiled_entry_with_delegated_module(cli, python, workspace, env):
    """测试编译入口调用显式委派异步/反射本地包，并从委派包反向导入编译模块。"""
    project = workspace / 'case1-mixed'
    (project / 'async_svc').mkdir(parents=True)
    (project / 'sync_math.py').write_text(
        'from pathlib import Path\ndef combine(a, b):\n    assert Path(__file__).is_file()\n    return a * 10 + b\n',
        encoding='utf-8',
    )
    (project / 'async_svc/__init__.py').write_text(
        'from .core import run_pipeline, describe_signature\n',
        encoding='utf-8',
    )
    (project / 'async_svc/core.py').write_text(
        'import asyncio\n'
        'import inspect\n'
        'import sys\n'
        'import sync_math\n'
        '\n'
        'class AsyncCtx:\n'
        '    def __init__(self, log):\n'
        '        self.log = log\n'
        '    async def __aenter__(self):\n'
        '        self.log.append("enter")\n'
        '        return self\n'
        '    async def __aexit__(self, exc_type, exc, tb):\n'
        '        self.log.append("exit")\n'
        '        return False\n'
        '\n'
        'async def _compute(x: int, factor: int = 3) -> int:\n'
        '    await asyncio.sleep(0)\n'
        '    frame = sys._getframe(0)\n'
        '    assert frame.f_code.co_name == "_compute"\n'
        '    return sync_math.combine(x, factor)\n'
        '\n'
        'def describe_signature():\n'
        '    sig = inspect.signature(_compute)\n'
        '    return [(name, str(p.default)) for name, p in sig.parameters.items()]\n'
        '\n'
        'def run_pipeline(x):\n'
        '    log = []\n'
        '    async def main():\n'
        '        async with AsyncCtx(log):\n'
        '            return await _compute(x)\n'
        '    return asyncio.run(main()), log\n',
        encoding='utf-8',
    )
    entry = project / 'app.py'
    entry.write_text(
        'import async_svc\n'
        'import sync_math\n'
        'res, log = async_svc.run_pipeline(7)\n'
        'print("res:", res, "log:", log, "sig:", async_svc.describe_signature(), "direct:", sync_math.combine(2, 5))\n',
        encoding='utf-8',
    )

    oracle = checked(execute([python, '-X', 'utf8', '-B', entry], cwd=project, env=env), 'case1 oracle')

    # 负例：未显式声明 --cpython-module async_svc 时禁止静默回退
    no_flag = execute([cli, entry, '--mode', 'jit', '-o', workspace / 'case1-fail'], cwd=project, env=env)
    assert no_flag.returncode != 0
    assert b'E0100' in no_flag.stderr, no_flag.stderr

    # JIT 验证
    jit_out = workspace / 'case1-jit'
    checked(execute([cli, entry, '--mode', 'jit', '--cpython-module', 'async_svc', '-o', jit_out],
                    cwd=project, env=env), 'case1 jit build')
    report = json.loads((jit_out / 'cpython-provider.json').read_text(encoding='utf-8'))
    assert report['execution'] == 'ryupy_compiled_entry'
    assert report['entry_provider'] == 'ryupy_compiled_source'
    assert report['delegated_local_modules'] == ['async_svc']
    assert report['modules']['async_svc']['reason'] == 'explicit_local_delegation'
    jit_run = checked(execute([shutil.which('dotnet'), jit_out / 'app.dll'], cwd=workspace, env=env), 'case1 jit run')
    assert (jit_run.stdout, jit_run.stderr) == (oracle.stdout, oracle.stderr)

    # Onefile + 删除全部输入 + 搬迁中文空格路径 + 污染环境
    onefile_out = workspace / 'case1-onefile'
    checked(execute([cli, entry, '--onefile', '--cpython-module', 'async_svc', '-o', onefile_out],
                    cwd=project, env=env), 'case1 onefile build')
    exe_name = 'app.exe' if sys.platform == 'win32' else 'app'
    relocated = workspace / '搬迁 目录 & 空格'
    relocated.mkdir()
    moved = relocated / exe_name
    shutil.copy2(onefile_out / 'app.onefile' / exe_name, moved)
    if sys.platform != 'win32':
        moved.chmod(0o755)
    shutil.rmtree(project)
    shutil.rmtree(onefile_out)
    empty = workspace / 'empty1'
    empty.mkdir()
    actual = checked(execute([moved], cwd=relocated, env=make_polluted_env(empty)), 'case1 onefile run')
    assert (actual.stdout, actual.stderr) == (oracle.stdout, oracle.stderr)
    print('[delegation] case1 编译入口 + --cpython-module 异步/反射包 + 反向编译导入 + onefile搬迁通过', flush=True)


def test_delegated_entry_and_exit_contracts(cli, python, workspace, env):
    """测试 --cpython-entry 整入口委派、源码反射、atexit、退出码、异常以及禁静默回退。"""
    project = workspace / 'case2-entry'
    (project / 'routes').mkdir(parents=True)
    (project / 'compiled_calc.py').write_text(
        'def add_tax(v):\n    return v + 100\n',
        encoding='utf-8',
    )
    (project / 'routes/__init__.py').write_text(
        'from .items import fetch_item\n',
        encoding='utf-8',
    )
    (project / 'routes/items.py').write_text(
        'import asyncio\n'
        'import inspect\n'
        'async def fetch_item(item_id: int, label: str = "默认") -> dict:\n'
        '    await asyncio.sleep(0)\n'
        '    return {"id": item_id, "label": label}\n'
        'def get_source_line():\n'
        '    return "async def fetch_item" in inspect.getsource(fetch_item)\n',
        encoding='utf-8',
    )
    entry = project / 'probe_entry.py'
    entry.write_text(
        'import asyncio\n'
        'import atexit\n'
        'import inspect\n'
        'from pathlib import Path\n'
        'import sys\n'
        'import compiled_calc\n'
        'from routes import fetch_item\n'
        'from routes.items import get_source_line\n'
        '\n'
        'atexit.register(lambda: print("atexit-ok"))\n'
        '\n'
        'async def main(count: int = 2):\n'
        '    assert __name__ == "__main__"\n'
        '    assert Path(__file__).is_file()\n'
        '    item = await fetch_item(compiled_calc.add_tax(count), "中文")\n'
        '    assert get_source_line()\n'
        '    assert "count" in inspect.signature(main).parameters\n'
        '    assert sys._getframe(0).f_code.co_name == "main"\n'
        '    return item\n'
        '\n'
        'if __name__ == "__main__":\n'
        '    mode = sys.argv[1] if len(sys.argv) > 1 else "ok"\n'
        '    if mode == "exit7":\n'
        '        sys.exit(7)\n'
        '    if mode == "boom":\n'
        '        raise ValueError("delegated boom")\n'
        '    print("entry:", asyncio.run(main()), "args:", sys.argv[1:])\n',
        encoding='utf-8',
    )

    oracle = checked(execute([python, '-X', 'utf8', '-B', entry, 'hello', '世界'], cwd=project, env=env), 'case2 oracle')

    # 负例 1：未传入 --cpython-entry 时禁止静默回退
    no_entry = execute([cli, entry, '--mode', 'jit', '--cpython-module', 'routes', '-o', workspace / 'case2-no-entry'],
                       cwd=project, env=env)
    assert no_entry.returncode != 0 and b'E0100' in no_entry.stderr, no_entry.stderr

    # 负例 2：--cpython-entry 但未传 --cpython-module routes 时，routes 仍必须进编译图并报 E0100
    no_mod = execute([cli, entry, '--mode', 'jit', '--cpython-entry', '-o', workspace / 'case2-no-mod'],
                     cwd=project, env=env)
    assert no_mod.returncode != 0 and b'E0100' in no_mod.stderr, no_mod.stderr

    # 负例 3：缺失本地模块名 / --native 冲突
    missing_mod = execute([cli, entry, '--mode', 'jit', '--cpython-entry', '--cpython-module', 'nonexistent_mod',
                           '-o', workspace / 'case2-missing'], cwd=project, env=env)
    assert missing_mod.returncode != 0
    native_conflict = execute([cli, entry, '--native', '--cpython-entry'], cwd=project, env=env)
    assert native_conflict.returncode != 0

    # Standalone 构建与验证
    dist_root = workspace / 'case2-standalone'
    checked(execute([cli, entry, '--mode', 'standalone', '--cpython-entry', '--cpython-module', 'routes',
                     '-o', dist_root], cwd=project, env=env), 'case2 standalone build')
    dist_dir = dist_root / 'probe_entry.dist'
    cfg = json.loads((dist_dir / 'ryupy-cpython.json').read_text(encoding='utf-8'))
    assert cfg['entry_script'] == 'python/ryupy_entry/probe_entry.py', cfg
    assert cfg['search_paths'][0] == 'python/ryupy_delegated', cfg
    report = json.loads((dist_dir / 'cpython-provider.json').read_text(encoding='utf-8'))
    assert report['execution'] == 'cpython_delegated_entry'
    assert report['entry_provider'] == 'cpython_delegated_source'
    assert report['delegated_local_modules'] == ['routes']

    # 搬迁后删除源目录运行
    relocated = workspace / '独立入口 搬迁 & 空格'
    shutil.move(dist_dir, relocated)
    shutil.rmtree(dist_root)
    shutil.rmtree(project)
    exe = relocated / ('probe_entry.exe' if sys.platform == 'win32' else 'probe_entry')
    empty = workspace / 'empty2'
    empty.mkdir()
    polluted = make_polluted_env(empty)

    actual = checked(execute([exe, 'hello', '世界'], cwd=relocated, env=polluted), 'case2 standalone run')
    assert (actual.stdout, actual.stderr) == (oracle.stdout, oracle.stderr)

    exit7 = execute([exe, 'exit7'], cwd=relocated, env=polluted)
    assert exit7.returncode == 7, (exit7.returncode, exit7.stdout, exit7.stderr)
    assert b'atexit-ok' in exit7.stdout

    boom = execute([exe, 'boom'], cwd=relocated, env=polluted)
    assert boom.returncode == 1
    assert b'ValueError: delegated boom' in boom.stderr, boom.stderr

    # 负例 4：缺失随包入口脚本时必须明确失败
    entry_file = relocated / 'python/ryupy_entry/probe_entry.py'
    saved = entry_file.read_bytes()
    try:
        entry_file.unlink()
        missing_entry = execute([exe], cwd=relocated, env=polluted)
        assert missing_entry.returncode != 0
    finally:
        entry_file.write_bytes(saved)

    print('[delegation] case2 --cpython-entry 整入口委派 + 源码反射 + atexit/退出码/异常 + 负例通过', flush=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--ryupy', required=True)
    parser.add_argument('--python', default=sys.executable)
    parser.add_argument('--keep', action='store_true')
    args = parser.parse_args()
    cli = Path(args.ryupy).resolve()
    python = Path(args.python).resolve()
    workspace = external_workspace('ryupy-delegation-')
    env = environment()
    try:
        test_compiled_entry_with_delegated_module(cli, python, workspace, env)
        test_delegated_entry_and_exit_contracts(cli, python, workspace, env)
        print('[delegation] 全部路线 A 端到端测试通过！', flush=True)
    except BaseException:
        print('[delegation] 失败现场：', workspace, flush=True)
        raise
    finally:
        if not args.keep and workspace.exists():
            shutil.rmtree(workspace, ignore_errors=True)


if __name__ == '__main__':
    main()
