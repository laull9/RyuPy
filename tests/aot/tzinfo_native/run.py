"""独立tzinfo用户单基Native图；只复用冻结工具，不改变Decimal图或其计数。"""
import importlib.util
import json
import os
import shutil
import sys
import time
import traceback
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(HERE.parent/'decimal_finite_native'))
from contract import require, sha, context, PYTHON_SHA
spec = importlib.util.spec_from_file_location('decimal_native_support', HERE.parent/'decimal_finite_native/run.py')
base = importlib.util.module_from_spec(spec)
spec.loader.exec_module(base)


def remaining(deadline, now):
    # 外层25分钟硬上限前留30秒给finally恢复和原件落盘。
    seconds = int(deadline - now - 30)
    require(seconds > 0, '共享验证预算耗尽，禁止开始新命令')
    return seconds


def fixture(folder):
    data = json.loads((folder/'SOURCE.json').read_text())
    require(data['python'] == 'CPython 3.12.13' and data['python_sha256'] == PYTHON_SHA, 'CP身份变化')
    names = {'entry.py', 'zones.py', 'stdout.txt'}
    require(len(data['files']) == 3 and {x['path'] for x in data['files']} == names, '样例清单变化')
    require({p.name for p in folder.iterdir()} == names | {'SOURCE.json'}, '额外样例来源')
    for row in data['files']:
        p = folder/row['path']
        require(p.is_file() and not p.is_symlink() and p.stat().st_size == row['size'] and sha(p) == row['sha256'], '样例原件变化')
    return (folder/'stdout.txt').read_bytes()


class Runner(base.Runner):
    def __init__(self):
        self.out = ROOT/'target/tzinfo-native-evidence'; self.out.mkdir(parents=True, exist_ok=False)
        self.python = Path(os.environ['PYTHON']).resolve(strict=True)
        self.dotnet = Path(os.environ['DOTNET']).resolve(strict=True)
        require(sys.version_info[:3] == (3, 12, 13) and sha(self.python) == PYTHON_SHA, '固定CP身份不同')
        self.deadline = float(os.environ['NATIVE_GRAPHS_DEADLINE'])
        remaining(self.deadline, time.time())
        self.env = dict(os.environ, PYTHONDONTWRITEBYTECODE='1', PYTHONHASHSEED='0')
        self.labels = set(); self.frozen = {}
        self.result = {'passed': False, 'graph': 'tzinfo-user-single-base', 'stages': {}, 'native_executions': 0}
        self.save('result.json', self.result)

    def command(self, label, argv, timeout=120, env=None):
        return super().command(label, argv, min(timeout, remaining(self.deadline, time.time())), env)


def prepare(r):
    previous = ROOT/'target/decimal-finite-native-evidence'
    result = json.loads((previous/'result.json').read_text())
    require(result.get('passed') is True and result.get('source_after_verified') is True
            and result.get('native_executions') == 2, 'Decimal图未完成，禁止复用不明工具')
    r.before = base.identity(r, 'source-before')
    require(r.before == json.loads((previous/'source-after-files.json').read_text()), '两图源码不一致')
    r.freeze([ROOT/x['path'] for x in r.before])
    frozen = json.loads((previous/'frozen.json').read_text())
    tools = r.out/'tools'; tools.mkdir()
    r.cli = tools/'ryupy'; r.runtime = tools/'RyuPy.Runtime.dll'
    for src, dest in ((previous/'tools/ryupy', r.cli), (previous/'tools/RyuPy.Runtime.dll', r.runtime)):
        require(sha(src) == frozen[str(src)], '共享工具已变化'); shutil.copy2(src, dest)
    for path in (r.python, r.dotnet): require(sha(path) == frozen[str(path)], '解释器或SDK变化')
    r.save('shared-tools.json', {'source_graph': 'decimal-finite-native', 'cli_sha256': sha(r.cli),
                               'runtime_sha256': sha(r.runtime), 'rebuilt': False})
    r.env.update(RYUPY_RUNTIME_DIR=str(tools), DOTNET=str(r.dotnet), DOTNET_ROOT=str(r.dotnet.parent))
    r.freeze([r.cli, r.runtime, r.python, r.dotnet])
    r.inputs = r.out/'inputs'; shutil.copytree(HERE/'fixtures', r.inputs)
    r.golden = fixture(r.inputs); r.entry = r.inputs/'entry.py'
    r.freeze([p for p in r.inputs.iterdir() if p.is_file()])


def main():
    context(os.environ, json.loads(Path(os.environ['GITHUB_EVENT_PATH']).read_text())); r = Runner()
    try:
        r.phase('reuse-frozen-tools', prepare)
        r.phase('managed', base.managed); r.phase('native', base.native)
        require(base.identity(r, 'source-after') == r.before, '完整源码发生改变')
        r.result.update(passed=True, source_after_verified=True,
                        scope='tzinfo普通用户单基与aware回调；非MI/完整datetime/真实toml')
    except Exception:
        r.result['error'] = traceback.format_exc(); raise
    finally: r.save('result.json', r.result)


if __name__ == '__main__': main()
