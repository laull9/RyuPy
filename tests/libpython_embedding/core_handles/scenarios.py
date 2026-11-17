"""测试控制面：模拟模块缓存与失败/重导事务，弱观察 F 和 globals 内哨兵。

生产候选胶水仅 bridge.c 内固定 factory/f；这里的准备、断言和模拟异常均为测试代码。
"""
import gc
import sys
import types
import weakref

watches = []


class Sentinel:
    """globals 的存活哨兵，不提供 __del__ 或额外根。"""


def prepare(offset):
    global m, d
    m = types.ModuleType("proof")
    d = m.__dict__
    d["offset"] = offset
    d["sentinel"] = Sentinel()
    sys.modules["proof"] = m


def observe():
    f = d["f"]
    assert isinstance(f, types.FunctionType)
    assert f.__globals__ is d
    assert f.__defaults__ is None and f.__kwdefaults__ is None
    assert f.__code__.co_filename == "<string>"
    assert f.__code__.co_freevars == ("p",)
    assert f.__closure__[0].cell_contents.__name__ == "cil_dispatch"
    watches.append((weakref.ref(f), weakref.ref(d["sentinel"])))
