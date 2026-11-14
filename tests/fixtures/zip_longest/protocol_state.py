# ryupy: pending
# 按列立即冻结、永久注销、有限sequence/特殊槽与一次安全有值重入。
from itertools import zip_longest


def drain(iterator):
    rows = []
    for unused in range(8):
        try:
            rows.append(next(iterator))
        except StopIteration as error:
            assert error.args == ()
            return rows
    raise AssertionError('finite fixture exceeded next budget')


class Source:
    def __init__(self, name, actions):
        self.name = name
        self.actions = actions
        self.position = 0

    def __iter__(self):
        events.append(('iter', self.name))
        return self

    def __next__(self):
        index = self.position
        self.position += 1
        assert self.position <= 12
        events.append(('next', self.name, index))
        if index >= len(self.actions):
            raise StopIteration('source-done')
        kind, value = self.actions[index]
        if kind == 'stop':
            raise StopIteration(value)
        if kind == 'error':
            raise value
        return value

    def __len__(self):
        raise AssertionError('length must not be used')


def values(*items):
    return [('value', value) for value in items]


events = []
z = zip_longest(Source('A', values(1, 2)), Source('B', values(10)),
                Source('C', values(100, 200, 300)))
rows = drain(z)
assert rows == [(1, 10, 100), (2, None, 200), (None, None, 300)]
assert rows[0] is not rows[1] and rows[1] is not rows[2]
assert events == [('iter', 'A'), ('iter', 'B'), ('iter', 'C'),
                  ('next', 'A', 0), ('next', 'B', 0), ('next', 'C', 0),
                  ('next', 'A', 1), ('next', 'B', 1), ('next', 'C', 1),
                  ('next', 'A', 2), ('next', 'C', 2), ('next', 'C', 3)]
before = list(events)
assert next(z, 'END') == 'END' and next(z, 'END') == 'END' and events == before
print('Z2.order', rows, events)

for label, use_iter in [('direct', False), ('iter-result', True)]:
    events = []
    source = Source('shared', values(1, 2, 3))
    supplied = iter(source) if use_iter else source
    rows = drain(zip_longest(supplied, supplied, fillvalue='F'))
    assert rows == [(1, 2), (3, 'F')]
    expected = [('iter', 'shared')] * (3 if use_iter else 2)
    expected += [('next', 'shared', i) for i in range(5)]
    assert events == expected
    print('Z2.alias-' + label, rows, events)

events = []
source = Source('revive', [('stop', 'once'), ('value', 9), ('stop', 'twice')])
rows = drain(zip_longest(source, source, fillvalue='F'))
assert rows == [('F', 9)]
assert events == [('iter', 'revive'), ('iter', 'revive'),
                  ('next', 'revive', 0), ('next', 'revive', 1), ('next', 'revive', 2)]
print('Z2.slot-retirement', rows, events)

# 实例同名值不替代类型特殊槽。
events = []
source = Source('shadow', values(1, 2))
source.__iter__ = 7
source.__next__ = 8
assert drain(zip_longest(source)) == [(1,), (2,)]
assert events == [('iter', 'shadow')] + [('next', 'shadow', i) for i in range(3)]
print('Z2.special-lookup', events)


def changed_next(self):
    events.append(('changed-next', self.position))
    if self.position >= 2:
        raise StopIteration
    self.position += 1
    return 99


events = []
source = Source('rebind', values(1, 2))
z = zip_longest(source)
assert next(z) == (1,)
original_next = Source.__next__
try:
    Source.__next__ = changed_next
    assert next(z) == (99,) and next(z, 'END') == 'END'
    assert events == [('iter', 'rebind'), ('next', 'rebind', 0),
                      ('changed-next', 1), ('changed-next', 2)]
    print('Z2.slot-rebind', events)
finally:
    Source.__next__ = original_next


class Done(StopIteration):
    pass


events = []
source = Source('stop-subclass', [('error', Done('ignored-value')), ('value', 9)])
rows = drain(zip_longest(source, [1, 2], fillvalue='F'))
assert rows == [('F', 1), ('F', 2)] and source.position == 1
print('Z2.stop-subclass', rows, events)


# 原始失败例保留；同应用核真实用户异常ABI，不另开CP参考进程。
class StopPayload:
    def __init__(self, name):
        self.name = name

    def __str__(self):
        format_events.append(('str', self.name))
        return 'value:' + self.name

    def __repr__(self):
        format_events.append(('repr', self.name))
        return 'payload:' + self.name


class MoreDone(Done):
    pass


format_events = []
payload = StopPayload('P')
other = StopPayload('Q')
empty_stop = Done()
none_stop = Done(None)
one_stop = Done(payload)
many_stop = Done(payload, other)
child_stop = MoreDone(payload)
assert format_events == []
assert empty_stop.args == () and empty_stop.value is None
assert none_stop.args == (None,) and none_stop.value is None
assert one_stop.args[0] is payload and one_stop.value is payload
assert many_stop.args[0] is payload and many_stop.args[1] is other
assert many_stop.value is payload and len(many_stop.args) == 2
assert type(child_stop) is MoreDone and isinstance(child_stop, StopIteration)
assert child_stop.value is payload and child_stop.args[0] is payload
assert str(empty_stop) == '' and str(none_stop) == 'None'
assert format_events == []
assert str(one_stop) == 'value:P'
assert format_events == [('str', 'P')]
format_events = []
assert str(many_stop) == '(payload:P, payload:Q)'
assert format_events == [('repr', 'P'), ('repr', 'Q')]
payload.name = 'changed'
format_events = []
assert str(child_stop) == 'value:changed' and str(one_stop) == 'value:changed'
assert format_events == [('str', 'changed'), ('str', 'changed')]
print('Z2.stop-construction', empty_stop.args, none_stop.args,
      one_stop.value is payload, many_stop.value is payload, format_events)


class BadStopPayload:
    def __str__(self):
        format_events.append('bad-str')
        raise format_marker


format_events = []
format_marker = RuntimeError('format-marker')
bad_payload = BadStopPayload()
bad_stop = Done(bad_payload)
assert format_events == [] and bad_stop.value is bad_payload
try:
    str(bad_stop)
except RuntimeError as error:
    assert error is format_marker and format_events == ['bad-str']
    print('Z2.stop-format-error', type(error).__name__, str(error), error is format_marker,
          format_events)
else:
    raise AssertionError('explicit str must raise the original payload error')


class Sequence:
    def __init__(self, name, terminal):
        self.name = name
        self.terminal = terminal

    def __getitem__(self, index):
        assert 0 <= index < 8
        events.append(('getitem', self.name, index))
        if index < 2:
            return index + 10
        raise self.terminal

    def __len__(self):
        raise AssertionError('sequence length must not be used')


for label, terminal in [('index', IndexError('end')), ('stop', StopIteration('end')),
                         ('stop-subclass', Done('end'))]:
    events = []
    z = zip_longest(Sequence(label, terminal), [1, 2, 3], fillvalue='F')
    assert events == []
    rows = drain(z)
    assert rows == [(10, 1), (11, 2), ('F', 3)]
    assert events == [('getitem', label, index) for index in range(3)]
    print('Z2.sequence-' + label, rows, events)


class GetterFailure(Sequence):
    @property
    def __iter__(self):
        events.append(('iter-getter',))
        raise RuntimeError('lookup-marker')


events = []
z = zip_longest(GetterFailure('property', IndexError('end')))
assert events == [('iter-getter',)]
rows = drain(z)
assert rows == [(10,), (11,)]
assert events == [('iter-getter',)] + [('getitem', 'property', i) for i in range(3)]
print('Z2.getter-fallback', rows, events)


class PropertyIterator:
    def __init__(self):
        self.position = 0

    @property
    def __iter__(self):
        events.append('get-iter')
        return self.start

    def start(self):
        events.append('call-iter')
        return self

    @property
    def __next__(self):
        events.append('get-next')
        return self.advance

    def advance(self):
        self.position += 1
        events.append('call-next')
        if self.position > 1:
            raise StopIteration
        return 7


events = []
assert drain(zip_longest(PropertyIterator())) == [(7,)]
assert events == ['get-iter', 'call-iter', 'get-next', 'call-next', 'get-next', 'call-next']
print('Z2.property-slots', events)


class Reenter:
    def __init__(self):
        self.calls = 0
        self.entered = False
        self.nested = None

    def __iter__(self):
        events.append(('iter', 'R'))
        return self

    def __next__(self):
        self.calls += 1
        assert self.calls <= 5
        number = self.calls
        events.append(('next', 'R', number))
        if not self.entered:
            self.entered = True
            self.nested = next(z)
            events.append(('nested', self.nested))
            return 'outer'
        if number == 2:
            return 'inner'
        if number == 3:
            return 'after'
        raise StopIteration


events = []
source = Reenter()
z = zip_longest(source, Source('right', values(10, 20, 30)), fillvalue='F')
outer_row = next(z)
remaining = drain(z)
assert source.nested == ('inner', 10) and outer_row == ('outer', 20)
assert remaining == [('after', 30)] and outer_row is not source.nested
assert events == [('iter', 'R'), ('iter', 'right'), ('next', 'R', 1), ('next', 'R', 2),
                  ('next', 'right', 0), ('nested', ('inner', 10)), ('next', 'right', 1),
                  ('next', 'R', 3), ('next', 'right', 2), ('next', 'R', 4), ('next', 'right', 3)]
print('Z2.reentry', source.nested, outer_row, remaining, events)

left = [1]
z = zip_longest(left, [10, 20, 30], fillvalue='F')
first = next(z)
left.append(2)
second = next(z)
third = next(z)
left.append(3)
assert first == (1, 10) and second == (2, 20) and third == ('F', 30)
assert next(z, 'END') == 'END'
print('Z2.list-mutation', first, second, third)
