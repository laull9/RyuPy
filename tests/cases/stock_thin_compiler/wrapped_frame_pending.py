# ryupy: pending
# 完整frame回归：普通partial包装仍须看到业务locals，直接canonical守卫仅覆盖直接对象。
import builtins
import functools

def wrapped_locals(x):
    business = 17
    return x()

def wrapped_eval(x):
    hidden = 23
    return x("hidden")

local_reader = functools.partial(getattr(builtins, "locals"))
evaluator = functools.partial(getattr(builtins, "eval"))
snapshot = wrapped_locals(local_reader)
print("locals", sorted(snapshot), snapshot.get("business"), snapshot.get("x") is local_reader)
try:
    result = wrapped_eval(evaluator)
except Exception as error:
    print("eval-error", type(error).__name__, str(error))
    result = None
else:
    print("eval", result)
assert sorted(snapshot) == ["business", "x"]
assert snapshot["business"] == 17
assert snapshot["x"] is local_reader
assert result == 23
