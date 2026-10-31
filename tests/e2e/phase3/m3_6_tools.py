# ryupy: pending
# M3.6：常用容器、惰性工具、函数包装和生成器上下文。
from collections import Counter, defaultdict, deque
from contextlib import contextmanager
from functools import lru_cache, partial, reduce, wraps
from itertools import chain, islice, repeat

counts = Counter("abaca")
groups = defaultdict(list)
groups["a"].append(1)
queue = deque([1, 2])
queue.appendleft(0)
print(counts["a"], dict(groups), queue.popleft())
print(list(islice(chain([1], repeat(2)), 4)))
print(partial(pow, 2)(5), reduce(lambda a, b: a + b, [1, 2, 3]))


@lru_cache(maxsize=2)
def square(value):
    print("compute", value)
    return value * value


print(square(2), square(2), square(3))
print(square.cache_info().hits, square.cache_info().currsize)


def original(value):
    """保留到包装函数的文档。"""
    return value


@wraps(original)
def wrapper(value):
    return original(value)


print(wrapper.__name__, wrapper.__doc__ == original.__doc__)


@contextmanager
def resource():
    print("acquire")
    try:
        yield 7
    finally:
        print("release")


with resource() as value:
    print(value)
