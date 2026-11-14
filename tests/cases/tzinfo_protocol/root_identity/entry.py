# 规范根身份与用户存储；不是以模块名字冒充类型。
import datetime as a
import datetime as b
from datetime import tzinfo, timezone, timedelta, datetime
print(a.tzinfo is b.tzinfo, tzinfo is a.tzinfo, type(tzinfo) is type)
print(issubclass(timezone, tzinfo), isinstance(timezone.utc, tzinfo))
print(timezone(timedelta(0)) is timezone.utc)
root = tzinfo()
print(type(root) is tzinfo, isinstance(root, tzinfo))
for method in ('utcoffset', 'dst', 'tzname'):
    try:
        getattr(root, method)(None)
    except Exception as e:
        print(method, type(e).__name__, str(e))
class Zone(tzinfo):
    def __init__(self, minutes):
        self.minutes = minutes
    def utcoffset(self, dt):
        return timedelta(minutes=self.minutes)
    def dst(self, dt):
        return timedelta(0)
    def tzname(self, dt):
        return 'user-zone'
z = Zone(90)
z.extra = 'field'
d = datetime(2024, 1, 2, tzinfo=z)
print(type(z) is Zone, isinstance(z, tzinfo), isinstance(z, timezone), d.tzinfo is z)
print(z.minutes, z.extra, d.isoformat(), d.tzname())

# 根不可修改任意类属性；普通用户子类仍可修改与删除类属性。
try:
    tzinfo.user_flag = 'forbidden'
except Exception as e:
    print('root-write', type(e).__name__, str(e))
Zone.user_flag = 'first'
print('child-write', Zone.user_flag, z.user_flag)
Zone.user_flag = 'second'
print('child-rewrite', z.user_flag)
del Zone.user_flag
print('child-delete', hasattr(Zone, 'user_flag'))
