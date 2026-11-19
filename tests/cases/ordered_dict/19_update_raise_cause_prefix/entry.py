# 使用raise from建立异常链；与原13独立，不绕过或改写原13的setter债记录。
from collections import OrderedDict as OD

pair_cause = ValueError('source cause')
pair_error = RuntimeError('source failed')
d = OD(seed=0)
alias = d

def pairs():
    print('yield_a')
    yield ('a', 1)
    print('after_a', list(d.items()))
    assert list(d.items()) == [('seed', 0), ('a', 1)]
    raise pair_error from pair_cause

try:
    d.update(pairs(), keyword=9)
except RuntimeError as exc:
    print('pair_failure', exc is pair_error, exc.__cause__ is pair_cause, exc.__context__ is None, list(d.items()), alias is d)
    assert exc is pair_error and exc.__cause__ is pair_cause and exc.__context__ is None
else:
    raise AssertionError('generator异常不得吞掉')
assert list(d.items()) == [('seed', 0), ('a', 1)]
assert 'keyword' not in d

mapping_cause = KeyError('mapping cause')
mapping_context = ValueError('mapping context')
mapping_error = LookupError('getitem failure')
events = []
class Mapping:
    def keys(self):
        events.append('keys')
        return ['a', 'b', 'unreached']
    def __getitem__(self, key):
        events.append(key)
        print('getitem', key, list(d.items()))
        if key == 'b':
            assert d['a'] == 10
            d['callback'] = 3
            try:
                raise mapping_context
            except ValueError:
                raise mapping_error from mapping_cause
        if key == 'unreached':
            raise AssertionError('不得读取失败之后键')
        return 10

d = OD(seed=0)
alias = d
saved_update = d.update
try:
    saved_update(Mapping(), keyword=9)
except LookupError as exc:
    print('mapping_failure', exc is mapping_error, exc.__cause__ is mapping_cause, exc.__context__ is mapping_context, list(d.items()), alias is d)
    assert exc is mapping_error and exc.__cause__ is mapping_cause and exc.__context__ is mapping_context
else:
    raise AssertionError('mapping异常不得吞掉')
print('mapping_events', events)
assert events == ['keys', 'a', 'b']
assert list(d.items()) == [('seed', 0), ('a', 10), ('callback', 3)]
assert 'keyword' not in d

consumption = []
class Unconsumed:
    def keys(self):
        consumption.append('keys')
        raise AssertionError('绑定失败不得消费来源')
    def __iter__(self):
        consumption.append('iter')
        raise AssertionError('绑定失败不得消费来源')
for label, fn in (
    ('construct_many', lambda: OD(Unconsumed(), [])),
    ('update_many', lambda: d.update(Unconsumed(), [])),
    ('init_many', lambda: d.__init__(Unconsumed(), [])),
):
    try:
        fn()
    except TypeError as exc:
        print('binding_first', label, str(exc), list(d.items()), consumption)
    else:
        raise AssertionError('多余位置参数必须拒绝')
    assert consumption == []
    assert list(d.items()) == [('seed', 0), ('a', 10), ('callback', 3)]
print('DONE update_raise_cause_prefix')
