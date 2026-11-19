# 保存receiver和bound method不随公开Tuple重绑改变，暖命中跳过冷helper。
import typing as t
root=t.Tuple
saved_getitem=root.__getitem__
T=t.TypeVar('T')
a=root[T]
t.Tuple=None
b=saved_getitem((T,))
assert a is not b and a == b
assert b.__origin__ is tuple
for helper in ['_type_check','_type_convert','_check_generic','_collect_parameters','_GenericAlias','_TypingEllipsis','Generic','ClassVar','_TupleType']:
    setattr(t,helper,None)
    assert root[T] is a and saved_getitem((T,)) is b
print('saved-receiver-cold-and-warm',True)
