"""测试控制面：模拟模块缓存与失败/重导事务，弱观察 F 和 globals 内哨兵。

生产候选胶水仅 bridge.c 内固定 factory/f；这里的准备、断言和模拟异常均为测试代码。
"""
import gc
import sys
import types
import weakref

gc.disable()  # 明确由测试循环触发收集，避免阈值造成时序漂移。
watches = []


class Sentinel:
    """globals 的存活哨兵，不提供 __del__ 或额外根。"""


def prepare(offset):
    global m, d
    m = types.ModuleType("proof")
    d = m.__dict__
    d["offset"] = offset
    d["sentinel"] = Sentinel()


def observe():
    f = d["f"]
    assert isinstance(f, types.FunctionType)
    assert f.__globals__ is d
    assert f.__defaults__ is None and f.__kwdefaults__ is None
    assert f.__code__.co_filename == "<string>"
    assert f.__code__.co_freevars == ("p",)
    assert f.__closure__[0].cell_contents.__name__ == "cil_dispatch"
    watches.append((weakref.ref(f), weakref.ref(d["sentinel"])))


def check_conversion_reentry():
    """__index__ 改写 globals 时，原 offset 必须活到本次转换结束。"""
    f = sys.modules["proof"].f
    state = []

    class Offset(int):
        def __del__(self):
            state.append("released")

    class Argument:
        def __index__(self):
            f.__globals__["offset"] = 10
            gc.collect()
            assert not state, "跨回调借用的 offset 已提前释放"
            return 5

    f.__globals__["offset"] = Offset(1234567)
    assert f(Argument()) == 1234572
    assert state == ["released"]


def fail_import():
    """由真实 importlib 事务安装并回滚 sys.modules，验证外逃 H 的旧 D。"""
    import importlib
    import importlib.util

    class FailingLoader:
        def __init__(self, candidate):
            self.candidate = candidate

        def find_spec(self, fullname, path=None, target=None):
            if fullname == "proof":
                return importlib.util.spec_from_loader(fullname, self)
            return None

        def create_module(self, spec):
            return self.candidate

        def exec_module(self, module):
            assert sys.modules["proof"] is module
            assert module.f.__globals__ is module.__dict__
            raise RuntimeError("测试模块执行失败")

    loader = FailingLoader(sys.modules.pop("proof"))
    sys.meta_path.insert(0, loader)
    try:
        try:
            importlib.import_module("proof")
        except RuntimeError as error:
            assert str(error) == "测试模块执行失败"
        else:
            raise AssertionError("导入应当失败")
        assert "proof" not in sys.modules
    finally:
        sys.meta_path.remove(loader)


def publish():
    """通过真实 importlib 建立成功缓存；候选模块体只安装预建的 CIL 薄函数。"""
    import importlib
    import importlib.util

    class Loader:
        def find_spec(self, fullname, path=None, target=None):
            if fullname == "proof":
                return importlib.util.spec_from_loader(fullname, self)
            return None

        def create_module(self, spec):
            return m

        def exec_module(self, module):
            assert module.f.__globals__ is module.__dict__
            assert module.f(2) == 2 + module.offset  # 同次 CPython oracle，仅测试控制面。

    loader = Loader()
    sys.meta_path.insert(0, loader)
    try:
        assert importlib.import_module("proof") is m
    finally:
        sys.meta_path.remove(loader)
