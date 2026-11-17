# ryupy: pending
"""编译入口与 CPython 回调共用真实活字典，覆盖身份、别名、写删和错误类别。"""
import sys
from bridge_fixture.live_module_dict import mutate

counter = 1
shared = []


def read():
    return counter


def write(value):
    global counter
    counter = value


def remove():
    global counter
    del counter


namespace = mutate(sys.modules["__main__"], read, write, remove, shared)
assert namespace is globals()
assert read.__globals__ is namespace
assert namespace["shared"] is shared
assert counter == 45
counter = 46
assert namespace["counter"] == 46
print("live module dict", counter)
