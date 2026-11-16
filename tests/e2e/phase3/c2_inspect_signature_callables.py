# ryupy: pending
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


def target(a, b, c=3):
    return a + b + c


bound = partial(target, 1)
print(list(inspect.signature(bound).parameters), bound(2))
keywords = partial(target, b=2)
first = inspect.signature(keywords)
print([(p.name, p.kind.name) for p in first.parameters.values()])
keywords.keywords["b"] = 8
print(first.parameters["b"].default, inspect.signature(keywords).parameters["b"].default)
print(str(inspect.signature(len)))
print(inspect.signature(float).parameters["x"].kind is inspect.Parameter.POSITIONAL_ONLY)
try:
    inspect.signature(1)
except TypeError:
    print("non-callable")
