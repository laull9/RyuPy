# 时间差浮点舍入、固定时区哈希与 naive/aware 错误边界。
from datetime import date, datetime, time, timedelta, timezone, tzinfo

print(isinstance(timezone.utc, tzinfo), isinstance(datetime(2024, 1, 1), date))
print(timedelta(milliseconds=0.0005).microseconds)
print(timedelta(microseconds=1.5).microseconds)
print(timedelta(microseconds=3) / 2)
print(-timedelta(microseconds=1), abs(timedelta(seconds=-1)))
print(date(2024, 1, 2) + timedelta(seconds=-1))
a = datetime(2024, 1, 1, 2, tzinfo=timezone(timedelta(hours=2)))
b = datetime(2024, 1, 1, tzinfo=timezone.utc)
print(a == b, hash(a) == hash(b), a - b)
print(a.astimezone(timezone.utc), a.timestamp() == b.timestamp())
print(a.date(), a.time(), a.timetz())
print(a.replace(year=2025).isoformat(timespec="minutes"))
print(time(1, 2, 3, 456789).isoformat(timespec="milliseconds"))
print(time(2, tzinfo=timezone(timedelta(hours=2))) == time(0, tzinfo=timezone.utc))
print(timezone(timedelta(seconds=30, microseconds=2)).tzname(None))
for fn in [lambda: a < datetime(2024, 1, 1), lambda: a - datetime(2024, 1, 1), lambda: time(1) < time(1, tzinfo=timezone.utc)]:
    try:
        fn()
    except TypeError:
        print("TypeError")
print(date.fromordinal(1), datetime.fromordinal(1))
print(repr(datetime(2024, 2, 3, 4, 5, 0, 1, tzinfo=timezone.utc)))

zone = time().tzinfo
print(zone is None)
zone = datetime(2024, 1, 1).tzinfo
print(zone is None)

for us in [1, 2, 7]:
    print(timedelta(days=999999999, microseconds=us).total_seconds() == 86399999913600.0)
