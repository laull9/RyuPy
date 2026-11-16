# 来源失败保已写前缀、异常身份及cause/context；kwargs在位置源成功后才写。
from collections import OrderedDict as OD

cause = ValueError('source cause')
error = RuntimeError('source failed')
error.__cause__ = cause
d = OD(seed=0)
def pairs():
    print('yield_a')
    yield ('a', 1)
    print('after_a', list(d.items()))
    assert list(d.items()) == [('seed', 0), ('a', 1)]
    raise error
try:
    d.update(pairs(), keyword=9)
except RuntimeError as exc:
    print('pair_failure', exc is error, exc.__cause__ is cause, exc.__context__, list(d.items()))
assert 'keyword' not in d
mapping_error = LookupError('getitem failure')
class Mapping:
    def keys(self):
        return ['a', 'b', 'unreached']
    def __getitem__(self, key):
        print('getitem', key, list(d.items()))
        if key == 'b':
            d['callback'] = 3
            raise mapping_error
        if key == 'unreached':
            raise AssertionError('不得读取失败之后键')
        return 10
d = OD(seed=0)
try:
    d.update(Mapping(), keyword=9)
except LookupError as exc:
    print('mapping_failure', exc is mapping_error, exc.__cause__, exc.__context__, list(d.items()))
assert list(d.items()) == [('seed', 0), ('a', 10), ('callback', 3)]
class Unconsumed:
    def keys(self):
        raise AssertionError('绑定失败不得消费来源')
    def __iter__(self):
        raise AssertionError('绑定失败不得消费来源')
for label, fn in (
    ('construct_many', lambda: OD(Unconsumed(), [])),
    ('update_many', lambda: d.update(Unconsumed(), [])),
    ('init_many', lambda: d.__init__(Unconsumed(), [])),
):
    try:
        fn()
    except TypeError as exc:
        print('binding_first', label, str(exc), list(d.items()))
print('DONE update_failure_prefix')
