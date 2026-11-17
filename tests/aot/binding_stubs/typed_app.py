# 标量和调用期借用ABI：保留字参数通过位置调用，机器边界由运行时检查。
import river_codec as native

print(native.integer(18446744073709551615), native.small(-128))
print(native.real(1.25), native.flag(True), native.empty() is None)
print(getattr(native, 'class')('中\x00文'), native.bytes(b'\x00\xff'))
print(native.mix('甲', b'\x00\xff', 7), native.integer is native.alias)
for kind in ['arity', 'kwargs', 'range', 'bool', 'float', 'float-range', 'str', 'bytes', 'utf8', 'return']:
    try:
        if kind == 'arity':
            native.integer()
        elif kind == 'kwargs':
            native.integer(value=1)
        elif kind == 'range':
            native.integer(-1)
        elif kind == 'bool':
            native.integer(True)
        elif kind == 'float':
            native.real(1)
        elif kind == 'float-range':
            native.real(1e100)
        elif kind == 'str':
            getattr(native, 'class')(b'x')
        elif kind == 'bytes':
            native.bytes('x')
        elif kind == 'utf8':
            getattr(native, 'class')('\ud800x')
        else:
            native.bad_bool()
    except Exception as error:
        print(kind, type(error).__name__)
print('typed-stub-ok')
