# 暖命中前仍消费Literal hash与等价alias eq所依赖的helper。
import typing as t
T=t.TypeVar('T')
key=t.Literal[17]
a=t.Tuple[key]
original=t._value_and_type_iter
log=[]
def values(args):
    log.append('literal-hash')
    return original(args)
t._value_and_type_iter=values
assert t.Tuple[key] is a
assert log==['literal-hash']
print('warm-key-hash-still-consumed',log)
t._value_and_type_iter=original
first=t.Set[T]
second=t.Set[(T,)]
assert first is not second and first==second
outer=t.Tuple[first]
t._GenericAlias=None
assert t.Tuple[first] is outer
try:
    t.Tuple[second]
except TypeError as e:
    print('warm-key-eq-typeerror-then-body',str(e))
else:
    assert False
