# 普通Legacy包；与b同basename而来源不同。
import warnings
from typing import TypedDict

def emit(level, label, category=DeprecationWarning):
    warnings.warn(label, category, stacklevel=level)
    return TypedDict('Record', {'value': int}).__module__

def decorate(function):
    def wrapped():
        warnings.warn('decorator', DeprecationWarning)
        return (TypedDict('Wrapper', {'value': int}).__module__, function())
    return wrapped

def make():
    def closure():
        warnings.warn('closure', DeprecationWarning)
        return TypedDict('Closure', {'value': int}).__module__
    return closure

def stream():
    warnings.warn('generator-first', DeprecationWarning)
    yield TypedDict('First', {'value': int}).__module__
    warnings.warn('generator-second', DeprecationWarning)
    yield TypedDict('Second', {'value': int}).__module__

def native():
    return bool(NotImplemented)

# 仅静态分类/ABI候选，不把无来源消费的数值返回冒称Static来源已经通过。
def static_candidate(value: int) -> int:
    return value + 1
