import typing as t
C=t.Callable
T=t.TypeVar('Nested')
first=t.List[T]; second=t.List[(T,)]
assert first is not second and first==second
arg=C[[first],int]
assert C[(second,),int] is arg
assert arg.__args__[0] is first
result=C[[],first]
assert C[(),second] is result and result.__args__[0] is first
scalar=C[first,int]
assert C[second,int] is scalar and scalar is not arg and scalar==arg
# 相同对象的Literal键允许命中，但不能把合法hash消费等同于永远零副作用。
literal=t.Literal['key']
x=C[[literal],T]
assert C[(literal,),T] is x
print('equal alias raw keys; preserved stored objects; scalar separation: ok')
