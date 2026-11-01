# 大字典动态标量键不执行用户hash/eq；保求值顺序、跨块重复键和暂停前绑定。
events = []
def key(i):
    events.append(('key', i))
    return i % 17
def value(i):
    events.append(('value', i))
    return i

def many():
    return {
        key(0): value(0),
        key(1): value(1),
        key(2): value(2),
        key(3): value(3),
        key(4): value(4),
        key(5): value(5),
        key(6): value(6),
        key(7): value(7),
        key(8): value(8),
        key(9): value(9),
        key(10): value(10),
        key(11): value(11),
        key(12): value(12),
        key(13): value(13),
        key(14): value(14),
        key(15): value(15),
        key(16): value(16),
        key(17): value(17),
        key(18): value(18),
        key(19): value(19),
        key(20): value(20),
        key(21): value(21),
        key(22): value(22),
        key(23): value(23),
        key(24): value(24),
        key(25): value(25),
        key(26): value(26),
        key(27): value(27),
        key(28): value(28),
        key(29): value(29),
        key(30): value(30),
        key(31): value(31),
        key(32): value(32),
        key(33): value(33),
    }
data = many()
print(list(data), list(data.values()))
print(events == [(part, i) for i in range(34) for part in ['key', 'value']])

class Code:
    @property
    def number(self):
        events.append('property')
        return 99
code = Code()
def mixed(first):
    return {first: 'first', 1: 'one', True: 'bool', 1.0: 'float',
            2: 2,
            3: 3,
            4: 4,
            5: 5,
            6: 6,
            7: 7,
            8: 8,
            9: 9,
            10: 10,
            11: 11,
            12: 12,
            13: 13,
            14: 14,
            15: 15,
            16: 16,
            code.number: 'last'}
for first in [0, None, 'text', int('18446744073709551617')]:
    data = mixed(first)
    print(len(data), list(data)[:3], data[1], data[99])
print(events[-4:])

first_key = 'old'
first_value = []
saved = first_value
def paused():
    return {first_key: first_value,
            1: 1,
            2: 2,
            3: 3,
            4: 4,
            5: 5,
            6: 6,
            7: 7,
            8: 8,
            9: 9,
            10: 10,
            11: 11,
            12: 12,
            13: 13,
            14: 14,
            15: 15,
            16: 16,
            17: 17,
            'tail': (yield 'pause')}
it = paused()
print(next(it))
first_key = 'new'
first_value = ['new']
try:
    it.send(100)
except StopIteration as error:
    data = error.value
    print(data['old'] is saved, 'new' in data, data['tail'])

def fail():
    events.append('failure')
    raise ValueError('stop')
events.clear()
try:
    data = {
        key(0): value(0),
        key(1): value(1),
        key(2): value(2),
        key(3): value(3),
        key(4): value(4),
        key(5): value(5),
        key(6): value(6),
        key(7): value(7),
        key(8): value(8),
        key(9): value(9),
        key(10): value(10),
        key(11): value(11),
        key(12): value(12),
        key(13): value(13),
        key(14): value(14),
        key(15): value(15),
        key(16): value(16),
        key(17): value(17),
        18: fail(), 19: value(19)}
except ValueError:
    print(events[-3:], len(events))
