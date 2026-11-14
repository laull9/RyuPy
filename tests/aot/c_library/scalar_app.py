"""CPython真实标量直接接声明C ABI，精确类型、范围和借用buffer均沿原合同。"""
import scalar_helper
import native_demo


def check_edges(function, name):
    edges = scalar_helper.edges(name)
    assert function(edges[0]) == edges[0]
    assert function(edges[1]) == edges[1]


values = scalar_helper.values()
assert native_demo.i8(values['i8']) == -128
assert native_demo.u8(values['u8']) == 255
assert native_demo.i16(values['i16']) == -32768
assert native_demo.u16(values['u16']) == 65535
assert native_demo.i32(values['i32']) == -2147483648
assert native_demo.u32(values['u32']) == 4294967295
assert native_demo.i64(values['i64']) == -9223372036854775808
assert native_demo.u64(values['u64']) == 18446744073709551615
assert native_demo.isize(values['isize']) == -4096
assert native_demo.usize(values['usize']) == 4096
assert native_demo.f32(values['f32']) == 1.25
assert native_demo.f64(values['f64']) == -3.5
assert native_demo.boolean(values['bool8']) is True
assert native_demo.touch(values['i32']) is None
assert native_demo.last() == -2147483648
check_edges(native_demo.i8, 'i8')
check_edges(native_demo.u8, 'u8')
check_edges(native_demo.i16, 'i16')
check_edges(native_demo.u16, 'u16')
check_edges(native_demo.i32, 'i32')
check_edges(native_demo.u32, 'u32')
check_edges(native_demo.i64, 'i64')
check_edges(native_demo.u64, 'u64')
check_edges(native_demo.isize, 'isize')
check_edges(native_demo.usize, 'usize')
extra = scalar_helper.more_values()
assert native_demo.f32(extra['rounding']) == 1.100000023841858
assert native_demo.boolean(extra['false']) is False
assert native_demo.utf8(extra['empty']) == 0
assert native_demo.data(scalar_helper.empty_bytes()) == 0
print(native_demo.utf8(values['text']))
print(native_demo.data(scalar_helper.raw_bytes()))
print('真实标量13种/void/借用buffer通过')
