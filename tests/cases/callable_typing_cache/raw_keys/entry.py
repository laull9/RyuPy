import typing as t
C=t.Callable
T=t.TypeVar('T')
a=C[[T],None]; b=C[(T,),None]; c=C[T,None]
assert a is b and a == c and a is not c
assert C[([T],None)] is a and C[((T,),None)] is a
assert C[T,None] is c
r=C[[T],type(None)]
assert a == r and a is not r
n=C[[None],T]; nt=C[[type(None)],T]
assert n == nt and n is not nt
assert n is C[(None,),T] and nt is C[(type(None),),T]
e=C[[],T]
assert e is C[(),T]
args=[T]
pair=(args,int)
f=C[pair]
args.append(str)
g=C[pair]
assert f is not g and f.__args__ == (T,int) and g.__args__ == (T,str,int)
args.pop()
assert C[pair] is f
print('list/tuple shared; scalar distinct; raw None distinct; empty shared; input list snapshot: ok')
