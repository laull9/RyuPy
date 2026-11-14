# 原始hash/eq在缓存命中前仍消费；TypeError触发未缓存重跑，其他异常原样传播。
import typing as t
log = []
class Key:
    def __init__(self, token):
        self.token = token
    def __hash__(self):
        log.append('hash:' + self.token)
        return 701
    def __eq__(self, other):
        log.append('eq:' + self.token)
        return isinstance(other, Key) and self.token == other.token
    def __repr__(self):
        return 'Key(' + self.token + ')'
k = Key('a')
a = t.List[k]
log.clear()
assert t.List[k] is a
print('same-hit', log)
log.clear()
assert t.List[Key('a')] is a
print('equal-hit', log)
class Unhashable:
    def __hash__(self):
        log.append('type-error-hash')
        raise TypeError('unhashable marker')
    def __repr__(self):
        return 'Unhashable()'
u = Unhashable()
log.clear()
x = t.List[u]
y = t.List[u]
assert x is not y and x == y
print('hash-fallback', log, repr(x))
class Explosive:
    def __hash__(self):
        log.append('value-error-hash')
        raise ValueError('hash marker')
log.clear()
try:
    t.List[Explosive()]
except ValueError as error:
    print('hash-propagation', str(error), log)
else:
    assert False
