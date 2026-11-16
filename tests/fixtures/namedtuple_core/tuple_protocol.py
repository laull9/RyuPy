# ryupy: pending
# namedtuple与普通tuple双向操作、键身份、代表性消费者及方法读取时序。
# 不测试性能，不扩大普通tuple尚未支持的自定义__index__协议。
from collections import namedtuple
from collections.abc import Collection, Container, Iterable, Sequence, Sized
import json


def expect_error(label, action, expected_name):
    try:
        action()
    except Exception as error:
        assert type(error).__name__ == expected_name
        print(label, type(error).__name__, str(error))
    else:
        raise AssertionError(label + ' did not raise')


Quad = namedtuple('Quad', 'a b c d', module='fixture.tuple')
Other = namedtuple('Other', 'w x y z', module='fixture.tuple')
Empty = namedtuple('Empty', '', module='fixture.tuple')
value = Quad(1, 2, 1, 3)
same = Other(1, 2, 1, 3)
plain = (1, 2, 1, 3)
assert len(value) == 4 and bool(value) and not bool(Empty())
assert value[0] == value[-4] == value[False] == 1
assert value[-1] == 3 and value[True] == 2
assert value[1:3] == (2, 1) and value[::-1] == (3, 1, 2, 1)
assert value[::2] == (1, 1) and value[9:] == ()
assert list(value) == [1, 2, 1, 3]
assert [element for element in value] == [1, 2, 1, 3]
first, second, third, fourth = value
head, *middle, tail = value
assert (first, second, third, fourth) == plain
assert head == 1 and middle == [2, 1] and tail == 3
assert 2 in value and 9 not in value
assert value.count(1) == 2 and value.index(1) == 0
print('B3.sequence', len(value), value[True], value[::-1], middle,
      value.count(1), value.index(1))
expect_error('B3.out-of-range', lambda: value[4], 'IndexError')
expect_error('B3.index-missing', lambda: value.index(9), 'ValueError')

assert value == plain and plain == value and value == same and same == value
assert not (value != plain) and not (plain != value)
assert value < (1, 2, 2, 0) and (1, 2, 0, 9) < value
assert value <= same and same >= value
assert hash(value) == hash(plain) == hash(same)
assert value != [1, 2, 1, 3]
assert sorted([Quad(3, 0, 0, 0), plain, Other(2, 0, 0, 0)]) == [
    plain, Other(2, 0, 0, 0), Quad(3, 0, 0, 0)]
print('B3.comparison', value == plain, plain == value, value == same,
      hash(value) == hash(plain), value < (1, 2, 2, 0))

# 等值覆盖必须保留最初的键对象，不按record类标签分割hash/equality。
record_first = {value: 'record'}
assert record_first[plain] == 'record' and record_first[same] == 'record'
record_first[plain] = 'updated'
assert len(record_first) == 1 and next(iter(record_first)) is value
plain_first = {plain: 'plain'}
assert plain_first[value] == 'plain'
plain_first[same] = 'updated'
assert len(plain_first) == 1 and next(iter(plain_first)) is plain
assert plain in {value} and value in {plain} and same in {value}
assert len({value, plain, same}) == 1
nested = {(value,): 'nested'}
assert nested[(plain,)] == 'nested' and nested[(same,)] == 'nested'
print('B3.keys', next(iter(record_first)) is value,
      next(iter(plain_first)) is plain, len({value, plain, same}), nested[(plain,)])
expect_error('B3.unhashable', lambda: hash(Quad([], 2, 1, 3)), 'TypeError')

Pair = namedtuple('Pair', 'left right', module='fixture.tuple')
shared = []
record = Pair(shared, 'right')
results = [tuple(record), record[:], record[::1], record + (), () + record,
           record * 1, 1 * record, record * 2, 2 * record]
for result in results:
    assert type(result) is tuple and result is not record
    assert result[0] is shared
assert type(record * 0) is tuple and record * 0 == ()
assert type(record * -1) is tuple and record * -1 == ()
assert record[1:] == ('right',) and type(record[1:]) is tuple
assert record[::-1][1] is shared
shared.append('visible')
assert all(result[0] == ['visible'] for result in results)
assert (3,) + value == (3, 1, 2, 1, 3)
assert value + (4,) == (1, 2, 1, 3, 4)
print('B3.plain-results', len(results), all(type(result) is tuple for result in results),
      all(result[0] is shared for result in results), tuple(record))

# 既有tuple消费端挑选代表性真实入口，不以is PyTuple源码推断全部已验证。
options = Pair(int, str)
assert isinstance(7, options) and isinstance('text', options)
assert issubclass(bool, options) and not isinstance([], options)
for protocol in (Sequence, Collection, Iterable, Sized, Container):
    assert isinstance(value, protocol) and issubclass(Quad, protocol)
assert Pair('key', 7) in {'key': 7}.items()
assert '%s:%s' % Pair('left', 'right') == 'left:right'
assert json.dumps(Pair(1, 2)) == '[1, 2]'
assert format(value, '') == repr(value)
print('B3.consumers', '%s:%s' % Pair('left', 'right'), json.dumps(Pair(1, 2)),
      format(value, ''), Pair('key', 7) in {'key': 7}.items())
expect_error('B3.format-nonempty', lambda: format(value, 'x'), 'TypeError')

calls = []


def plus_ten(number):
    calls.append('ten:' + str(number))
    return number + 10


def plus_twenty(number):
    calls.append('twenty:' + str(number))
    return number + 20


def call_count(receiver, argument):
    return receiver.count(argument)


def call_index(receiver, argument):
    return receiver.index(argument)


CountFirst = namedtuple('CountFirst', 'count marker', module='fixture.tuple')
CountLast = namedtuple('CountLast', 'marker count', module='fixture.tuple')
IndexFirst = namedtuple('IndexFirst', 'index marker', module='fixture.tuple')
IndexLast = namedtuple('IndexLast', 'marker index', module='fixture.tuple')
first_counter = CountFirst(plus_ten, 0)
last_counter = CountLast(0, plus_twenty)
saved = first_counter.count
assert saved is plus_ten and saved(2) == 12
assert first_counter.count(3) == 13
count_results = [call_count(item, 1) for item in
                 [first_counter, Pair(1, 2), last_counter, first_counter]]
index_results = [call_index(item, 1) for item in
                 [IndexLast(0, plus_twenty), Pair(1, 2),
                  IndexFirst(plus_ten, 0), IndexLast(0, plus_twenty)]]
assert count_results == [11, 1, 21, 11]
assert index_results == [21, 0, 11, 21]
assert calls == ['ten:2', 'ten:3', 'ten:1', 'twenty:1', 'ten:1',
                 'twenty:1', 'ten:1', 'twenty:1']
print('B3.same-callsite', count_results, index_results, calls)

events = []


def argument():
    events.append('argument')
    return 1


def missing(receiver):
    return receiver.missing(argument())


def noncallable(receiver):
    return receiver.count(argument())


expect_error('B3.missing-before-argument', lambda: missing(first_counter), 'AttributeError')
assert events == []
expect_error('B3.noncallable-after-argument', lambda: noncallable(CountFirst(7, 0)),
             'TypeError')
assert events == ['argument']
print('B3.argument-events', events)

# yield实参暂停前就冻结真实字段callable；恢复时重绑全局receiver不能偷换目标。
selected = first_counter


def prepared():
    result = selected.count((yield 'ready'))
    yield result


iterator = prepared()
assert next(iterator) == 'ready'
selected = last_counter
assert iterator.send(5) == 15
assert next(iterator, 'finished') == 'finished'
assert calls[-1] == 'ten:5'
assert selected is last_counter
print('B3.prepared', calls[-1], selected is last_counter)
