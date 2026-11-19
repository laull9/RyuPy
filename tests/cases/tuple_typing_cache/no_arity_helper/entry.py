# Tuple不消费普通根arity helper；任意多个有限合法参数及多前缀尾Ellipsis成立。
import typing as t
t._check_generic=None
T=t.TypeVar('T')
for raw in [(),T,(T,),(int,str),(T,int,...),(None,...)]:
    alias=t.Tuple[raw]
    assert alias.__origin__ is tuple
print('tuple-never-consumes-check-generic',True)
