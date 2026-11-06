# CP合法非类型值与ForwardRef保完整结果；F1a未知域NI不能冒充CP通过。
import typing as t
for value in ['Thing', 42, True, 2.5, ..., [int], {'x': int}, {int}]:
    a = t.List[value]
    b = t.List[value]
    print('legal-value', repr(a), a is b, a == b, a.__parameters__)
f = t.List['Thing'].__args__[0]
assert f.__forward_arg__ == 'Thing'
assert f.__forward_is_argument__ and not f.__forward_is_class__
