"""测试控制面：模拟模块缓存与失败/重导事务，弱观察 F 和 globals 内哨兵。

生产候选胶水仅 bridge.c 内固定 factory/f；这里的准备、断言和模拟异常均为测试代码。
"""
import gc
import sys
import types
import weakref

gc.disable()  # 明确由测试循环触发收集，避免阈值造成时序漂移。
watches = []
creation_trace = {"factory_returns": 0, "calls": 0}


def begin_creation_trace():
    """旧 factory 的 return hook 会提前调用 F，回归锁住初始化期间的发布顺序。"""
    creation_trace.update(factory_returns=0, calls=0)

    def trace(frame, event, arg):
        if frame.f_code.co_name == "_factory" and event == "return":
            creation_trace["factory_returns"] += 1
            arg(1)
        if frame.f_code.co_name == "f" and event == "call":
            creation_trace["calls"] += 1
        return trace

    sys.settrace(trace)


def end_creation_trace():
    sys.settrace(None)
    assert creation_trace["factory_returns"] == 0
    assert creation_trace["calls"] > 0, "创建期 trace 必须实际观察到发布后的 F"


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


resurrected = []
resurrected_watch = None


class ResurrectionKeeper:
    """在 GC finalizer 中直接进入真实 CIL，然后把原 F 保存在 CP 端。"""
    def __init__(self, function):
        self.function = function

    def __del__(self):
        assert self.function(2) == 52
        resurrected.append(self.function)


def arm_function_resurrection():
    global resurrected_watch
    function = sys.modules["proof"].f
    function.marker = ResurrectionKeeper(function)
    resurrected_watch = weakref.ref(function)


def collect_function_resurrection():
    del sys.modules["proof"]
    gc.collect()
    assert resurrected_watch() is None
    assert len(resurrected) == 1 and resurrected[0](2) == 52
    # 旧 F 与 globals 哨兵的 weakref 均已被 GC 清空；复活后新建观察，不改旧 weakref 语义。
    function = resurrected[0]
    watches.append((weakref.ref(function), weakref.ref(function.__globals__["sentinel"])))


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
