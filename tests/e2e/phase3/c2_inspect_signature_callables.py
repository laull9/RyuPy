# ADR0219：真实接收者移除一次、简单类/callable、partial实时关键词及有限native表。
import inspect
from functools import partial


class Example:
    def __init__(self, value=3):
        self.value = value

    def method(self, value, *, option=4):
        return value

    @staticmethod
    def static(value=5):
        return value

    @classmethod
    def class_method(cls, value=6):
        return value

    def __call__(self, value=7):
        return value


instance = Example()
print(list(inspect.signature(Example).parameters))
print(list(inspect.signature(Example.method).parameters))
print(list(inspect.signature(instance.method).parameters))
print(list(inspect.signature(Example.static).parameters))
print(list(inspect.signature(Example.class_method).parameters))
print(inspect.signature(instance).parameters["value"].default)
custom = inspect.Signature([
    inspect.Parameter("receiver", inspect.Parameter.POSITIONAL_ONLY),
    inspect.Parameter("chosen", inspect.Parameter.KEYWORD_ONLY),
])
Example.method.__signature__ = custom
print(inspect.signature(Example.method) is custom)
print(list(inspect.signature(instance.method).parameters))


def target(a, b, c=3):
    return a + b + c


bound = partial(target, 1)
print(list(inspect.signature(bound).parameters), bound(2))
keywords = partial(target, b=2)
first = inspect.signature(keywords)
print([(p.name, p.kind.name) for p in first.parameters.values()])
keywords.keywords["b"] = 8
print(first.parameters["b"].default, inspect.signature(keywords).parameters["b"].default)
invalid = partial(target, unknown=1)
try:
    inspect.signature(invalid)
except ValueError:
    print("invalid-partial")
print(str(inspect.signature(len)))
print(inspect.signature(float).parameters["x"].kind is inspect.Parameter.POSITIONAL_ONLY)
try:
    inspect.signature(1)
except TypeError:
    print("non-callable")

class ParentConstructor:
    def __new__(cls, parent=1):
        return object.__new__(cls)
class ChildConstructor(ParentConstructor):
    def __init__(self, child=2):
        pass
print(list(inspect.signature(ChildConstructor).parameters))

class StaticCallable:
    @staticmethod
    def __call__(value, option=8):
        return value
class ClassCallable:
    @classmethod
    def __call__(cls, value, option=9):
        return value
for value in [instance, StaticCallable(), ClassCallable()]:
    print(list(inspect.signature(value).parameters))
instance.__call__ = lambda shadow, extra: shadow
print(list(inspect.signature(instance).parameters))

bindings = []
class CallDescriptor:
    def __get__(self, receiver, owner):
        bindings.append((receiver is not None, owner is DescribedCallable))
        return lambda actual, option=11: actual
class DescribedCallable:
    __call__ = CallDescriptor()
print(list(inspect.signature(DescribedCallable()).parameters), bindings)
