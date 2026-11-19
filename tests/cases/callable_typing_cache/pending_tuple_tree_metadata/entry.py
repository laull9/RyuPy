# CP合法内部tuple形状的完整合同；旧错误草稿和双失败原流独立保留。
import typing as t
C=t.Callable
T=t.TypeVar('T'); S=t.TypeVar('S')
a=C[[(T,S,T)],int]
assert a.__args__==((T,S,T),int) and a.__parameters__==(T,S)
assert t.get_args(a)==((T,S,T),int) and t.get_args(a) is a.__args__
assert repr(a)=='typing.Callable[[~T, ~S, ~T], int]'
# copy_with绕过类型检查，typing alias构造器确实递归内部tuple参数。
nested=t.List.copy_with(((T,),))
assert nested.__args__==((T,),) and nested.__parameters__==(T,)
b=C[[nested],S]
assert b.__args__==(nested,S) and b.__parameters__==(T,S)
assert t.get_args(b)==([nested],S)
# native alias的collector不同，不能虚构它已经递归tuple。
native=list[((T,),)]
assert native.__args__==((T,),) and native.__parameters__==()
c=C[[native],S]
assert c.__parameters__==(S,) and t.get_args(c)==([native],S)
print('tuple tree public shape; typing nested collector; native distinction: ok')
