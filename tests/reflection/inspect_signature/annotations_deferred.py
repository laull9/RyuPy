# ryupy: pending
# ADR0219独立后续原始输入：3.14/3.15延迟注解；本轮及首切值注解通过后仍保pending。
# 仅在相应版本lane运行，严禁用提前求值或假NameError满足本合同。
import inspect
import sys

if sys.version_info < (3, 14):
    print("version-before-lazy-annotations")
else:
    from annotationlib import Format
    events = []

    def mark():
        events.append("annotation")
        return int

    def target(value: mark()) -> mark():
        return value

    print("defined", events)
    text = inspect.signature(target, annotation_format=Format.STRING)
    print("string", events, isinstance(text.parameters["value"].annotation, str))
    first = inspect.signature(target, annotation_format=Format.VALUE)
    print("value", events, first.parameters["value"].annotation is int)
    target.__annotations__ = {"value": str, "return": str}
    second = inspect.signature(target)
    print("replaced", second.parameters["value"].annotation is str)
