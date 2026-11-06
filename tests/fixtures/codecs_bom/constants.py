# codecs 的有限真实 BOM 合同：值、身份、宿主端序、bytes 与模块槽位。
import codecs
import sys
from codecs import BOM, BOM_UTF16, BOM_UTF32, BOM_UTF8
from codecs import BOM_LE, BOM_BE, BOM32_LE, BOM32_BE, BOM64_LE, BOM64_BE
from codecs import BOM_UTF16_LE, BOM_UTF16_BE, BOM_UTF32_LE, BOM_UTF32_BE

assert sys.byteorder in ('little', 'big')
assert BOM_UTF8 == b'\xef\xbb\xbf'
assert BOM_UTF16_LE == b'\xff\xfe'
assert BOM_UTF16_BE == b'\xfe\xff'
assert BOM_UTF32_LE == b'\xff\xfe\x00\x00'
assert BOM_UTF32_BE == b'\x00\x00\xfe\xff'
values = (BOM_UTF8, BOM_UTF16_LE, BOM_UTF16_BE, BOM_UTF32_LE, BOM_UTF32_BE)
for value in values:
    assert type(value) is bytes
for i in range(5):
    for j in range(5):
        assert (values[i] is values[j]) == (i == j)
print('B1.values', values)

assert BOM_LE is BOM32_LE and BOM_LE is BOM_UTF16_LE
assert BOM_BE is BOM32_BE and BOM_BE is BOM_UTF16_BE
assert BOM64_LE is BOM_UTF32_LE and BOM64_BE is BOM_UTF32_BE
assert BOM is BOM_UTF16
if sys.byteorder == 'little':
    assert BOM is BOM_UTF16_LE and BOM_UTF32 is BOM_UTF32_LE
else:
    assert BOM is BOM_UTF16_BE and BOM_UTF32 is BOM_UTF32_BE
print('B1.aliases', sys.byteorder, BOM, BOM_UTF32)

assert len(BOM_UTF8) == 3
assert BOM_UTF8[0] == 239 and BOM_UTF8[-1] == 191
assert BOM_UTF32_LE[:2] == BOM_UTF16_LE
assert BOM_UTF32_BE[2:] == BOM_UTF16_BE
assert BOM_UTF16_LE + b'\x00\x00' == BOM_UTF32_LE
assert BOM_UTF8 * 2 == b'\xef\xbb\xbf\xef\xbb\xbf'
try:
    BOM_UTF8[0] = 0
except TypeError as error:
    assert str(error) == "'bytes' object does not support item assignment"
    print('B1.immutable', str(error))
else:
    raise AssertionError('BOM bytes 被原地修改')

# 别名仅捕获初始化对象；重绑一个模块成员不得动态联动其它成员或已导入名。
old = codecs.BOM_UTF16_LE
codecs.BOM_UTF16_LE = b'local replacement'
from codecs import BOM_UTF16_LE as rebound
assert rebound == b'local replacement'
assert BOM_UTF16_LE is old
assert codecs.BOM_LE is old and codecs.BOM32_LE is old
if sys.byteorder == 'little':
    assert codecs.BOM is old and codecs.BOM_UTF16 is old
codecs.BOM_UTF16_LE = old
assert codecs.BOM_UTF16_LE is BOM_UTF16_LE
old_native = codecs.BOM
codecs.BOM = b'native replacement'
assert codecs.BOM_UTF16 is old_native and BOM is old_native
codecs.BOM = old_native
import codecs as again
assert again is codecs and again.BOM is BOM
assert again.BOM_UTF8 is BOM_UTF8
print('B1.rebind', True)

# 真正未知名在两端均缺失；不拿 CPython 已有 encode/decode 冒充缺失 API。
try:
    codecs.ryupy_missing_bom_member
except AttributeError:
    print('B1.unknown-attribute', True)
else:
    raise AssertionError('未知成员被伪造')
try:
    from codecs import ryupy_missing_bom_member
except ImportError:
    print('B1.unknown-import', True)
else:
    raise AssertionError('未知导入被伪造')
print('B1.complete', True)
