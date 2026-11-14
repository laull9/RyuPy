# canonical ABC根图、抽象默认体和mixin边界，不以实例化失败替代super默认语义。
from collections import abc
from collections.abc import Generator
names = ['Iterable', 'Iterator', 'Reversible', 'Generator', 'Sized', 'Container', 'Callable', 'Collection', 'Sequence', 'MutableSequence', 'Set', 'MutableSet', 'Mapping', 'MutableMapping', 'MappingView', 'KeysView', 'ItemsView', 'ValuesView']
for name in names:
    root = getattr(abc, name)
    print(name, sorted(root.__abstractmethods__), tuple(base.__name__ for base in root.__bases__))
print(list(abc.Iterable.__iter__(None)))
print(list(abc.Reversible.__reversed__(None)))
print(abc.Sized.__len__(None), abc.Container.__contains__(None, 1), abc.Callable.__call__(None, 1, x=2))
for root, name, args in [(abc.Iterator, '__next__', (None,)), (abc.Sequence, '__getitem__', (None, 0)), (abc.MutableSequence, '__setitem__', (None, 0, 1)), (abc.MutableSequence, 'insert', (None, 0, 1)), (abc.Mapping, '__getitem__', (None, 'x')), (abc.MutableMapping, '__setitem__', (None, 'x', 1)), (abc.MutableSet, 'add', (None, 1))]:
    try:
        getattr(root, name)(*args)
    except Exception as e:
        print(root.__name__, name, type(e).__name__, e.args)
class Gen(Generator):
    def send(self, value): raise StopIteration()
    def throw(self, typ, val=None, tb=None): raise typ
print(Gen().close())
print(abc.Set.__and__ is abc.Set.__rand__, abc.Set.__or__ is abc.Set.__ror__, abc.Set.__xor__ is abc.Set.__rxor__)
for root in (abc.Sized, abc.Callable):
    try: root[int]
    except TypeError: print(root.__name__, 'TypeError')
class Left:
    def __eq__(self, other):
        print('left eq')
        return True
class Right:
    def __eq__(self, other):
        print('right eq')
        return False
print(Right() in abc.ValuesView({'x': Left()}))
print(('x', Right()) in abc.ItemsView({'x': Left()}))
for typ, value in [(ValueError, KeyError('payload')), (ValueError, 'invalid')]:
    try: abc.Generator.throw(None, typ, value)
    except Exception as e: print(type(e).__name__, e.args)
for name in ['__and__', '__or__', '__sub__', '__rsub__', '__xor__']:
    print(name, getattr(abc.Set, name)(abc.KeysView({}), 1) is NotImplemented)
