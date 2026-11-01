# M3.6：LRU 顺序、参数键和包装身份。
from functools import lru_cache

@lru_cache(maxsize=2)
def cached(value):
    print("compute", value)
    return [value]
a = cached(1)
print(cached(1) is a)
cached(2)
cached(1)
cached(3)
cached(2)
print(cached.cache_info().hits, cached.cache_info().misses, cached.cache_info().currsize)
print(cached.cache_parameters())
cached.cache_clear()
print(cached.cache_info().hits, cached.cache_info().currsize)
@lru_cache(None)
def keyword(**kwargs):
    print("kwargs", kwargs)
    return len(kwargs)
keyword(a=1, b=2)
keyword(b=2, a=1)
print(keyword.cache_info().currsize)
@lru_cache(typed=True)
def typed(value):
    return type(value).__name__
print(typed(1), typed(1.0), typed(True))
class Worker:
    @lru_cache(maxsize=2)
    def work(self, x):
        return x + 1
print(Worker().work(3))
