# ryupy: pending
"""CP1b：真实 Python 对象的身份、别名、调用、迭代及异常合同。"""
import json
from collections import deque
from contextlib import nullcontext

value = json.loads('{"items": [1, 2], "enabled": true}')
alias = value["items"]
assert alias is value["items"]
alias.append(3)
assert value["items"][2] == 3
value["enabled"] = False
assert not value["enabled"]
assert len(alias) == 3
assert sum(alias) == 6
assert json.dumps(value, sort_keys=True, ensure_ascii=False) == '{"enabled": false, "items": [1, 2, 3]}'
queue = deque(alias)
queue.appendleft(0)
assert list(queue) == [0, 1, 2, 3]
with nullcontext(value) as entered:
    assert entered is value
try:
    json.loads('{')
except json.JSONDecodeError as error:
    assert isinstance(error, ValueError)
    assert error.doc == '{'
    assert error.pos == 1
else:
    raise AssertionError('JSONDecodeError 未跨桥传播')
print('CP1b 对象协议通过')
