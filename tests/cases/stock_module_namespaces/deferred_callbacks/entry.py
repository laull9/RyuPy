"""非 GUI 回调合同：真实 CPython 通过位置、关键字及异常路径进入 CIL。"""
import json
import operator
from consumers import capture, dispatch, fail_before_read, ignore, readers, variadic

assert json.loads('{"discarded": 17}', object_hook=ignore) is None
observed = json.loads('{"observed": 23}', object_hook=capture)
assert readers[-1]() is observed
assert dispatch(True, observed) is observed
assert dispatch(False, observed) is None
assert operator.call(dispatch, enabled=True, payload=observed) is observed
assert operator.call(variadic, observed, observed) is observed

try:
    json.loads('{"unused": 31}', object_hook=fail_before_read)
except ValueError:
    pass
else:
    raise AssertionError("异常未越过回调边界")

observed['retained'] = 29
assert readers[-1]()["retained"] == 29
print("按读物化、别名、闭包与错误路径通过")
