"""C0/C1 产品真实验证手写探针输入。

本程序作为 RyuPy 编译产物真实验证用例：
1. 继承自 CPython collections.UserDict，触发类桥 _rp_make_class、_rp_calculate_meta 与 _rp_wrap_method；
2. 基础对象槽（__new__、__init__、__setattr__、__delattr__）在 C1 候选已由原生描述符接管，
   验证其不再调用已移除的 _rp_object_new、_rp_object_init、_rp_object_setattr、_rp_object_delattr；
3. 导入 logging 并在 Python 侧维持标准 atexit 清理链，确保进程关闭阶段存在真实 Python 执行帧；
4. 注册 atexit 回调验证确定性进程终止副作用；
5. 输出严格且确定，与固定 CPython 3.12.13 同次 oracle 执行比对一致。
"""
from __future__ import annotations

import atexit
from collections import UserDict
import logging


def audit_atexit_hook() -> None:
    """关闭阶段触发的 atexit 回调，输出确定性文本供三流一致性验证。"""
    print("AUDIT_ATEXIT_SHUTDOWN_CONFIRMED")


# 注册退出回调
atexit.register(audit_atexit_hook)


class AuditedRecord(UserDict):
    """跨域继承 UserDict，触发类构建与方法包装 helper。"""

    def __init__(self, initial=None):
        super().__init__(initial)

    def format_summary(self) -> str:
        return f"record:{len(self)}"


def main() -> None:
    # 1. 实例化：经由 AllocateFromBaseObject 与 InitializeFromBaseObject 原生描述符
    rec = AuditedRecord({"k1": "v1", "k2": "v2"})

    # 2. 属性写入：经由 SetAttrFromBaseObject 原生描述符
    rec.custom_slot = "audited_payload"
    assert rec.custom_slot == "audited_payload"
    assert rec.format_summary() == "record:2"

    # 3. 属性删除：经由 DelAttrFromBaseObject 原生描述符
    del rec.custom_slot

    # 4. 删除后读取抛出 AttributeError，验证异常行为
    try:
        _ = rec.custom_slot
    except AttributeError:
        print("ATTRIBUTE_ERROR_AFTER_DELETE_CONFIRMED")
    else:
        raise AssertionError("读取已删除属性未抛出 AttributeError")

    # 5. 运行时阶段正常完成标记
    print("AUDIT_RUNTIME_CONFIRMED")


if __name__ == "__main__":
    main()
