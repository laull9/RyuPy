# 根协议改写合法，但G1不模拟；消费时应明确NI。
import typing as t
T = t.TypeVar('T')
events = []
def init(self):
    events.append(type(self) is t.Generic)
t.Generic.__init__ = init
assert type(t.Generic()) is t.Generic
assert type(t.Generic[T]()) is t.Generic
assert events == [True, True]
print('root-hooks', events)
