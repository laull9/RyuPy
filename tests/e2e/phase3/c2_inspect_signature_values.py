# ryupy: pending
# ADR0219：真实kind/empty、不可变参数与签名、只读有序映射及构造负例；本轮不运行。
import inspect

Parameter = inspect.Parameter
Signature = inspect.Signature
empty = Parameter.empty
print(empty is Signature.empty, empty is inspect._empty, empty is not None)
for kind in [Parameter.POSITIONAL_ONLY, Parameter.POSITIONAL_OR_KEYWORD,
             Parameter.VAR_POSITIONAL, Parameter.KEYWORD_ONLY, Parameter.VAR_KEYWORD]:
    print(kind.name, kind.value, kind.description, isinstance(kind, int), int(kind))
print(Parameter.KEYWORD_ONLY == 3, {Parameter.KEYWORD_ONLY: "kind"}[3])

shared = []
first = Parameter("value", Parameter.POSITIONAL_OR_KEYWORD, default=shared, annotation=int)
changed = first.replace(name="other", annotation=str)
print(first.default is shared, changed.default is shared, first.name, changed.name)
print(first.kind is Parameter.POSITIONAL_OR_KEYWORD, first.annotation is int)
signature = Signature([first], return_annotation=str)
print(list(signature.parameters), signature.parameters["value"] is first)
replacement = signature.replace(return_annotation=int)
print(signature.return_annotation is str, replacement.return_annotation is int)
print(replacement.parameters["value"] is first)
shared.append(3)
print(signature.parameters["value"].default)
plain = Parameter("plain", Parameter.POSITIONAL_ONLY)
print(plain == plain.replace(), hash(plain) == hash(plain.replace()))
print(Signature([plain]) == Signature([plain.replace()]))
print(str(Signature([plain, Parameter("option", Parameter.KEYWORD_ONLY, default=None)])))
print(Signature().return_annotation is empty, plain.default is empty)
left = Signature([Parameter("a", Parameter.KEYWORD_ONLY), Parameter("b", Parameter.KEYWORD_ONLY)])
right = Signature([Parameter("b", Parameter.KEYWORD_ONLY), Parameter("a", Parameter.KEYWORD_ONLY)])
print(list(left.parameters), list(right.parameters), left == right, hash(left) == hash(right))


def reflected(value=3):
    return value


print(list(Signature.from_callable(reflected).parameters))

try:
    signature.parameters["new"] = plain
except TypeError:
    print("parameters-readonly")
try:
    first.name = "mutated"
except AttributeError:
    print("parameter-readonly")
try:
    signature.return_annotation = None
except AttributeError:
    print("signature-readonly")
try:
    hash(first)
except TypeError:
    print("mutable-default-unhashable")
try:
    Parameter("bad-name", Parameter.POSITIONAL_ONLY)
except ValueError:
    print("invalid-name")
try:
    Parameter("value", 99)
except ValueError:
    print("invalid-kind")
try:
    Parameter("args", Parameter.VAR_POSITIONAL, default=1)
except ValueError:
    print("variadic-default")
try:
    Signature([plain, plain])
except ValueError:
    print("duplicate-name")
try:
    Signature([Parameter("first", 1, default=1), Parameter("second", 1)])
except ValueError:
    print("required-after-default")
try:
    Signature([Parameter("first", 3), Parameter("second", 1)])
except ValueError:
    print("invalid-kind-order")
