import typing as t
from collections import namedtuple
from collections.abc import Callable, Iterable, Sized, Container
T=t.TypeVar('T'); U=t.TypeVar('T'); events=[]
d={T:'old'}; old=hash(T)
def poison(*args):
    events.append('poison')
    raise RuntimeError('poison')
names=('__hash__','__eq__','__call__','__getitem__','__iter__','__len__','__contains__','__next__','__length_hint__','__trunc__')
for name in names:
    setattr(T,name,poison)
    assert getattr(T,name) is poison
assert hash(T)==old and d[T]=='old' and len({T,U})==2
assert not callable(T)
for abc in (Callable,Iterable,Sized,Container):
    assert not isinstance(T,abc)
assert events==[]
print('fixed-slots', True)
class Other:
    def __eq__(self, other):
        events.append('right')
        return True
assert T==Other()
assert events==['right']
print('right-reflection',events)
events.clear()
def reject(label, call):
    try:
        call()
    except TypeError as error:
        print(label,type(error).__name__,str(error))
    else:
        assert False
reject('missing-call',lambda:T())
reject('missing-getitem',lambda:T[0])
reject('missing-iter',lambda:iter(T))
reject('missing-length',lambda:len(T))
items=[1]
reject('list-extend',lambda:items.extend(T))
assert items==[1]
Pair=namedtuple('Pair','left right')
reject('namedtuple-make',lambda:Pair._make(T))
reject('namedtuple-defaults',lambda:namedtuple('NewPair','left right',defaults=T))
def unpack():
    first,*rest=T
reject('star-unpack',unpack)
def unpack_exact():
    first,second=T
reject('exact-unpack',unpack_exact)
assert events==[]
print('no-poison-events',events)
for name in names:
    setattr(T,name,None)
assert hash(T)==old and d[T]=='old' and T!=U and not callable(T)
for abc in (Callable,Iterable,Sized,Container):
    assert not isinstance(T,abc)
for name in names:
    delattr(T,name)
assert hash(T)==old and d[T]=='old' and T!=U
print('write-delete-key',True)
