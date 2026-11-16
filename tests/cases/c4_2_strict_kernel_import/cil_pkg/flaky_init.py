"""C4-2 初始化失败回滚与二次重试验证模块。"""
import os

if os.environ.get("RYUPY_C4_2_FAIL_FLAKY") == "1":
    PARTIAL_BEFORE_FAIL = "must_be_rolled_back"
    raise RuntimeError("simulated flaky cil exec failure")

COMPLETE_VALUE = "clean_retry_ok"
