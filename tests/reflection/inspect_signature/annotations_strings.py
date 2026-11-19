# ryupy: pending
# ADR0219独立后续输入：合法运行期字符串注解要求真实求值与副作用时机，本切保原阻断。
import inspect
import sys

events = []


def mark():
    events.append("annotation")
    return int


def target(value):
    return value


target.__annotations__ = {"value": "mark()", "return": "str"}
plain = inspect.signature(target)
print(plain.parameters["value"].annotation, events)
if sys.version_info >= (3, 10):
    evaluated = inspect.signature(target, globals={"mark": mark, "str": str}, eval_str=True)
    print(evaluated.parameters["value"].annotation is int, evaluated.return_annotation is str, events)
else:
    try:
        inspect.signature(target, eval_str=True)
    except TypeError:
        print("3.9-signature-keyword-boundary")
