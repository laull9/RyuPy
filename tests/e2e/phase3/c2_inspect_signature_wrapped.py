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

# 无wrapped的终端不调用stop；类对象即使有该属性也保持原对象。
seen.clear()
print(inspect.unwrap(target, stop=stop) is target, seen)
class WrappedClass:
    __wrapped__ = target
    def __init__(self, chosen=1):
        pass
print(inspect.unwrap(WrappedClass) is WrappedClass)
print(list(inspect.signature(WrappedClass).parameters))

reads = []
class DynamicWrapper:
    @property
    def __wrapped__(self):
        reads.append("wrapped")
        return target
    def __call__(self, *args, **kwargs):
        return target(*args, **kwargs)
wrapped_instance = DynamicWrapper()
print(inspect.unwrap(wrapped_instance) is target, reads)
reads.clear()
print(list(inspect.signature(wrapped_instance).parameters), reads)

# 显式Signature已经是用户快照；eval_str=True也不执行其中的字符串注解。
custom_strings = inspect.Signature([inspect.Parameter("value", 1, annotation="opaque")])
wrapper.__signature__ = custom_strings
print(inspect.signature(wrapper, eval_str=True) is custom_strings)
