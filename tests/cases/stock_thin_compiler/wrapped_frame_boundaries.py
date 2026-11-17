# ryupy: pending
# exact stdlib partial的frame负合同：业务frame未物化时精确拒绝，显式环境与普通值调用保持可用。
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
try:
    snapshot = wrapped_locals(local_reader)
except NotImplementedError:
    print("partial-locals", "blocked")
else:
    assert sorted(snapshot) == ["business", "x"]
    assert snapshot["business"] == 17 and snapshot["x"] is local_reader
    print("partial-locals", "real-scope")
try:
    result = wrapped_eval(evaluator)
except NotImplementedError:
    print("partial-eval", "blocked")
else:
    assert result == 23
    print("partial-eval", "real-scope")
# 无缺失frame依赖的partial继续执行真实builtin与其公开参数协议。
sample = builtins
assert wrapped_locals(functools.partial(getattr(builtins, "vars"), sample)) is sample.__dict__
assert wrapped_locals(functools.partial(getattr(builtins, "eval"), "value+1", {"value": 41})) == 42
assert type(wrapped_locals(functools.partial(getattr(builtins, "compile"), "1+2", "<partial>", "eval", dont_inherit=True))).__name__ == "code"
assert wrapped_locals(functools.partial(getattr(builtins, "str"), 123)) == "123"
print("ordinary", True)
