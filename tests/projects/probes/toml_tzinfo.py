# 真实未修改 toml 包：Z 与正负偏移，解析、序列化和再次解析。
import toml
from toml.tz import TomlTz
from datetime import tzinfo, datetime
for suffix in ('Z', '+05:30', '-03:45'):
    source = 'stamp = 1979-05-27T07:32:00' + suffix + '\n'
    data = toml.loads(source)
    dt = data['stamp']
    print(suffix, isinstance(dt, datetime), isinstance(dt.tzinfo, TomlTz), isinstance(dt.tzinfo, tzinfo))
    print(dt.isoformat(), dt.utcoffset(), dt.dst(), dt.tzname())
    output = toml.dumps(data)
    print(repr(output))
    other = toml.loads(output)['stamp']
    print(other == dt, other.utcoffset() == dt.utcoffset())
