# ryupy: pending
# 真实退出阶段首次/再次签名检查；用户atexit顺序不变，类型/kind/empty仍属同一解释器。
import atexit
import inspect
import bridge_callbacks as compiled

before = inspect.signature(compiled.direct)

def closing():
    after = inspect.signature(compiled.direct)
    first = inspect.signature(compiled.late)
    assert type(after) is inspect.Signature
    assert type(first.parameters["value"]) is inspect.Parameter
    assert after.parameters["value"].default is before.parameters["value"].default
    assert first.parameters["value"].kind is inspect.Parameter.POSITIONAL_OR_KEYWORD
    assert first.parameters["value"].default is inspect.Parameter.empty is inspect.Signature.empty is inspect._empty
    assert compiled.snapshot(compiled.late).parameters["value"].annotation is inspect.Parameter.empty
    print("atexit: first repeated and exact inspect identities")

atexit.register(closing)
print("atexit: registered")
