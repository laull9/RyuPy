# 每个pair只取三次判定恰好二项；不读len/hint，不消费失败后的pair。
from collections import OrderedDict as OD

class Pair:
    def __init__(self, count):
        self.count = count
    def __iter__(self):
        print('pair_iter', self.count)
        for value in ['x', 1, 2, 3][:self.count]:
            print('pair_next', value)
            yield value
    def __len__(self):
        raise AssertionError('不得读取pair长度')
    def __length_hint__(self):
        raise AssertionError('不得读取pair hint')
for count in (0, 1, 2, 3, 4):
    d = OD(seed=0)
    try:
        d.update([('prefix', 8), Pair(count)], tail=9)
    except Exception as exc:
        print('pair_error', count, type(exc).__name__, str(exc), exc.args)
    print('state', count, list(d.items()))
for pair in (1, (), ('x',), ('x', 1, 2), 'ab'):
    try:
        print('literal_pair', repr(pair), list(OD([pair]).items()))
    except Exception as exc:
        print('literal_error', repr(pair), type(exc).__name__, str(exc), exc.args)
error = TypeError('inner custom type error')
class InnerError:
    def __iter__(self):
        print('inner_iter')
        raise error
try:
    OD([InnerError()])
except TypeError as exc:
    print('inner_identity', exc is error, str(exc), exc.__cause__, exc.__context__)
def outer():
    print('outer_prefix')
    yield ('ok', 1)
    print('outer_bad')
    yield ('bad', 2, 3)
    raise AssertionError('不得读取失败之后来源')
d = OD()
try:
    d.update(outer(), late=5)
except ValueError as exc:
    print('outer_failure', str(exc), list(d.items()))
assert list(d.items()) == [('ok', 1)]
print('DONE pair_update_nplusone')
