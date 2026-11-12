# 相同 zone 快路不查偏移；跨 zone 与 fold 哈希不是同一条规则。
from datetime import tzinfo, timedelta, datetime
class Zone(tzinfo):
    def __init__(self, hours):
        self.hours = hours
        self.calls = []
    def utcoffset(self, dt):
        self.calls.append(dt.fold)
        return timedelta(hours=self.hours + dt.fold)
    def dst(self, dt):
        return timedelta(0)
z = Zone(1)
a = datetime(2024, 1, 2, 3, tzinfo=z, fold=0)
b = datetime(2024, 1, 2, 3, tzinfo=z, fold=1)
print('same', a == b, a < b, b - a, z.calls)
print('hash', hash(a) == hash(b), z.calls)
z.calls = []
print('cached', hash(a) == hash(b), z.calls)
w = Zone(1)
c = datetime(2024, 1, 2, 3, tzinfo=w)
print('distinct', a == c, a <= c, a - c)
print('distinct-calls', z.calls, w.calls)
class Fixed(tzinfo):
    def __init__(self, hours):
        self.hours = hours
    def utcoffset(self, dt):
        return timedelta(hours=self.hours)
    def dst(self, dt):
        return timedelta(0)
x = datetime(2024, 1, 2, 3, tzinfo=Fixed(1))
y = datetime(2024, 1, 2, 4, tzinfo=Fixed(2))
print('fixed', x == y, x - y, hash(x) == hash(y), len({x, y}))
