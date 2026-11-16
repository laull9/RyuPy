# ADR0219：inspect绑定不执行正文；apply_defaults与活arguments按真实参数协议回调。
from inspect import Parameter, Signature, signature

shared = []
calls = []


def target(a, /, b=shared, *args, c, d=None, **kwargs):
    calls.append(a)
    return a, b, args, c, d, kwargs


sig = signature(target)
bound = sig.bind(1, c=3)
print(list(bound.arguments), bound.args, bound.kwargs, len(calls))
bound.apply_defaults()
print(list(bound.arguments), bound.signature is sig)
print(type(bound.arguments) is dict)
print(bound.arguments["b"] is shared, bound.arguments["args"], bound.arguments["kwargs"])
result = target(*bound.args, **bound.kwargs)
print(result[1] is shared, result[2], result[3], result[4], result[5], calls)
bound.arguments["a"] = 9
bound.arguments["kwargs"]["extra"] = shared
print(bound.args[0], bound.kwargs["extra"] is shared)
print(target(*bound.args, **bound.kwargs)[5]["extra"] is shared)
special = sig.bind(4, c=5, a=6)
print(special.arguments["a"], special.arguments["kwargs"]["a"])
partial = sig.bind_partial(c=8)
partial.apply_defaults()
print("a" in partial.arguments, partial.arguments["b"] is shared, partial.arguments["c"])
print(partial.arguments["kwargs"] is not bound.arguments["kwargs"])
replaced = sig.bind(1, c=2)
arguments = {"a": 7, "c": 9}
replaced.arguments = arguments
print(replaced.arguments is arguments, replaced.args, replaced.kwargs)

before = len(calls)
for operation in [lambda: sig.bind(), lambda: sig.bind(1),
                  lambda: sig.bind(1, 2, b=4, c=3)]:
    try:
        operation()
    except TypeError:
        print("bind-TypeError")
print(before == len(calls))
positional = Signature([Parameter("only", Parameter.POSITIONAL_ONLY)])
try:
    positional.bind(only=1)
except TypeError:
    print("positional-only-TypeError")
print(list(positional.bind_partial().arguments))

# 严格绑定的声明缺参与未知keyword优先级，保留精确TypeError文本。
required = Signature([Parameter("first", 1), Parameter("second", 1)])
for values in [{"unknown": 1}, {"first": 1, "unknown": 2}]:
    try:
        required.bind(**values)
    except TypeError as error:
        print(str(error))
positional_required = Signature([Parameter("first", 0), Parameter("second", 0)])
try:
    positional_required.bind(second=1)
except TypeError as error:
    print(str(error))
