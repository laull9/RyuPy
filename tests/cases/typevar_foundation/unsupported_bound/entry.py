# ryupy: pending
# 合法用户 bound 的相等副作用尚未支持；独立保留，不能当成非法类型。
import typing as t
events = []
class Bound:
    def __eq__(self, other):
        events.append(getattr(other, '__name__', '?'))
        return False
bound = Bound()
value = t.TypeVar('V', bound=bound)
assert value.__bound__ is bound
print('bound-events', events)
