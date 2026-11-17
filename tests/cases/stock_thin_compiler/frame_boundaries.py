# ryupy: pending
# 负合同：CP oracle验证真实函数scope；候选按实际内建身份拒绝未准入frame操作，同名用户值正常。
import builtins

events = []
marker = object()

def frame_call(x):
    return x()

def evaluate(x):
    hidden = 17
    return x("hidden")

def execute(x):
    x("events.append('exec')")
    return None

def replacement(x):
    return marker

for label in ["locals", "vars"]:
    callable_value = getattr(builtins, label)
    try:
        result = frame_call(callable_value)
    except NotImplementedError:
        print(label, "blocked")
    else:
        assert set(result) == {"x"}
        assert result["x"] is callable_value
        print(label, "real-scope")
try:
    result = evaluate(getattr(builtins, "eval"))
except NotImplementedError:
    print("eval", "blocked")
else:
    assert result == 17
    print("eval", "real-scope")
try:
    execute(getattr(builtins, "exec"))
except NotImplementedError:
    print("exec", "blocked")
else:
    assert events == ["exec"]
    print("exec", "real-scope")
replacement.__name__ = "locals"
UserCallable = type("locals", (), {"__call__": replacement})
print("ordinary", frame_call(UserCallable()) is marker)
