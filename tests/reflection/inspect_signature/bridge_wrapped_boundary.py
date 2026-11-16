# ryupy: pending
# CPoracle输出原始wrapper形状；产品分支必须明确NI/ValueError，不得静默返回已展开签名。
import inspect
import sys
import bridge_callbacks as compiled

assert list(inspect.signature(compiled.wrapper).parameters) == ["value", "option"]
if "--product-boundary" in sys.argv:
    try:
        inspect.signature(compiled.wrapper, follow_wrapped=False)
    except (ValueError, NotImplementedError):
        print("wrapped false boundary")
    else:
        raise AssertionError("opaque CIL wrapper must reject raw CodeType inspection")
else:
    assert list(inspect.signature(compiled.wrapper, follow_wrapped=False).parameters) == ["args", "kwargs"]
    print("CPython raw wrapper supported")
