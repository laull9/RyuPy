# ryupy: pending
# 正面 Python 合同；CLR-only/回流与双 GC 另由产品宿主门控制，不能由此脚本推断。
import importlib
import sys
import types
import weakref
import gc
import subject

old = subject.read
old_globals = subject.__dict__
assert isinstance(old, types.FunctionType)
assert old.__globals__ is old_globals
assert old(2) == old(x=2) == 12
token = []
assert subject.echo(token) is token
error = ValueError("保留异常身份")
try:
    subject.fail(error)
except ValueError as caught:
    assert caught is error
else:
    raise AssertionError("应当抛出原异常")

old.__name__ = "公开改名"
old.__qualname__ = "对外限定名"
old.__module__ = "公开模块名"
old.__annotations__ = {"x": token}
old.marker = token
assert old.__annotations__["x"] is old.marker is token
assert old.__name__ == "公开改名" and old.__qualname__ == "对外限定名"
assert old.__module__ == "公开模块名"
old_globals["offset"] = 20
assert old(2) == 22

del sys.modules["subject"]
fresh = importlib.import_module("subject")
assert fresh is not subject
assert fresh.__dict__ is not old_globals
assert fresh.read.__globals__ is fresh.__dict__
fresh.offset = 100
assert old(2) == 22 and fresh.read(2) == 102
del old_globals["offset"]
try:
    old(2)
except NameError:
    pass
else:
    raise AssertionError("删除必须立即可见")

watch = weakref.ref(old)
del old, old_globals, subject, fresh
del sys.modules["subject"]
gc.collect()
# 保留的异常 traceback 仍持有 fail 的真实 frame，进而保活旧 D 和 read。
assert watch() is not None
del error
gc.collect()
assert watch() is None
print("stock thin function live globals and reload")
