# 回调返回检查与异常透传；边界严格小于一天。
from datetime import tzinfo, timedelta, datetime, time
class Zone(tzinfo):
    def __init__(self, value):
        self.value = value
    def utcoffset(self, dt):
        return self.value
    def dst(self, dt):
        return self.value
    def tzname(self, dt):
        return self.value
for label, value in [('none', None), ('int', 1), ('string', 'name'),
                     ('plus24', timedelta(hours=24)), ('minus24', timedelta(hours=-24)),
                     ('insideplus', timedelta(days=1, microseconds=-1)),
                     ('insideminus', timedelta(days=-1, microseconds=1))]:
    z = Zone(value)
    for obj in (datetime(2024, 1, 2, tzinfo=z), time(3, 4, tzinfo=z)):
        for method in ('utcoffset', 'dst', 'tzname'):
            try:
                result = getattr(obj, method)()
                print(label, type(obj).__name__, method, result)
            except Exception as e:
                print(label, type(obj).__name__, method, type(e).__name__, str(e))
class MarkerError(Exception):
    pass
error = MarkerError('original')
class Boom(tzinfo):
    def utcoffset(self, dt):
        raise error
    def dst(self, dt):
        raise error
    def tzname(self, dt):
        raise error
for method in ('utcoffset', 'dst', 'tzname'):
    try:
        getattr(datetime(2024, 1, 2, tzinfo=Boom()), method)()
    except MarkerError as e:
        print('propagate', method, e is error)
# tzinfo 非 None 但偏移为 None 仍按 naive 比较；没有隐含本地时区转换。
n = datetime(2024, 1, 2, tzinfo=Zone(None))
plain = datetime(2024, 1, 2)
print('none-aware', n == plain, n - plain, n.isoformat())
