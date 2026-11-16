# ryupy: pending
"""C4-2 严格无字节码内核产品接入待办手写验证入口。

覆盖 ADR-0221 核心不变量（与固定 CPython 3.12.13 语义一致）：
1. 内建 math 模块真实计算与 sys.modules 身份；
2. 多层 CIL 包、fromlist、相对导入；
3. globals() / mod.__dict__ / fn.__globals__ 返回同一实时 dict 身份，验证
   type(d) is dict、isinstance(d, dict)、iter/keys/values/items、in、[]= / del d[k]；
4. __builtins__ 真实字典捕获及重绑隔离；
5. 循环初始化单一事务语义；
6. 初始化失败：从 sys.modules 清除但被用户持有的部分模块继续存活，重导得新身份；
7. del sys.modules[name] 后重新导入得到全新模块与 __dict__ 身份，旧引用独立保活；
8. 进程关闭阶段 atexit 回调与 shutdown 合法延迟导入。
"""
import atexit
import math
import os
import sys

from cil_pkg import PKG_TRACE
import cil_pkg
import cil_pkg.sub as sub


def inspect_builtin_probe(seq):
    """用于核查函数 __globals__ 与 __builtins__ 真实字典身份。"""
    return len(seq)


def on_shutdown() -> None:
    """在 shutdown 阶段延迟导入并确认输出。"""
    import cil_pkg.shutdown_hook as hook

    assert math.isfinite(math.pi)
    assert hook.SHUTDOWN_STAMP == "atexit-cil-import:12"
    print(f"C4_2_SHUTDOWN_OK {hook.SHUTDOWN_STAMP}")


atexit.register(on_shutdown)


def main() -> None:
    # 1. 内建 math
    assert int(math.sqrt(81)) == 9
    assert "math" in sys.modules

    # 2. 包与相对导入、__dict__ / globals() / __globals__ 同一对象与 exact dict 身份
    assert sub.describe() == "c4-2-pkg-v1:70"
    assert sys.modules["cil_pkg.sub"] is sub
    assert sub.read_live_slot("VERSION") == "c4-2-pkg-v1"
    live_ns = sub.read_live_slot.__globals__
    assert live_ns is sub.__dict__
    assert type(live_ns) is dict and isinstance(live_ns, dict)
    assert type(globals()) is dict and isinstance(globals(), dict)
    assert inspect_builtin_probe([1, 2, 3]) == 3
    assert isinstance(inspect_builtin_probe.__builtins__, dict)
    # CPython 导入模块默认捕获字典；主脚本入口允许保留 builtins 模块对象。
    assert isinstance(sub.__dict__["__builtins__"], dict)

    # 3. 实时 dict 协议：iter / keys / items / in / []= / del d[k] 与 getattr 联动
    sub.__dict__["EXT_INJECTED"] = "from_dict"
    assert "EXT_INJECTED" in sub.__dict__
    assert sub.read_live_slot("EXT_INJECTED") == "from_dict"
    sub.write_live_slot("INT_INJECTED", "from_globals")
    assert getattr(sub, "INT_INJECTED") == "from_globals"
    keys_seen = [k for k in sub.__dict__]
    assert "EXT_INJECTED" in keys_seen and "INT_INJECTED" in keys_seen
    assert ("INT_INJECTED", "from_globals") in list(sub.__dict__.items())
    del sub.__dict__["EXT_INJECTED"]
    assert "EXT_INJECTED" not in sub.__dict__

    # 4. 循环初始化不重复执行
    import cil_pkg.cyclic_a as cyclic_a

    assert cyclic_a.EXEC_COUNT == 1
    assert cyclic_a.AFTER_B == 33
    assert cyclic_a.SELF_IS_IN_SYS is True
    assert sys.modules["cil_pkg.cyclic_b"].HAS_AFTER_YET is False
    assert PKG_TRACE[:4] == ["a:enter", "b:enter", "b:exit", "a:exit"]

    # 5. 初始化失败回滚：sys.modules 清除，但外部捕获的部分模块保持存活；重导生成新身份
    os.environ["RYUPY_C4_2_FAIL_FLAKY"] = "1"
    try:
        import cil_pkg.flaky_init as flaky_failed  # noqa: F401
    except RuntimeError as exc:
        assert "simulated flaky cil exec failure" in str(exc)
    else:
        raise AssertionError("首次导入预期触发 RuntimeError")
    assert "cil_pkg.flaky_init" not in sys.modules
    partial_held = cil_pkg.CAPTURED_PARTIAL_FLAKY
    assert partial_held.PARTIAL_BEFORE_FAIL == "must_be_rolled_back"

    os.environ["RYUPY_C4_2_FAIL_FLAKY"] = "0"
    import cil_pkg.flaky_init as flaky_ok

    assert flaky_ok is not partial_held
    assert flaky_ok.__dict__ is not partial_held.__dict__
    assert flaky_ok.COMPLETE_VALUE == "clean_retry_ok"
    assert not hasattr(flaky_ok, "PARTIAL_BEFORE_FAIL")
    assert partial_held.PARTIAL_BEFORE_FAIL == "must_be_rolled_back"

    # 6. 删除 sys.modules 缓存后重导生成全新身份，旧引用保持旧值
    import cil_pkg.reimport_target as first_mod

    assert first_mod.TOKEN == "gen-1"
    del sys.modules["cil_pkg.reimport_target"]
    assert "cil_pkg.reimport_target" not in sys.modules
    import cil_pkg.reimport_target as second_mod

    assert second_mod is not first_mod
    assert second_mod.__dict__ is not first_mod.__dict__
    assert first_mod.TOKEN == "gen-1"
    assert second_mod.TOKEN == "gen-2"

    print("C4_2_RUNTIME_OK")


if __name__ == "__main__":
    main()
