
# 有限scalar键域、NaN身份和value比较，不借普通自定义key冒充支持。
class Data(dict):
    pass

d = Data()
for key, value in [(None, 'none'), ('text', 'str'), (True, 'bool'), (1, 'int'),
                   (1.0, 'float'), (2**80, 'big'), (2.5, 'fraction')]:
    d[key] = value
assert len(d) == 5 and d[True] == 'float' and d[2**80] == 'big'
assert list(d) == [None, 'text', True, 2**80, 2.5]
nan = float('nan')
d[nan] = 'nan'
assert d[nan] == 'nan' and nan in d and float('nan') not in d
print('dict-subclass.scalar-keys', len(d), d[True], d[2**80], d[nan])

EVENTS = []
class Value:
    def __init__(self, number):
        self.number = number
    def __eq__(self, other):
        EVENTS.append(self.number)
        return self.number == other.number
v = Value(4)
a = Data(x=v)
b = Data(x=v)
assert a == b and not (a != b) and EVENTS == []
b['x'] = Value(4)
assert a == b and EVENTS == [4]
assert Data(x=nan) == Data(x=nan)
assert Data(x=float('nan')) != Data(x=float('nan'))
assert dict.__eq__(a, 3) is NotImplemented and dict.__ne__(a, 3) is NotImplemented
try:
    hash(a)
except TypeError:
    pass
else:
    raise AssertionError('dict subclass is unhashable')
print('dict-subclass.value-equality', EVENTS, Data(x=nan) == Data(x=nan))
