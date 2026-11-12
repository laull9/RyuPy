# astimezone 必须调用目标对象的 fromutc，而不是读取一次偏移替代。
from datetime import tzinfo, timedelta, datetime, timezone
class Target(tzinfo):
    def utcoffset(self, dt):
        return timedelta(hours=2)
    def dst(self, dt):
        return timedelta(0)
    def fromutc(self, dt):
        print('fromutc', dt.tzinfo is self, dt.hour, dt.minute)
        return dt + timedelta(hours=3)
z = Target()
a = datetime(2024, 1, 2, 10, 15, tzinfo=timezone.utc)
b = a.astimezone(z)
print('result', b.hour, b.minute, b.tzinfo is z)
print('same', b.astimezone(z) is b)
class Fixed(tzinfo):
    def utcoffset(self, dt):
        return timedelta(hours=2)
    def dst(self, dt):
        return timedelta(0)
f = Fixed()
c = a.astimezone(f)
print('inherited', c.hour, c.tzinfo is f)
for value in (a, 42):
    try:
        f.fromutc(value)
    except Exception as e:
        print('bad-input', type(e).__name__, str(e))
# 基类 fromutc 要求两个回调非 None，override 则保留用户异常身份。
class Missing(tzinfo):
    def __init__(self, which):
        self.which = which
    def utcoffset(self, dt):
        if self.which == 'offset':
            return None
        return timedelta(0)
    def dst(self, dt):
        return None
for which in ('offset', 'dst'):
    missing = Missing(which)
    try:
        missing.fromutc(a.replace(tzinfo=missing))
    except Exception as e:
        print('missing', which, type(e).__name__, str(e))
error = RuntimeError('fromutc-original')
class Boom(Target):
    def fromutc(self, dt):
        raise error
try:
    a.astimezone(Boom())
except RuntimeError as e:
    print('fromutc-error', e is error)

# 用户 fromutc override 的返回值不被 astimezone 再限制为 datetime。
answer = ('custom', 42)
class TupleTarget(Target):
    def fromutc(self, dt):
        print('tuple-input', dt.tzinfo is self, dt.hour)
        return answer
result = a.astimezone(TupleTarget())
print('tuple-result', result is answer, result)
