# 日期采用公历与微秒整数；固定时区不能套用宿主14小时offset上限。
from datetime import date, datetime, time, timedelta, timezone

print(date(2024, 2, 29).isoformat(), date(2024, 2, 29).weekday())
print(date(2024, 3, 1) - date(2024, 2, 28))
print(date(2024, 2, 28) + timedelta(days=2))
for delta in [timedelta(), timedelta(seconds=-1), timedelta(microseconds=-1), timedelta(days=2, seconds=3, microseconds=4)]:
    print(str(delta), delta.days, delta.seconds, delta.microseconds, delta.total_seconds())
print(timedelta(seconds=0.5) + timedelta(milliseconds=500))
print(time(1, 2, 3, 456).isoformat())
value = datetime(2024, 2, 28, 23, 59, 59, 999999, tzinfo=timezone.utc)
print((value + timedelta(microseconds=1)).isoformat())
print((value - timedelta(days=1)).isoformat())
print((value + timedelta(seconds=2)) - value)
print(timezone(timedelta(hours=20)).utcoffset(None))
print(timezone(timedelta(hours=-3, minutes=-30)).tzname(None))
print(datetime(2024, 1, 1, tzinfo=timezone(timedelta(hours=2))).isoformat())
print(datetime(2024, 1, 1, 2, tzinfo=timezone(timedelta(hours=2))) == datetime(2024, 1, 1, tzinfo=timezone.utc))
for action in [lambda: date(2023, 2, 29), lambda: datetime(2024, 1, 1, 24), lambda: timezone(timedelta(hours=24))]:
    try:
        action()
    except ValueError:
        print('ValueError')
