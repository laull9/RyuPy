# ryupy: pending
# 构造与推进错误原对象/时点，吞终止异常后ambient恢复，最后保完整用户错误链。
from itertools import zip_longest


class Source:
    def __init__(self, name, first, second):
        self.name = name
        self.first = first
        self.second = second
        self.position = 0

    def __iter__(self):
        events.append(('iter', self.name))
        return self

    def __next__(self):
        index = self.position
        self.position += 1
        events.append(('next', self.name, index))
        if index == 0:
            if self.first is marker:
                raise self.first
            return self.first
        if index == 1:
            return self.second
        raise StopIteration


for fail_at in [0, 1, 2]:
    marker = LookupError('next-marker-' + str(fail_at))
    events = []
    inputs = [Source('A', 1, 2), Source('B', 10, 20), Source('C', 100, 200)]
    inputs[fail_at].first = marker
    z = zip_longest(*inputs)
    try:
        next(z)
    except LookupError as error:
        assert error is marker
        expected = [('iter', 'A'), ('iter', 'B'), ('iter', 'C')]
        expected += [('next', name, 0) for name in ['A', 'B', 'C'][:fail_at + 1]]
        assert events == expected
        before = list(events)
        assert next(z, 'END') == 'END' and next(z, 'END') == 'END'
        assert events == before
        print('Z3.next-error-' + str(fail_at), type(error).__name__, str(error),
              error is marker, events, [item.position for item in inputs])
    else:
        raise AssertionError('next error expected')
    assert next(inputs[fail_at]) == [2, 20, 200][fail_at]


class Sequence:
    def __getitem__(self, index):
        events.append(('getitem', index))
        if index < 2:
            return index + 10
        raise marker


events = []
marker = LookupError('sequence-marker')
z = zip_longest(Sequence(), [1, 2, 3])
assert next(z) == (10, 1) and next(z) == (11, 2)
try:
    next(z)
except LookupError as error:
    assert error is marker and events == [('getitem', 0), ('getitem', 1), ('getitem', 2)]
    before = list(events)
    assert next(z, 'END') == 'END' and events == before
    print('Z3.sequence-error', type(error).__name__, str(error), error is marker, events)
else:
    raise AssertionError('sequence failure expected')


class DisabledSequence(Sequence):
    __iter__ = None


class GetterNone(Sequence):
    @property
    def __iter__(self):
        events.append('iter-getter')
        return None


class MethodFailure(Sequence):
    def __iter__(self):
        events.append('iter-body')
        raise marker


for label, source, name, message, expected in [
    ('iter-none', DisabledSequence(), 'TypeError', "'DisabledSequence' object is not iterable", []),
    ('property-none', GetterNone(), 'TypeError', "'GetterNone' object is not iterable", ['iter-getter']),
    ('method-error', MethodFailure(), 'RuntimeError', 'body-marker', ['iter-body']),
]:
    events = []
    marker = RuntimeError('body-marker')
    try:
        zip_longest(source)
    except Exception as error:
        assert type(error).__name__ == name and str(error) == message
        assert events == expected
        if label == 'method-error':
            assert error is marker
        print('Z3.' + label, type(error).__name__, str(error), error is marker, events)
    else:
        raise AssertionError('non-fallback error expected')


class StopFirst:
    def __iter__(self):
        return self

    def __next__(self):
        events.append('stop')
        raise StopIteration('swallowed')


class EmptySequence:
    def __getitem__(self, index):
        events.append('index-end')
        raise IndexError('swallowed')


class FailNext:
    def __iter__(self):
        return self

    def __next__(self):
        events.append('fail')
        raise marker


# 模块级active context必须胜过被内部吞掉的Stop/IndexError。
for label, first, first_event in [('stop', StopFirst(), 'stop'),
                                 ('index', EmptySequence(), 'index-end')]:
    active = ValueError('active-context')
    marker = LookupError('after-swallowed')
    events = []
    z = zip_longest(first, FailNext())
    try:
        raise active
    except ValueError:
        try:
            next(z)
        except LookupError as error:
            assert error is marker and error.__context__ is active
            assert error.__cause__ is None and not error.__suppress_context__
            assert events == [first_event, 'fail']
            before = list(events)
            assert next(z, 'END') == 'END' and events == before
            print('Z3.context-' + label, error is marker, error.__context__ is active,
                  type(error).__name__, str(error), events)
        else:
            raise AssertionError('context error expected')


# 两段未捕获链分别经过真实构造__iter__正文与真实推进__next__正文。
class ConstructorFailure:
    def __iter__(self):
        print('Z3.chain-constructor', 'iter')
        raise ValueError('constructor context')


def construct_middle():
    return zip_longest(ConstructorFailure())


def construct_outer():
    return construct_middle()


class BodyFailure:
    def __iter__(self):
        return self

    def __next__(self):
        print('Z3.chain-body', 'next')
        raise LookupError('next chain failed')


def next_middle():
    value = zip_longest(BodyFailure())
    return next(value)


def next_outer():
    return next_middle()


try:
    construct_outer()
except ValueError:
    next_outer()
