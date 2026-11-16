# v3 ABI 差分入口：边界标量、借用缓冲区、错误前置与函数绑定。
import abi
import sys

if len(sys.argv) > 1:
    for index in range(128):
        try:
            abi.optional(b'held\x00\xff')
        except ImportError:
            pass
        else:
            raise AssertionError('missing native input unexpectedly loaded')
    print('loader-failure-ok')
    sys.exit(0)


def rejected(function, value):
    try:
        function(value)
    except (TypeError, OverflowError, ValueError, UnicodeError) as error:
        print(type(error).__name__)


for name, low, high in [
    ('i8', -128, 127), ('u8', 0, 255),
    ('i16', -32768, 32767), ('u16', 0, 65535),
    ('i32', -2147483648, 2147483647), ('u32', 0, 4294967295),
    ('i64', -9223372036854775808, 9223372036854775807),
    ('u64', 0, 18446744073709551615),
    ('isize', -9223372036854775808, 9223372036854775807),
    ('usize', 0, 18446744073709551615),
]:
    function = getattr(abi, name)
    print(name, function(low), function(high), function(0))
    for value in [low - 1, high + 1, True, 1.0, '1', None]:
        rejected(function, value)

print(abi.f32(1.25), abi.f64(-2.5))
print(abi.f32(0.1) == 0.10000000149011612)
print(abi.f32(1e-45) == 1.401298464324817e-45)
print(abi.f32(-0.0), abi.f64(-0.0))
print(abi.f64(float('inf')), abi.f32(float('-inf')))
print(abi.f64(float('nan')) != abi.f64(float('nan')))
rejected(abi.f32, 1e100)
rejected(abi.f64, True)
rejected(abi.f64, 1)
print(abi.bool8(True), abi.bool8(False))
rejected(abi.bool8, 1)
try:
    abi.bad_bool()
except ValueError:
    print('invalid bool result')
for index in range(128):
    try:
        abi.bad_bool_buffer(b'held\x00\xff')
    except ValueError:
        pass
    else:
        raise AssertionError('invalid bool result accepted')
print('return-failure-ok')

for text in ['', 'abc', '中文🌍', 'a\x00b']:
    print(abi.utf8(text), len(text.encode('utf-8')))
for data in [b'', b'abc', b'\x00\xff\x80']:
    print(abi.bytes(data), len(data))
rejected(abi.utf8, b'a')
rejected(abi.utf8, 'a' + chr(0xd800) + 'b')
rejected(abi.bytes, 'a')
rejected(abi.bytes, bytearray(b'a'))
print(abi.mix('中\x00', b'\xff\x00', 7))
before = abi.count()
for index in range(128):
    try:
        abi.mix('held', b'held', True)
    except TypeError:
        pass
print(abi.count() == before)
assert abi.optional(b'abc') == 297
assert abi.i32_alias(123) == 123 and abi.i32_alias is not abi.i32
print(abi.empty(), abi.sixteen(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10.0, 11.0, 12, 13, 14, 15))
buffers = [b'', b'a', b'\x00\xff', b'\x80'] * 4
assert abi.sixteen_buffers(*buffers) == sum(len(data) + sum(data) for data in buffers)
print('32-machine-arguments-ok')
for arguments in [(), (1, 2)]:
    try:
        abi.i8(*arguments)
    except TypeError:
        print('arity')
try:
    abi.i8(value=1)
except TypeError:
    print('posonly')


class Converted:
    def __index__(self):
        raise RuntimeError('conversion hook must not run')

    def __float__(self):
        raise RuntimeError('conversion hook must not run')


rejected(abi.i32, Converted())
rejected(abi.f64, Converted())
