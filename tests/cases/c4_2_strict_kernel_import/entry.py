# ryupy: pending
"""C4-2 严格无字节码内核产品接入待办手写验证入口。

本用例验证 C4-2 计划中的严格内核 + CIL 导入核心语义（与固定 CPython 3.12.13 行为一致）：
1. 内建 math 模块真实计算与 sys.modules 身份；
2. 多层级 CIL 包、fromlist 与相对导入；
3. 模块 __dict__ / globals() / sys.modules 实时同源双向读写（无快照失真）；
4. 循环初始化单一事务语义（exec_module 前入 sys.modules，不重复执行）；
5. 初始化异常回滚与洁净二次导入重试；
6. 进程关闭阶段 atexit 回调与 shutdown 合法延迟导入。

注：独立原生 .so 扩展交叉导入与三交付形态（JIT / NativeAOT / OneFile）搬迁门控由
tests/cil_execution 严格审计产品门验证（ADR-0221 §7.1）。
"""
import atexit
import math
import os
import sys

from cil_pkg import PKG_TRACE
import cil_pkg.sub as sub


def on_shutdown() -> None:
    """在 shutdown 阶段（_PyAtExit_Call）延迟导入并确认完整输出。"""
    import cil_pkg.shutdown_hook as hook

    assert math.isfinite(math.pi)
    assert hook.SHUTDOWN_STAMP == "atexit-cil-import:12"
    print(f"C4_2_SHUTDOWN_OK {hook.SHUTDOWN_STAMP}")


atexit.register(on_shutdown)


def main() -> None:
    # 1. 内建 math
    assert int(math.sqrt(81)) == 9
    assert "math" in sys.modules

    # 2. 包与相对导入
    assert sub.describe() == "c4-2-pkg-v1:70"
    assert sys.modules["cil_pkg.sub"] is sub

    # 3. 实时 __dict__ / globals() 双向同步
    sub.__dict__["EXT_INJECTED"] = "from_dict"
    assert sub.read_live_slot("EXT_INJECTED") == "from_dict"
    sub.write_live_slot("INT_INJECTED", "from_globals")
    assert getattr(sub, "INT_INJECTED") == "from_globals"
    assert sys.modules["cil_pkg.sub"].__dict__["INT_INJECTED"] == "from_globals"

    # 4. 循环初始化不重复执行且共享同一身份
    import cil_pkg.cyclic_a as cyclic_a

    assert cyclic_a.EXEC_COUNT == 1
    assert cyclic_a.AFTER_B == 33
    assert cyclic_a.SELF_IS_IN_SYS is True
    assert sys.modules["cil_pkg.cyclic_b"].HAS_AFTER_YET is False
    assert PKG_TRACE == ["a:enter", "b:enter", "b:exit", "a:exit"]

    # 5. 初始化失败自动从 sys.modules 与 Runtime 回滚，清理后二次导入成功
    os.environ["RYUPY_C4_2_FAIL_FLAKY"] = "1"
    try:
        import cil_pkg.flaky_init  # noqa: F401
    except RuntimeError as exc:
        assert "simulated flaky cil exec failure" in str(exc)
    else:
        raise AssertionError("首次导入预期触发 RuntimeError")
    assert "cil_pkg.flaky_init" not in sys.modules

    os.environ["RYUPY_C4_2_FAIL_FLAKY"] = "0"
    import cil_pkg.flaky_init as flaky_ok

    assert flaky_ok.COMPLETE_VALUE == "clean_retry_ok"
    assert not hasattr(flaky_ok, "PARTIAL_BEFORE_FAIL")

    print("C4_2_RUNTIME_OK")


if __name__ == "__main__":
    main()
