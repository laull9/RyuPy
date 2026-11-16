# ADR0219：同一函数读取当前元数据，旧签名固定结构并保留原对象，检查不调用正文。
import inspect

calls = []
first_default = ["first"]
second_default = ["second"]


def target(value=first_default, *, option=1):
    calls.append(value)
    return value


annotations = {"value": int, "return": str}
target.__annotations__ = annotations
old = inspect.signature(target)
print(old.parameters["value"].default is first_default)
print(old.parameters["value"].annotation is int, old.return_annotation is str)
target.__defaults__ = (second_default,)
keywords = {"option": second_default}
target.__kwdefaults__ = keywords
annotations["value"] = str
annotations["return"] = int
new = inspect.signature(target)
print(new.parameters["value"].default is second_default)
print(new.parameters["option"].default is second_default)
print(new.parameters["value"].annotation is str, new.return_annotation is int)
print(old.parameters["value"].annotation is int, old.return_annotation is str)
old_bound = old.bind()
old_bound.apply_defaults()
print(old_bound.arguments["value"] is first_default, old_bound.arguments["option"])
first_default.append("changed")
print(old.parameters["value"].default)
keywords["option"] = 9
print(inspect.signature(target).parameters["option"].default)
print(new.parameters["option"].default is second_default, len(calls))
print(target() is second_default)
del target.__defaults__
del target.__kwdefaults__
del target.__annotations__
last = inspect.signature(target)
print(last.parameters["value"].default is inspect.Parameter.empty)
print(last.parameters["option"].default is inspect.Parameter.empty)
print(last.parameters["value"].annotation is inspect.Parameter.empty)
print(last.return_annotation is inspect.Signature.empty)
try:
    target()
except TypeError:
    print("next-call-missing")
print(len(calls))
