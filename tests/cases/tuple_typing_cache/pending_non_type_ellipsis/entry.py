# CP合法的单独/中间Ellipsis、ForwardRef和非类型输入完整保留，不能伪造TypeError。
import typing as t
for raw in [Ellipsis,(Ellipsis,),(Ellipsis,int),(Ellipsis,Ellipsis),(int,Ellipsis,str),7,True,'ForwardName',[int],{'x':int}]:
    a=t.Tuple[raw]
    print(repr(a))
    assert a.__origin__ is tuple
u=[int]
a=t.Tuple[u]
b=t.Tuple[u]
assert a is not b and a.__args__[0] is u and b.__args__[0] is u
print('unhashable-body-success-no-insert',True)
