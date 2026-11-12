# 最小独立Native正合同；不是完整datetime或真实toml通过声明。
import datetime as mod
from datetime import tzinfo, timezone, datetime, time
from zones import Zone
print('root', tzinfo is mod.tzinfo, isinstance(tzinfo, type))
print('types', issubclass(Zone, tzinfo), issubclass(timezone, tzinfo), issubclass(Zone, timezone))
for minutes, name in ((330, 'east'), (-225, 'west'), (0, 'zero')):
    z = Zone(minutes, name)
    print('instance', isinstance(z, tzinfo), isinstance(z, timezone), z.minutes, z.label)
    d = datetime(1979, 5, 27, 7, 32, tzinfo=z)
    z.expected = d
    print('datetime-zone', d.tzinfo is z)
    print('datetime-offset', d.utcoffset())
    print('datetime-dst', d.dst())
    print('datetime-name', d.tzname())
    print('datetime-iso', d.isoformat())
    t = time(7, 32, tzinfo=z)
    z.expected = None
    print('time-zone', t.tzinfo is z)
    print('time-offset', t.utcoffset())
    print('time-dst', t.dst())
    print('time-name', t.tzname())
    print('time-iso', t.isoformat())
print('tzinfo-native-complete')
