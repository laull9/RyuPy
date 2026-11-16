"""C4-2 失败回滚与部分模块存活参考模块。"""
import os

if os.environ.get("RYUPY_C4_2_FAIL_FLAKY") == "1":
    PARTIAL_BEFORE_FAIL = "must_be_rolled_back"
    # 允许外部测试先捕获 sys.modules 中的半成品引用，验证其随后仍可用
    import cil_pkg  # noqa: F401
    import sys

    cil_pkg.CAPTURED_PARTIAL_FLAKY = sys.modules[__name__]
    raise RuntimeError("simulated flaky cil exec failure")

COMPLETE_VALUE = "clean_retry_ok"
