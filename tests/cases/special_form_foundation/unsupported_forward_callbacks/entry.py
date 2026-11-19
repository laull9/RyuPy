# CP合法字符串、标量与自定义哈希回调：保留真结果而不伪造TypeError。
import typing as t
a = t.Optional['Thing']
f = a.__args__[0]
assert f.__forward_arg__ == 'Thing'
assert f.__forward_is_argument__ and not f.__forward_is_class__
assert a is t.Optional['Thing']
print('forward', repr(a))
log = []
class Key:
    def __hash__(self):
        log.append('hash')
        return 701
    def __eq__(self, other):
        return self is other
    def __repr__(self):
        return 'Key()'
k = Key()
a = t.Optional[k]
log.clear()
assert t.Optional[k] is a
assert log == ['hash']
print('callback-hit', log)
assert t.Optional[42].__args__[0] == 42
print('legal-number', repr(t.Optional[42]))
