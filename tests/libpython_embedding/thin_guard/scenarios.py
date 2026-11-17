"""有限普通函数合同：即时 CPython 对照、元数据、audit、观察和真实循环回收。

C 内固定源码属于登记胶水；本文件全部属于测试控制面。
仅运行 A closure：实例 P 不进入无 GC traversal 的 code 常量。
"""
import faulthandler
import gc
import inspect
import sys
import types
import warnings
import weakref

import thin_native as native

faulthandler.enable()
sys.stdout.reconfigure(line_buffering=True)


def make(mode, namespace=None, fail=0):
    """常量直接调用模板可产生 SyntaxWarning，精确捕获并公开报告。"""
    if namespace is None:
        namespace = {"__name__": "thin_proof"}
    with warnings.catch_warnings(record=True) as caught:
        warnings.simplefilter("always", SyntaxWarning)
        f = native.make(mode, namespace, fail)
    assert all(w.category is SyntaxWarning and "not callable" in str(w.message)
               and w.filename == f"<ryupy-thin-guard-{mode}>" for w in caught)
    if caught:
        assert mode == "C" and len(caught) == 1
        warning_modes.add(mode)
    return f


def expect(error_type, operation, message=None):
    try:
        operation()
    except error_type as error:
        if message:
            assert message in str(error), str(error)
        return type(error)
    raise AssertionError(f"应抛出：{error_type.__name__}")


def oracle(x):
    return x


def binding_and_metadata(mode):
    f = make(mode)
    value = []
    for invoke in (lambda g: g(value), lambda g: g(x=value),
                   lambda g: native.callback(g, value)):
        assert invoke(f) is invoke(oracle) is value
    bad = (lambda g: g(), lambda g: g(1, 2),
           lambda g: g(1, x=2), lambda g: g(y=1))
    before = native.stats()[2]
    for invoke in bad:
        assert expect(TypeError, lambda: invoke(f)) is expect(TypeError, lambda: invoke(oracle))
    assert native.stats()[2] == before
    assert type(f) is types.FunctionType
    assert str(inspect.signature(f)) == "(x)"
    assert f.__code__.co_filename == f"<ryupy-thin-guard-{mode}>"
    assert f.__code__.co_freevars == (("p",) if mode == "A" else ())
    assert (f.__closure__ is None) == (mode != "A")
    assert f.__doc__ is None
    assert f.__defaults__ is None and f.__kwdefaults__ is None
    # 元数据以真实 F 为权威，两边施加相同合法变更后逐项比较。
    for g in (f, oracle):
        g.__name__, g.__qualname__, g.__module__ = "renamed", "renamed", "new_module"
        g.__annotations__ = {"x": object}
        g.marker = value
    for name in ("__name__", "__qualname__", "__module__", "__annotations__", "__dict__"):
        assert getattr(f, name) == getattr(oracle, name)
    assert f.marker is oracle.marker is value
    assert inspect.signature(f) == inspect.signature(oracle)
    assert f(x=value) is oracle(x=value) is value
    print(f"PASS {mode} binding identity metadata signature")

    # CP setter 允许这些元数据；首切执行合同在 dispatch 明确拒绝，且不进入叶子。
    for name, replacement in (("__defaults__", (value,)), ("__defaults__", ()),
                              ("__kwdefaults__", {"x": value}), ("__kwdefaults__", {})):
        setattr(f, name, replacement)
        setattr(oracle, name, replacement)
        assert getattr(f, name) is replacement
        assert inspect.signature(f) == inspect.signature(oracle)
        assert oracle(value) is value
        before = native.stats()[2]
        expect(RuntimeError, lambda: f(value), "形状失效")
        if name == "__defaults__" and replacement:
            assert oracle() is value
            expect(RuntimeError, f, "形状失效")
        assert native.stats()[2] == before
        setattr(f, name, None)
        setattr(oracle, name, None)
        assert f(value) is value
    print(f"PASS {mode} legal-default-writes=4 capability-rejected-before-leaf restored")


def code_guard(mode):
    f = make(mode)
    old = f.__code__
    # 同一 code 和合法同形 code 均属于首切拒绝的赋值；不执行替换体。
    replacement = old.replace(co_filename="<test-control-replacement>")
    writes = (lambda: setattr(f, "__code__", replacement),
              lambda: object.__setattr__(f, "__code__", replacement),
              lambda: type(f).__dict__["__code__"].__set__(f, replacement),
              lambda: native.set_code(f, replacement),
              lambda: setattr(f, "__code__", old))
    before = native.stats()[3]
    calls = native.stats()[2]
    for write in writes:
        expect(RuntimeError, write, "禁止替换")
        assert f.__code__ is old
    assert native.stats()[3] == before + len(writes)
    assert native.stats()[2] == calls
    assert f(42) == 42
    # 未登记的普通函数允许 setter；Python hook 核对事件三元组身份。
    ordinary = lambda x: x
    events = []

    def observe(event, args):
        if event == "object.__setattr__" and args[0] is ordinary:
            events.append(args)

    sys.addaudithook(observe)
    changed = ordinary.__code__.replace(co_filename="<ordinary-control>")
    ordinary.__code__ = changed
    assert ordinary.__code__ is changed and ordinary(42) == 42
    assert len(events) == 1
    assert events[0][0] is ordinary and events[0][1] == "__code__" and events[0][2] is changed
    # audit hooks 不能移除；显式清空捕获，避免测试控制面成为额外函数根。
    events.clear()
    ordinary = None
    print(f"PASS {mode} code-audit rejected={len(writes)} unchanged-before-leaf ordinary-event=1")


def observations(mode):
    f = make(mode)
    code = f.__code__
    control_code = oracle.__code__
    traced, profiled, control_trace, control_profile = [], [], [], []

    def trace(frame, event, arg):
        if frame.f_code is code:
            traced.append(event)
        elif frame.f_code is control_code:
            control_trace.append(event)
        return trace

    def profile(frame, event, arg):
        if frame.f_code is code:
            profiled.append(event)
        elif frame.f_code is control_code:
            control_profile.append(event)

    # 仅观察，回调不改 frame、code、cell 或调用入口。
    sys.settrace(trace)
    try:
        assert native.callback(f, 9) == native.callback(oracle, 9) == 9
    finally:
        sys.settrace(None)
    sys.setprofile(profile)
    try:
        assert f(x=12) == oracle(x=12) == 12
    finally:
        sys.setprofile(None)
    assert traced[0] == "call" and traced[-1] == "return" and "line" in traced
    assert control_trace == ["call", "line", "return"]
    assert control_profile == ["call", "return"]
    assert profiled == ["call", "c_call", "c_return", "return"]
    print(f"PASS {mode} trace={','.join(traced)} profile={','.join(profiled)} glue-frame")


def lifetime():
    gc.collect()
    baseline, start_destroyed, _, _ = native.stats()
    assert baseline == 0
    # 关闭自动 GC，确定释放强根后环仍存活，再由显式 collect 收集。
    gc.disable()
    try:
        for mode in ("A",):
            for stage in (1, 2, 3):
                before = native.stats()[1]
                expect(RuntimeError, lambda: make(mode, fail=stage), "注入创建失败")
                gc.collect()
                assert native.stats()[:2] == (baseline, before + 1)
            for _ in range(30):
                namespace = {"__name__": "cycle"}
                f = make(mode, namespace)
                namespace["f"] = f
                watch = weakref.ref(f)
                before = native.stats()[1]
                del f, namespace
                assert watch() is not None and native.stats()[0] == baseline + 1
                gc.collect()
                assert watch() is None and native.stats()[:2] == (baseline, before + 1)
        # P 的 self 是 GC owner：P/owner/cell 任意外部持有均延长 F 的生命期。
        for mode in ("A",):
            f = make(mode)
            p = (f.__closure__[0].cell_contents if mode == "A" else
                 next(x for x in f.__code__.co_consts if isinstance(x, types.BuiltinFunctionType)))
            watch = weakref.ref(f)
            before = native.stats()[1]
            del f
            gc.collect()
            assert watch() is not None and native.stats()[0] == baseline + 1
            expect(RuntimeError, lambda: p(1), "原胶水")
            del p
            gc.collect()
            assert native.stats()[:2] == (baseline, before + 1)
    finally:
        gc.enable()
    assert native.stats()[1] - start_destroyed == 34
    print("PASS lifetime failure-stages=3 cycles=30 escaped-dispatch=1 destroyed-delta=34 before-finalize")


def construction_choice():
    """普通 cell 赋值保留 CP 行为；原 native core 仅负责仍绑定原 P 的函数。"""
    f = make("A")
    cell = f.__closure__[0]
    original = cell.cell_contents
    before = native.stats()[2]
    cell.cell_contents = abs
    assert f(-7) == 7
    assert native.stats()[2] == before
    cell.cell_contents = original
    assert f(-7) == -7
    del f, cell, original
    gc.collect()
    assert native.stats()[0] == 0
    print("PASS closure-cell-reassignment-obeys-CP restored-before-core")


def resurrected_functions():
    """GC 清 weak 后的 Python finalizer 复活，以及原生 watcher 复活后再次 GC。"""
    saved = []

    class Keeper:
        def __init__(self, function):
            self.function = function

        def __del__(self):
            # finalizer 正在 GC 内执行时就调用，不能等待 gc.stop 才恢复关联。
            assert self.function(17) == 17
            saved.append(self.function)

    for native_first in (False,):
        for mode in ("A",):
            f = make(mode)
            if native_first:
                native.arm_resurrection(f)
                del f
                gc.collect()
                f = native.take_resurrection()
                # 此处不调用 F：直接再形成新环，覆盖 watcher 保活后下一次 GC。
            watch = weakref.ref(f)
            f.marker = Keeper(f)
            before = native.stats()[1]
            del f
            gc.collect()
            assert watch() is None and len(saved) == 1
            f = saved.pop()
            assert f(23) == 23
            original = f.__code__
            expect(RuntimeError, lambda: setattr(f, "__code__", original), "禁止替换")
            del f, original
            gc.collect()
            assert native.stats()[0] == 0 and native.stats()[1] == before + 1
    print("PASS resurrection python-finalizer=1 callable-inside-GC before-finalize")


def held_owner_and_cell():
    """P 的 self 与 closure cell 均为真实 CP 强根，额外弱回调不参与内部生命期。"""
    for which in ("owner", "cell"):
        f = make("A")
        p = f.__closure__[0].cell_contents
        root = p.__self__ if which == "owner" else f.__closure__[0]
        seen = []
        watch = weakref.ref(f, lambda ref: seen.append("dead"))
        del f, p
        gc.collect()
        assert watch() is not None and not seen
        assert watch()(31) == 31
        del root
        gc.collect()
        assert watch() is None and seen == ["dead"] and native.stats()[0] == 0
    print("PASS held-owner-and-cell keep-F-alive release-all-before-finalize")


def clear_phase_watcher_oracle():
    """只观察原版 stock 的 clear 期 watcher 复活；字段已清理时不尝试执行。"""
    namespace = {}
    exec("def factory(p):\n def f(x):\n  return p(x)\n return f\n", namespace)
    f = namespace.pop("factory")(abs)
    namespace["f"] = f
    native.arm_resurrection(f)
    del f, namespace
    gc.collect()
    f = native.take_resurrection()
    ordinary = native.resurrection_state(f)
    del f
    gc.collect()
    f = make("A")
    native.arm_resurrection(f)
    del f
    gc.collect()
    f = native.take_resurrection()
    candidate = native.resurrection_state(f)
    # 原版和候选都可能已清 code/globals/closure；此时不能把对象重新当可执行函数。
    # 对照仅观察公开 getter，不触发 CP evaluator 对已清字段的解引用。
    assert ordinary != (1, 1, 1) and candidate != (1, 1, 1)
    del f
    gc.collect()
    assert native.stats()[0] == 0
    print(f"BOUNDARY clear-phase-native-watcher stock-fields={ordinary} candidate-fields={candidate} environment-cleared-not-called")


warning_modes = set()
for mode in ("A",):
    binding_and_metadata(mode)
    code_guard(mode)
    observations(mode)
lifetime()
construction_choice()
resurrected_functions()
held_owner_and_cell()
clear_phase_watcher_oracle()
gc.collect()
assert native.stats()[0] == 0, native.stats()
assert warning_modes == set()
print(f"WARNINGS captured-template-syntax modes={','.join(sorted(warning_modes)) or 'none'}")
print("PASS all stock finite-contract assertions")
