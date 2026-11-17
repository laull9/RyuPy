"""无自定义callable的编译入口：真实globals与CP模块dict共用权威存储。"""
import sys
from bridge_fixture.live_module_dict import mutate_entry

counter = 1
shared = []
namespace = globals()
assert namespace is sys.modules["__main__"].__dict__
assert mutate_entry(sys.modules["__main__"], namespace, shared) is namespace
assert counter == 43
counter = 44
assert namespace["counter"] == 44
del counter
assert "counter" not in namespace
namespace["counter"] = 45
assert counter == 45
assert namespace["shared"] is shared
print("live entry dict", counter)
