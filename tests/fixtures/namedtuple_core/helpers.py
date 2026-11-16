# ryupy: pending
# 生成helper的真实绑定、完整迭代消费、返回类/元素身份及CP3.12错误语义。
from collections import namedtuple


def expect_error(label, action, expected_name, expected_message=None):
    try:
        action()
    except Exception as error:
        assert type(error).__name__ == expected_name
        if expected_message is not None:
            assert str(error) == expected_message
        print(label, type(error).__name__, str(error))
    else:
        raise AssertionError(label + ' did not raise')


shared = []
Pair = namedtuple('Pair', ['left', 'right'], defaults=[shared], module='fixture.helpers')
source = Pair(1)
saved_constructor = Pair.__new__
pair_defaults = saved_constructor.__defaults__
assert type(pair_defaults) is tuple and pair_defaults == (shared,)
assert pair_defaults[0] is shared
assert Pair.__new__.__defaults__ is pair_defaults
assert saved_constructor.__defaults__ is pair_defaults

# defaults的公开tuple在创建时固定；精确tuple输入保自身，派生tuple输入转plain。
exact_defaults = (shared,)
ExactDefault = namedtuple('ExactDefault', 'value', defaults=exact_defaults,
                          module='fixture.helpers')
assert ExactDefault.__new__.__defaults__ is exact_defaults
assert ExactDefault.__new__.__defaults__[0] is shared and ExactDefault().value is shared
NoDefault = namedtuple('NoDefault', 'value', module='fixture.helpers')
NoneDefault = namedtuple('NoneDefault', 'value', defaults=None, module='fixture.helpers')
assert NoDefault.__new__.__defaults__ is None and NoneDefault.__new__.__defaults__ is None
empty_defaults = ()
EmptyTupleDefault = namedtuple('EmptyTupleDefault', 'value', defaults=empty_defaults,
                               module='fixture.helpers')
EmptyListDefault = namedtuple('EmptyListDefault', 'value', defaults=[],
                              module='fixture.helpers')
assert EmptyTupleDefault.__new__.__defaults__ is empty_defaults
empty_list_defaults = EmptyListDefault.__new__.__defaults__
assert type(empty_list_defaults) is tuple and empty_list_defaults == ()
assert EmptyListDefault.__new__.__defaults__ is empty_list_defaults
DefaultValues = namedtuple('DefaultValues', 'value', module='fixture.helpers')
derived_defaults = DefaultValues(shared)
FromDerived = namedtuple('FromDerived', 'value', defaults=derived_defaults,
                         module='fixture.helpers')
plain_defaults = FromDerived.__new__.__defaults__
assert type(plain_defaults) is tuple and plain_defaults is not derived_defaults
assert plain_defaults[0] is shared and FromDerived().value is shared
assert FromDerived.__new__.__defaults__ is plain_defaults
from_list = Pair._make([1, shared])
from_tuple = Pair._make((1, shared))
from_generator = Pair._make(value for value in [1, shared])
saved_make = Pair._make
instance_make = source._make
from_saved = saved_make(iterable=[1, shared])
from_instance = instance_make([1, shared])
for result in [from_list, from_tuple, from_generator, from_saved, from_instance]:
    assert type(result) is Pair and result == source and result is not source
    assert result.right is shared
assert from_list is not from_tuple
print('B4.make-forms', type(from_list) is Pair, from_list is not from_tuple,
      from_instance.right is shared, from_saved.right is shared)


class Finite:
    def __init__(self, events, values, failure=None):
        self.events = events
        self.values = values
        self.failure = failure

    def __iter__(self):
        self.events.append('iter')
        for index, value in enumerate(self.values):
            self.events.append('yield:' + str(index))
            yield value
        if self.failure is not None:
            self.events.append('raise')
            raise self.failure
        self.events.append('end')


short_events = []
expect_error('B4.make-short', lambda: Pair._make(Finite(short_events, [1])), 'TypeError')
assert short_events == ['iter', 'yield:0', 'end']
long_events = []
expect_error('B4.make-long', lambda: Pair._make(Finite(long_events, [1, 2, 3, 4])),
             'TypeError')
assert long_events == ['iter', 'yield:0', 'yield:1', 'yield:2', 'yield:3', 'end']
print('B4.make-length-events', short_events, long_events)

# 超过所需数量后仍继续迭代；之后的用户异常必须胜过长度错误并保原对象。
failure_events = []
original = LookupError('iterator-after-extra')
try:
    Pair._make(Finite(failure_events, [1, 2, 3], original))
except LookupError as error:
    assert error is original
    print('B4.make-original-error', type(error).__name__, str(error), error is original)
else:
    raise AssertionError('_make discarded iterator error')
assert failure_events == ['iter', 'yield:0', 'yield:1', 'yield:2', 'raise']
print('B4.make-failure-events', failure_events)

expect_error('B4.make-missing', lambda: Pair._make(), 'TypeError')
expect_error('B4.make-extra', lambda: Pair._make([1, 2], 3), 'TypeError')
expect_error('B4.make-keyword', lambda: Pair._make(values=[1, 2]), 'TypeError')

same_values = source._replace()
assert same_values == source and same_values is not source and type(same_values) is Pair
assert same_values.right is shared
changed = source._replace(left=9)
assert type(changed) is Pair and changed == (9, shared) and changed is not source
assert changed.right is shared and source.left == 1
replacement = ['new']
changed_right = source._replace(right=replacement)
assert changed_right.right is replacement and source.right is shared
bound_replace = source._replace
unbound_replace = Pair._replace
assert bound_replace(left=7) == (7, shared)
assert unbound_replace(source, left=8) == (8, shared)
print('B4.replace', type(same_values) is Pair, same_values is not source,
      changed.right is shared, changed_right.right is replacement)
expect_error('B4.replace-unknown', lambda: source._replace(z=3, w=4), 'ValueError')
expect_error('B4.replace-positional', lambda: source._replace(9), 'TypeError')

# self是合法字段；方法接收者为位置专用，不能抢走待替换的self关键字。
SelfFields = namedtuple('SelfFields', 'self cls', module='fixture.helpers')
self_value = SelfFields(1, 2)
self_changed = self_value._replace(self=9, cls=8)
assert type(self_changed) is SelfFields and tuple(self_changed) == (9, 8)
assert tuple(self_value) == (1, 2)
print('B4.replace-self', self_changed)

first_dict = source._asdict()
second_dict = source._asdict()
bound_asdict = source._asdict
unbound_asdict = Pair._asdict
assert type(first_dict) is dict and type(second_dict) is dict
assert first_dict is not second_dict and list(first_dict) == ['left', 'right']
assert first_dict['right'] is shared and second_dict['right'] is shared
assert bound_asdict()['right'] is shared
assert unbound_asdict(source)['right'] is shared
first_dict['left'] = 99
assert source.left == 1 and second_dict['left'] == 1
print('B4.asdict', list(second_dict), first_dict is not second_dict,
      second_dict['right'] is shared, source.left)
expect_error('B4.asdict-extra', lambda: source._asdict(1), 'TypeError')
expect_error('B4.asdict-keyword', lambda: source._asdict(extra=1), 'TypeError')

args = source.__getnewargs__()
saved_getnewargs = source.__getnewargs__
unbound_getnewargs = Pair.__getnewargs__
assert type(args) is tuple and args is not source and args == (1, shared)
assert args[1] is shared and saved_getnewargs()[1] is shared
assert unbound_getnewargs(source)[1] is shared
print('B4.getnewargs', type(args) is tuple, args is not source, args[1] is shared)
expect_error('B4.getnewargs-extra', lambda: source.__getnewargs__(1), 'TypeError')

Empty = namedtuple('Empty', '', module='fixture.helpers')
empty = Empty()
made_empty = Empty._make([])
replaced_empty = empty._replace()
assert type(made_empty) is Empty and made_empty is not empty
assert type(replaced_empty) is Empty and replaced_empty is not empty
assert type(empty._asdict()) is dict and empty._asdict() == {}
assert type(empty.__getnewargs__()) is tuple and empty.__getnewargs__() == ()
print('B4.empty', made_empty is not empty, replaced_empty is not empty,
      empty._asdict(), empty.__getnewargs__())

# 未绑定helper保定义处闭包，又使用实际receiver的类型/字段/_make；不能强制原class。
Peer = namedtuple('Peer', 'alpha beta', module='fixture.helpers')
Short = namedtuple('Short', 'alpha', module='fixture.helpers')
Long = namedtuple('Long', 'alpha beta gamma', defaults=[None], module='fixture.helpers')
peer = Peer(1, shared)
short = Short(shared)
long = Long(1, shared, 3)
peer_changed = Pair._replace(peer, left=9)
assert type(peer_changed) is Peer and tuple(peer_changed) == (9, shared)
assert peer_changed is not peer and peer_changed.beta is shared
assert peer.alpha == 1 and peer.beta is shared
peer_dict = Pair._asdict(peer)
assert type(peer_dict) is dict and list(peer_dict) == ['alpha', 'beta']
assert peer_dict['beta'] is shared
peer_args = Pair.__getnewargs__(peer)
assert type(peer_args) is tuple and peer_args == (1, shared) and peer_args[1] is shared
print('B4.cross-equal', type(peer_changed) is Peer, peer_changed.beta is shared,
      list(peer_dict), peer_args[1] is shared)
expect_error('B4.cross-unknown', lambda: Pair._replace(peer, left=9, alpha=5, extra=6),
             'ValueError', "Got unexpected field names: ['alpha', 'extra']")

# map在实际receiver较短时截断：未走到的闭包字段不会消费对应关键字。
short_changed = Pair._replace(short, left=7)
assert type(short_changed) is Short and tuple(short_changed) == (7,)
short_same = Pair._replace(short)
assert type(short_same) is Short and short_same is not short and short_same.alpha is shared
print('B4.cross-short', short_changed, short_same is not short,
      short_same.alpha is shared)
expect_error('B4.cross-short-unused', lambda: Pair._replace(short, right=9),
             'ValueError', "Got unexpected field names: ['right']")

# receiver较长时实际Long._make拒绝截短结果；defaults不能补齐，未知kw检查也未到达。
expect_error('B4.cross-long', lambda: Pair._replace(long, left=9),
             'TypeError', 'Expected 3 arguments, got 2')
expect_error('B4.cross-long-before-unknown', lambda: Pair._replace(long, extra=9),
             'TypeError', 'Expected 3 arguments, got 2')
assert list(Pair._asdict(short)) == ['alpha']
assert Pair._asdict(short)['alpha'] is shared
assert list(Pair._asdict(long)) == ['alpha', 'beta', 'gamma']
assert Pair._asdict(long)['beta'] is shared
assert Pair.__getnewargs__(short) == (shared,)
assert type(Pair.__getnewargs__(long)) is tuple
assert Pair.__getnewargs__(long) == (1, shared, 3)
assert Pair.__getnewargs__(long)[1] is shared
print('B4.cross-content', list(Pair._asdict(short)), list(Pair._asdict(long)),
      Pair.__getnewargs__(long)[1] is shared)

# repr保定义处标签/数量，但类型名来自实际receiver。异长按普通percent错误处理。
assert Pair.__repr__(peer) == 'Peer(left=1, right=[])'
assert repr(peer) == 'Peer(alpha=1, beta=[])'
print('B4.cross-repr', Pair.__repr__(peer), repr(peer))
expect_error('B4.cross-repr-short', lambda: Pair.__repr__(short), 'TypeError',
             'not enough arguments for format string')
expect_error('B4.cross-repr-long', lambda: Pair.__repr__(long), 'TypeError',
             'not all arguments converted during string formatting')
