# 此用例断言已登记的无来源AOT合同，不冒充CPython文件来源元数据差分。
import plain
import state
import cycle_a
import located_pkg
from importlib import resources, util
assert __spec__ is None
assert __loader__ is None
assert __cached__ is None
assert plain.value == 7
marker = [1]
plain.__spec__ = marker
assert plain.check() is marker
plain.delete()
assert not hasattr(plain, "__spec__")
try:
    plain.check()
except NameError:
    pass
else:
    raise AssertionError("deleted slot fallback")
plain.__loader__ = marker
plain.__cached__ = marker
plain.__path__ = marker
values = plain.read_rest()
assert values[0] is marker
assert values[1] is marker
assert values[2] is marker
plain.delete_rest()
assert not hasattr(plain, "__loader__")
assert not hasattr(plain, "__cached__")
assert not hasattr(plain, "__path__")
try:
    plain.read_rest()
except NameError:
    pass
else:
    raise AssertionError("deleted metadata fallback")
try:
    import pkg
except ValueError:
    pass
else:
    raise AssertionError("first import must fail")
import pkg
import pkg.child
assert state.attempts == 2
assert pkg.child.value == 42
assert pkg.__path__ == []
assert pkg.__spec__ is None
try:
    resources.files(pkg)
except NotImplementedError:
    pass
else:
    raise AssertionError("invented resource location")
try:
    util.find_spec("pkg.missing")
except NotImplementedError:
    pass
else:
    raise AssertionError("invented finder capability")
print("metadata ok")
