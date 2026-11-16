# ryupy: pending
# ADR0219：显式签名/None与wrapped优先级、身份循环、用户属性异常均须当次差分。
import inspect


def target(value, option=2):
    return value


def wrapper(*args, **kwargs):
    return target(*args, **kwargs)


wrapper.__wrapped__ = target
print(inspect.unwrap(wrapper) is target)
seen = []


def stop(value):
    seen.append(value is wrapper)
    return value is wrapper


print(inspect.unwrap(wrapper, stop=stop) is wrapper, seen)
print(list(inspect.signature(wrapper).parameters))
print(list(inspect.signature(wrapper, follow_wrapped=False).parameters))
custom = inspect.Signature([inspect.Parameter("custom", inspect.Parameter.KEYWORD_ONLY)])
wrapper.__signature__ = custom
print(inspect.signature(wrapper) is custom)
print(inspect.signature(wrapper, follow_wrapped=False) is custom)
print(inspect.unwrap(wrapper) is target)
wrapper.__signature__ = None
print(list(inspect.signature(wrapper).parameters))
del wrapper.__signature__
target.__defaults__ = (7,)
print(inspect.signature(wrapper).parameters["option"].default)
wrapper.__signature__ = 1
try:
    inspect.signature(wrapper)
except TypeError:
    print("invalid-explicit-signature")
del wrapper.__signature__
wrapper.__wrapped__ = wrapper
try:
    inspect.unwrap(wrapper)
except ValueError:
    print("unwrap-cycle")
try:
    inspect.signature(wrapper)
except ValueError:
    print("signature-cycle")
wrapper.__signature__ = custom
print(inspect.signature(wrapper) is custom)


class Broken:
    @property
    def __signature__(self):
        raise RuntimeError("signature-property")

    def __call__(self, value):
        return value


try:
    inspect.signature(Broken())
except RuntimeError as error:
    print(str(error))
