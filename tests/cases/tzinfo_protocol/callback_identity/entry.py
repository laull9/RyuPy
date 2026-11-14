# datetime 回调保持对象身份，time 回调传 None。
from datetime import tzinfo, timedelta, datetime, time
class Zone(tzinfo):
    def __init__(self):
        self.expected = None
    def utcoffset(self, dt):
        print('offset', dt is self.expected, dt is None)
        return timedelta(hours=1, seconds=2, microseconds=3)
    def dst(self, dt):
        print('dst', dt is self.expected, dt is None)
        return timedelta(0)
    def tzname(self, dt):
        print('name', dt is self.expected, dt is None)
        return 'zone'
z = Zone()
d = datetime(2024, 1, 2, 3, 4, tzinfo=z)
z.expected = d
print(d.utcoffset(), d.dst(), d.tzname(), d.isoformat())
t = time(3, 4, tzinfo=z)
z.expected = None
print(t.utcoffset(), t.dst(), t.tzname(), t.isoformat())

# tzname 接受 str 子类并保留返回对象身份，不强制复制为基类字符串。
class Name(str):
    pass
name = Name('subclass-name')
class Named(Zone):
    def tzname(self, dt):
        return name
named = Named()
for obj in (datetime(2024, 1, 2, tzinfo=named), time(3, 4, tzinfo=named)):
    result = obj.tzname()
    print('name-identity', result is name, type(result) is Name, result)
