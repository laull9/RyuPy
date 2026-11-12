"""独立Enum auto有限图：默认工具、CP、双PIC、真实ELF原位及搬迁。"""
import importlib.util
import json
import os
import sys
import traceback
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent/'decimal_finite_native'))
from contract import context, require
spec = importlib.util.spec_from_file_location('enum_native_shared', HERE.parent/'decimal_finite_native/run.py')
base = importlib.util.module_from_spec(spec); spec.loader.exec_module(base)
# 仅此进程的独立模块对象重定位输入；旧Decimal文件、fixture与结果不变。
base.HERE = HERE


class Runner(base.Runner):
    def __init__(self):
        self.out = base.ROOT/'target/enum-auto-native-evidence'; self.out.mkdir(parents=True, exist_ok=False)
        self.python = Path(os.environ['PYTHON']).resolve(strict=True)
        self.dotnet = Path(os.environ['DOTNET']).resolve(strict=True)
        require(sys.version_info[:3] == (3, 12, 13) and base.sha(self.python) == base.PYTHON_SHA, '固定CP身份不同')
        self.env = dict(os.environ, PYTHONDONTWRITEBYTECODE='1', PYTHONHASHSEED='0')
        self.labels = set(); self.frozen = {}
        self.result = {'passed': False, 'graph': 'enum-auto-finite', 'stages': {}, 'native_executions': 0}
        self.save('result.json', self.result)


def main():
    context(os.environ, json.loads(Path(os.environ['GITHUB_EVENT_PATH']).read_text())); r = Runner()
    try:
        r.phase('prepare-default-tools', base.prepare)
        r.phase('managed', base.managed); r.phase('native', base.native)
        require(base.identity(r, 'source-after') == r.before, '完整源码发生改变')
        r.result.update(passed=True, source_after_verified=True,
                        scope='有限Enum/Flag整数auto、warning协议、普通类；非全MI/自定义hook/混scalar生成')
    except Exception:
        r.result['error'] = traceback.format_exc(); raise
    finally: r.save('result.json', r.result)


if __name__ == '__main__': main()
