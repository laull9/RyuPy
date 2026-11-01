# M3.6：部分应用、惰性归约与显式包装元数据。
from functools import partial, reduce, wraps

def combine(a, b=0, c=0):
    return a, b, c
p = partial(combine, 1, b=2)
print(p(c=3), p(b=4), p.args, p.keywords)
p.keywords["b"] = 5
print(p(), p.func is combine)
print(reduce(lambda a, b: a + b, [1, 2, 3]), reduce(lambda a, b: a + b, [], 7))
try:
    reduce(lambda a, b: a + b, [])
except TypeError:
    print("empty")

def original(x: int) -> int:
    """原始文档。"""
    return x
original.extra = "kept"
@wraps(original)
def wrapper(x):
    return original(x)
print(wrapper.__name__, wrapper.__doc__, wrapper.extra)
print(wrapper.__wrapped__ is original, wrapper.__annotations__ is original.__annotations__)
