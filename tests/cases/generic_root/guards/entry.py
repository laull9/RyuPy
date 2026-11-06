# 导出根后空/非空/功能式NamedTuple与TypeVar bound不得被存在性守卫误伤。
import typing as t
class Empty(t.NamedTuple):
    pass
class Record(t.NamedTuple):
    value: int
Functional = t.NamedTuple('Functional', [('value', str)])
FunctionalEmpty = t.NamedTuple('FunctionalEmpty', [])
assert Empty() == () and FunctionalEmpty() == ()
assert Record(7).value == 7 and Functional('ok').value == 'ok'
assert Record.__annotations__['value'] is int
assert t.TypeVar('B', bound=int).__bound__ is int
assert t.TypeVar('S', bound=str).__bound__ is str
assert t.TypeVar('N', bound=None).__bound__ is None
T = t.TypeVar('T')
for annotation in (t.Generic, t.Generic[T]):
    try:
        t.TypeVar('Bad', bound=annotation)
    except TypeError as error:
        print('bound', str(error))
    else:
        assert False
    try:
        t.NamedTuple('Bad', [('x', annotation)])
    except TypeError as error:
        print('functional', str(error))
    else:
        assert False
    try:
        class Bad(t.NamedTuple):
            x: annotation
    except TypeError as error:
        print('class', str(error))
    else:
        assert False
print('guards', True)
