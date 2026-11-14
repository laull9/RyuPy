import typing as t
C=t.Callable
T=t.TypeVar('T')
x=C[[T],int]
t.Callable=list
assert C[[T],int] is x
fresh=C[[T],str]
assert fresh.__args__==(T,str)
# inner miss reads its constructor, not the exported root or _CallableType.
t._CallableType=None
assert C[[T],bytes].__args__==(T,bytes)
t._check_generic=None
assert C[[],float].__args__==(float,)
t._type_check=None
assert C[[T],int] is x
try: C[[T],complex]
except TypeError: pass
else: assert False
print('saved receiver; _CallableType and _check_generic not read; warm bypass check: ok')
