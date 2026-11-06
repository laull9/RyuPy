# ryupy: pending
# CP 合法的非类型、不可哈希参数、前向引用及回调域完整保留，不将 C1 白名单当语言拒绝规则。
import typing as t
CV = t.ClassVar
first = CV[1]
assert CV[True] is first and CV[1.0] is first
assert first.__args__ == (1,)
print('equal-scalar-raw-cache', repr(first))
for value in [42, 2.5, ..., [int], {'x': str}]:
    alias = CV[value]
    assert alias.__args__[0] is value
    print('legal-non-type', repr(alias))
items = [int]
left = CV[items]
right = CV[items]
assert left is not right and left.__args__[0] is items
print('unhashable-not-cached', repr(left), repr(right))

events = []
class Key:
    def __repr__(self):
        return 'Key()'
    def __hash__(self):
        events.append('hash')
        return 7

key = Key()
old = CV[key]
assert CV[key] is old
assert events == ['hash', 'hash']
print('user-hash-even-warm', events, repr(old))

alias = CV['Thing']
forward = alias.__args__[0]
assert alias is CV['Thing']
assert type(forward).__module__ == 'typing'
assert type(forward).__name__ == 'ForwardRef'
assert forward.__forward_arg__ == 'Thing'
assert forward.__forward_is_argument__ is True
assert forward.__forward_is_class__ is False
print('forward', repr(alias), forward.__forward_is_argument__, forward.__forward_is_class__)
try:
    CV['!']
except SyntaxError as error:
    print('invalid-forward', str(error))
else:
    assert False
