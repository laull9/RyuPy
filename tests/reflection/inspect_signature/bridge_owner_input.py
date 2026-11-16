# 保留旧owner再顺序重建；旧m_free不得清掉新owner别名，解释器关闭负责所有owner释放。
import gc
import inspect
import sys
import bridge_callbacks as compiled

first = inspect.signature(compiled.direct)
if "--require-cil" in sys.argv:
    old_owner = sys.modules.pop("_ryupy_inspect_profile")
    second = compiled.snapshot(compiled.direct)
    assert sys.modules["_ryupy_inspect_profile"] is not old_owner
    assert second.parameters["value"].default is first.parameters["value"].default
    del old_owner
    gc.collect()
    third = compiled.snapshot(compiled.direct)
else:
    third = inspect.signature(compiled.direct)
assert third.parameters["value"].kind is inspect.Parameter.POSITIONAL_OR_KEYWORD
assert third.parameters["value"].annotation is inspect.Parameter.empty is inspect._empty
print("profile owner: sequential rebuild preserves exact identities")
