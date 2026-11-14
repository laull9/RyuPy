# 真正的普通用户单基，源模块导入且保留对象字段与回调身份。
from datetime import tzinfo, timedelta
class Zone(tzinfo):
    def __init__(self, minutes, label):
        self.minutes = minutes
        self.label = label
        self.expected = None
    def utcoffset(self, dt):
        print('offset-identity', dt is self.expected, dt is None)
        return timedelta(minutes=self.minutes)
    def dst(self, dt):
        print('dst-identity', dt is self.expected, dt is None)
        return timedelta(0)
    def tzname(self, dt):
        print('name-identity', dt is self.expected, dt is None)
        return self.label
