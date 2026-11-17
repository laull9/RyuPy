# ryupy: pending
# 失败事务的正面合同：外逃 F 保留旧 D，重复失败创建独立 D。
import gc
import importlib
import sys
import weakref
import control

for attempt in range(2):
    try:
        importlib.import_module("subject")
    except RuntimeError as error:
        assert str(error) == "stock 导入失败"
    else:
        raise AssertionError("应当导入失败")
    assert "subject" not in sys.modules

left, right = control.escaped
assert left is not right
assert left.__globals__ is not right.__globals__
left.__globals__["offset"] = 50
assert left(2) == 52 and right(2) == 42
watches = (weakref.ref(left), weakref.ref(right))
control.escaped.clear()
del left, right
gc.collect()
assert all(watch() is None for watch in watches)
print("stock thin function failed import escapes")
