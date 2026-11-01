# NotImplemented 不是真值结果；必须继续反射侧或抛出类型错误。
events = []
class Left:
    def __lt__(self, other):
        events.append('left')
        return NotImplemented
class Right:
    def __gt__(self, other):
        events.append('right')
        return True
print(Left() < Right(), events)
class Neither:
    def __gt__(self, other):
        return NotImplemented
try:
    Left() < Neither()
except TypeError:
    print('unsupported comparison')
print(NotImplemented is NotImplemented, repr(NotImplemented), str(NotImplemented))
print(type(NotImplemented).__name__, type(NotImplemented)() is NotImplemented)
try:
    NotImplemented()
except TypeError:
    print('not callable')
import warnings
with warnings.catch_warnings(record=True) as recorded:
    warnings.simplefilter('always')
    print(bool(NotImplemented), len(recorded), recorded[0].category is DeprecationWarning)
with warnings.catch_warnings():
    warnings.simplefilter('error')
    try:
        bool(NotImplemented)
    except DeprecationWarning:
        print('deprecated bool')
