# ryupy: pending
# zip_longest真实类型、惰性构造、调用签名、值与共享引用，不导入第三方项目。
import itertools
from itertools import zip_longest

constructor = zip_longest
z = constructor()
assert type(z) is constructor and z.__class__ is constructor
assert z.__class__ is z.__class__ and isinstance(z, constructor)
assert isinstance(z, object) and issubclass(constructor, object)
assert iter(z) is z and z.__iter__() is z
assert constructor.__name__ == 'zip_longest' and constructor.__module__ == 'itertools'
assert next(z, 'END') == 'END' and next(z, 'END') == 'END'
print('Z1.type', type(z) is constructor, iter(z) is z, constructor.__module__)

for label, args, expected in [
    ('zero', (), []), ('empty', ([],), []), ('all-empty', ([], []), []),
    ('one', ([1, 2],), [(1,), (2,)]),
    ('positional-source', ([1], 'xy'), [(1, 'x'), (None, 'y')]),
    ('unequal', ([1, 2], (10,), 'abc'), [(1, 10, 'a'), (2, None, 'b'), (None, None, 'c')]),
]:
    rows = list(constructor(*args))
    assert rows == expected and all(type(row) is tuple for row in rows)
    print('Z1.' + label, rows)

assert tuple(constructor([1, 2], **{'fillvalue': 9})) == ((1,), (2,))
assert list(constructor(*[[], [1]], **{'fillvalue': 7})) == [(7, 1)]
saved = constructor
container = [saved]
assert list(container[0]([1])) == [(1,)]
try:
    itertools.zip_longest = lambda: 'replacement'
    assert itertools.zip_longest() == 'replacement'
    assert type(saved()) is saved and type(z) is saved
    print('Z1.alias', itertools.zip_longest(), type(z) is saved)
finally:
    itertools.zip_longest = saved

shared = []
z = constructor([1, 2], [], [], fillvalue=shared)
first = next(z)
second = z.__next__()
assert first is not second and first[1] is shared and first[2] is shared
assert second[1] is shared and second[2] is shared
shared.append('changed')
assert first == (1, ['changed'], ['changed']) and second == (2, ['changed'], ['changed'])
assert next(z, 'END') == 'END'
print('Z1.fill-reference', first, second, first is not second)


class PoisonFill:
    def __bool__(self):
        raise AssertionError('fill truth forbidden')

    def __iter__(self):
        raise AssertionError('fill iteration forbidden')

    def __str__(self):
        raise AssertionError('fill conversion forbidden')


fill = PoisonFill()
row = next(constructor([], [1], fillvalue=fill))
assert row[0] is fill and row[1] == 1
assert list(constructor(fillvalue=fill)) == []
print('Z1.fill-no-protocol', row[0] is fill)

for action in ['read', 'write', 'delete']:
    z = constructor(fillvalue=fill)
    try:
        if action == 'read':
            z.fillvalue
        elif action == 'write':
            z.fillvalue = 9
        else:
            del z.fillvalue
    except AttributeError as error:
        assert str(error) == "'itertools.zip_longest' object has no attribute 'fillvalue'"
        print('Z1.fill-' + action, type(error).__name__, str(error))
    else:
        raise AssertionError('fillvalue is not a public attribute')


class Source:
    def __init__(self, name, failure=None):
        self.name = name
        self.failure = failure

    def __iter__(self):
        events.append(('iter', self.name))
        if self.failure is not None:
            raise self.failure
        return self

    def __next__(self):
        events.append(('next', self.name))
        raise StopIteration('source value')

    def __len__(self):
        raise AssertionError('constructor must not ask source length')


events = []
z = constructor(Source('A'), Source('B'), Source('C'))
assert events == [('iter', 'A'), ('iter', 'B'), ('iter', 'C')]
print('Z1.construct-order', events)
assert list(z) == []
assert events == [('iter', 'A'), ('iter', 'B'), ('iter', 'C'),
                  ('next', 'A'), ('next', 'B'), ('next', 'C')]
print('Z1.empty-next-order', events)

for label, keywords in [('unknown', {'bad': 1}), ('strict', {'strict': True}),
                         ('fill-unknown', {'fillvalue': 7, 'bad': 1}),
                         ('two-unknown', {'a': 1, 'b': 2})]:
    events = []
    try:
        constructor(Source('not-visited', RuntimeError('iter')), **keywords)
    except TypeError as error:
        assert str(error) == 'zip_longest() got an unexpected keyword argument'
        assert events == []
        print('Z1.' + label, type(error).__name__, str(error), events)
    else:
        raise AssertionError('unknown keyword expected')

for label, marker in [('runtime', RuntimeError('iter-marker')),
                       ('stop', StopIteration('iter-stop-marker'))]:
    events = []
    try:
        constructor(Source('A'), Source('B', marker), Source('C'))
    except Exception as error:
        assert error is marker and events == [('iter', 'A'), ('iter', 'B')]
        print('Z1.iter-error-' + label, type(error).__name__, str(error),
              error is marker, events)
    else:
        raise AssertionError('original constructor error expected')


class InvalidIter:
    def __iter__(self):
        events.append(('iter', 'invalid'))
        return 7


for label, source, message, expected in [
    ('int', 7, "'int' object is not iterable", [('iter', 'A')]),
    ('none', None, "'NoneType' object is not iterable", [('iter', 'A')]),
    ('result', InvalidIter(), "iter() returned non-iterator of type 'int'",
     [('iter', 'A'), ('iter', 'invalid')]),
]:
    events = []
    try:
        constructor(Source('A'), source, Source('C'))
    except TypeError as error:
        assert str(error) == message and events == expected
        print('Z1.invalid-' + label, type(error).__name__, str(error), events)
    else:
        raise AssertionError('invalid iterator expected')
