# ryupy: pending
# ADR0219独立域边界：同一手写源码分两臂，Instance须与CP等价，Legacy须明确NI。
# 原始CP正例保留；Legacy的明确失败另由Runtime/driver负门登记，不改成本文件的伪成功。
import inspect


def target(value: int = 1) -> int:
    return value


sig = inspect.signature(target)
print(sig.parameters["value"].default, sig.return_annotation is int)
print(target.__globals__ is globals())
