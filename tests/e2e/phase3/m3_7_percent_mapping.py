# 百分号映射格式逐项取键、转换；无转换格式不能探测调用 __getitem__。
events = []


class Number:
    def __int__(self):
        events.append('int')
        return 12


class Mapping:
    def __getitem__(self, key):
        events.append(key)
        if key == 'missing':
            raise KeyError(key)
        if key == 'number':
            return Number()
        if key == 'pair':
            return (1, 2)
        return 4

    def __str__(self):
        events.append('str mapping')
        return 'mapping'


mapping = Mapping()
print('Working' % mapping, '%%' % mapping)
print(events)
print('%(number)d/%(index)d/%(index)d/%%/%(pair)s' % mapping)
print(events)
events.clear()
print('%s/%(index)d' % mapping)
print(events)
events.clear()
print('%((index))d' % mapping)
print(events)

for format in ['%(index)s/%s', '%(index)q', '%(index)', '%(missing)q']:
    events.clear()
    try:
        print(format % mapping)
    except (TypeError, ValueError, KeyError) as error:
        print(type(error).__name__, events)

print('%(name)s:%(count)d' % {'name': 'done', 'count': 3})


class DisabledItem:
    __getitem__ = None


print('plain' % DisabledItem())
for value in [1, 'text', (1,), None]:
    try:
        print('plain' % value)
    except TypeError:
        print('非映射有未消费参数')
for value in [(), [], b'x', bytearray(b'x')]:
    print('plain' % value)
try:
    print('%(index)d' % (mapping,))
except TypeError:
    print('tuple 不作为映射')

# 前一键结果转换改变映射后，下一键必须重新取值。
state = {'later': 1}


class MutatingResult:
    def __str__(self):
        events.append('mutate')
        state['later'] = 9
        return 'first'


class ChangingMapping:
    def __getitem__(self, key):
        events.append(key)
        if key == 'first':
            return MutatingResult()
        return state[key]


events.clear()
print('%(first)s/%(later)d/%(later)d' % ChangingMapping())
print(events)
events.clear()
try:
    print('%(unfinished' % mapping)
except ValueError:
    print('不完整键不读取', events)
try:
    print('%(index)d' % DisabledItem())
except TypeError:
    print('无可调用getitem仍不能取键')
from collections import deque
try:
    print('plain' % deque([1]))
except TypeError:
    print('deque不是mapping slot')


def added_getitem(self, key):
    return 9


class AddedSlot:
    def __str__(self):
        AddedSlot.__getitem__ = added_getitem
        return 'added'


added = AddedSlot()
try:
    print('%s/%(index)s' % added)
except TypeError:
    print('资格取入口快照')
print('%s/%(index)s' % added)
