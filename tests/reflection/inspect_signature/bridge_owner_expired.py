# ryupy: pending
# 私有owner提前失效后的独立负门：只允许明确NI，不能以旧借用指针继续生成对象。
import gc
import inspect
import sys
import bridge_callbacks as compiled

assert type(inspect.signature(compiled.direct)) is inspect.Signature
if "--product-boundary" in sys.argv:
    sys.modules.pop("_ryupy_inspect_profile")
    gc.collect()
    try:
        compiled.snapshot(compiled.direct)
    except NotImplementedError:
        print("profile owner expired: explicit NI")
    else:
        raise AssertionError("expired owner must invalidate borrowed aliases")
else:
    assert type(compiled.snapshot(compiled.direct)) is inspect.Signature
    print("CPython signature precondition")
