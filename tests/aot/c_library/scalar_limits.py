"""真实外部标量的严格拒绝：错误类型/子类/转换协议不进入C，巨大整数仍按range拒绝。"""
import scalar_helper
import native_demo


def check_overflow(function, value):
    try:
        function(value)
    except OverflowError:
        pass
    else:
        raise AssertionError('越界标量进入C')


before = native_demo.calls()
for name in ['trap', 'int_subclass', 'wrong_int']:
    try:
        native_demo.i32(scalar_helper.rejected(name))
    except TypeError:
        pass
    else:
        raise AssertionError('非精确整数进入C')
for name in ['trap', 'float_subclass', 'wrong_float']:
    try:
        native_demo.f64(scalar_helper.rejected(name))
    except TypeError:
        pass
    else:
        raise AssertionError('非精确浮点进入C')
for name in ['trap', 'str_subclass', 'wrong_str']:
    try:
        native_demo.utf8(scalar_helper.rejected(name))
    except TypeError:
        pass
    else:
        raise AssertionError('非精确str进入C')
for name in ['trap', 'bytes_subclass', 'wrong_bytes']:
    try:
        native_demo.data(scalar_helper.rejected(name))
    except TypeError:
        pass
    else:
        raise AssertionError('非精确bytes进入C')
try:
    native_demo.boolean(scalar_helper.rejected('wrong_bool'))
except TypeError:
    pass
else:
    raise AssertionError('非bool进入C bool8')
try:
    native_demo.i64(scalar_helper.rejected('overflow'))
except OverflowError:
    pass
else:
    raise AssertionError('巨大整数未按range拒绝')
try:
    native_demo.f32(scalar_helper.rejected('f32_overflow'))
except OverflowError:
    pass
else:
    raise AssertionError('float32溢出未拒绝')
check_overflow(native_demo.i8, scalar_helper.edges('i8')[2])
check_overflow(native_demo.u8, scalar_helper.edges('u8')[2])
check_overflow(native_demo.i16, scalar_helper.edges('i16')[2])
check_overflow(native_demo.u16, scalar_helper.edges('u16')[2])
check_overflow(native_demo.i32, scalar_helper.edges('i32')[2])
check_overflow(native_demo.u32, scalar_helper.edges('u32')[2])
check_overflow(native_demo.i64, scalar_helper.edges('i64')[2])
check_overflow(native_demo.u64, scalar_helper.edges('u64')[2])
check_overflow(native_demo.isize, scalar_helper.edges('isize')[2])
check_overflow(native_demo.usize, scalar_helper.edges('usize')[2])
check_overflow(native_demo.f32, scalar_helper.more_values()['positive_overflow'])
check_overflow(native_demo.f32, scalar_helper.more_values()['negative_overflow'])
assert scalar_helper.hooks() == 0
assert native_demo.calls() == before
print('严格类型/范围/无转换hook/未进入C通过')
