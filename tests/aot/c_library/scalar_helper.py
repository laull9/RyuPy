"""CPython真实标量来源与拒绝输入；转换hook计数必须始终为零。"""
import json
import sys

_hooks = 0


def values():
    return json.loads('{"i8":-128,"u8":255,"i16":-32768,"u16":65535,'
        '"i32":-2147483648,"u32":4294967295,"i64":-9223372036854775808,'
        '"u64":18446744073709551615,"isize":-4096,"usize":4096,'
        '"f32":1.25,"f64":-3.5,"bool8":true,"text":"中文\\u0000🌍"}')


def raw_bytes():
    return b'a\x00\xffz'


def empty_bytes():
    return b''


def edges(kind):
    # 由CP创建边界值；指针宽度取本次固定解释器的真实宿主位宽。
    width = {'i8': 8, 'u8': 8, 'i16': 16, 'u16': 16, 'i32': 32, 'u32': 32,
             'i64': 64, 'u64': 64, 'isize': sys.maxsize.bit_length() + 1,
             'usize': sys.maxsize.bit_length() + 1}[kind]
    signed = kind.startswith('i')
    low = -(1 << (width - 1)) if signed else 0
    high = (1 << (width - 1 if signed else width)) - 1
    return json.loads(json.dumps([low, high, high + 1]))


def more_values():
    return json.loads('{"rounding":1.1,"positive_overflow":1e300,"negative_overflow":-1e300,'
                      '"false":false,"empty":""}')


class Trap:
    def __int__(self):
        global _hooks
        _hooks += 1
        return 17
    __index__ = __int__
    def __float__(self):
        global _hooks
        _hooks += 1
        return 1.25
    def __str__(self):
        global _hooks
        _hooks += 1
        return 'converted'
    def __bytes__(self):
        global _hooks
        _hooks += 1
        return b'converted'


class IntegerSubclass(int):
    __int__ = Trap.__int__
    __index__ = Trap.__index__


class FloatSubclass(float):
    __float__ = Trap.__float__


class StringSubclass(str):
    __str__ = Trap.__str__


class BytesSubclass(bytes):
    __bytes__ = Trap.__bytes__


def rejected(kind):
    return {'trap': Trap(), 'int_subclass': IntegerSubclass(4096),
            'float_subclass': FloatSubclass(1.25), 'str_subclass': StringSubclass('text'),
            'bytes_subclass': BytesSubclass(b'bytes'), 'wrong_bool': 4096,
            'wrong_int': True, 'wrong_float': 4096, 'wrong_str': b'bytes',
            'wrong_bytes': 'text', 'overflow': 10 ** 5000, 'f32_overflow': 1e300}[kind]


def hooks():
    return _hooks
