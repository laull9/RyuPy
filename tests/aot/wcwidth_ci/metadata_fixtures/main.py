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
try:
    plain.check()
except NotImplementedError as error:
    assert str(error) == "builtin fallback for unbound module name '__spec__' is not supported"
else:
    raise AssertionError("spec builtin boundary")
plain.__loader__ = marker
assert plain.read_loader() is marker
plain.delete_loader()
try:
    plain.read_loader()
except NotImplementedError as error:
    assert str(error) == "builtin fallback for unbound module name '__loader__' is not supported"
else:
    raise AssertionError("loader builtin boundary")
plain.__cached__ = marker
assert plain.read_cached() is marker
plain.delete_cached()
try:
    plain.read_cached()
except NameError:
    pass
else:
    raise AssertionError("cached name must be missing")
plain.__path__ = marker
assert plain.read_path() is marker
plain.delete_path()
try:
    plain.read_path()
except NameError:
    pass
else:
    raise AssertionError("path name must be missing")
# 外部属性读取不查builtins；四个槽都必须缺失，不回退初值。
for name in ["__spec__", "__loader__", "__cached__", "__path__"]:
    try:
        getattr(plain, name)
    except AttributeError:
        pass
    else:
        raise AssertionError("deleted attribute fallback")
plain.__spec__ = marker
plain.__loader__ = marker
plain.__cached__ = marker
plain.__path__ = marker
assert plain.check() is marker
assert plain.read_loader() is marker
assert plain.read_cached() is marker
assert plain.read_path() is marker
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
