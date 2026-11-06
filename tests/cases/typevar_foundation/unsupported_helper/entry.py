# ryupy: pending
# 合法动态 helper 覆写独立保留，不允许静默忽略转换或改 oracle。
import typing as t
events = []
def convert(value, message):
    events.append(value is int)
    return str
t._type_check = convert
assert t.TypeVar('V', bound=int).__bound__ is str
assert t.TypeVar('C', int, str).__constraints__ == (int, str)
assert events == [True]
print('helper-events', events)
