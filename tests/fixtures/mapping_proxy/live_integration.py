# 活跃源应用：首次固定CP/JIT差分已通过；完整门禁仍由父任务验收。
# 活视图、原生dict派生和消费者；固定CP先验证全部断言，不能只比两端空输出。
from types import MappingProxyType as P
from collections import defaultdict, Counter
from collections.abc import Mapping, MutableMapping
import json


def rejected(action, error_type):
    try:
        action()
    except error_type as error:
        return str(error)
    raise AssertionError('missing expected exception')


def assign(proxy):
    proxy['a'] = 9


def delete(proxy):
    del proxy['a']


def attribute_assign(proxy):
    proxy.extra = 1


def attribute_delete(proxy):
    del proxy.extra


def inplace(proxy):
    proxy |= {'b': 8}


d = {'a': [1]}
p = P(d)
keys, values, items = p.keys(), p.values(), p.items()
assert list(keys) == ['a']
copy = p.copy()
assert type(copy) is dict and copy is not d and copy['a'] is d['a']
d['b'] = 2
d['a'].append(3)
assert list(keys) == ['a', 'b']
assert list(values) == [[1, 3], 2]
assert list(items) == [('a', [1, 3]), ('b', 2)]
assert 'b' not in copy and copy['a'] == [1, 3]
assert len(p) == 2 and bool(p) and 'b' in p and p['b'] == 2
assert not bool(P({}))
del d['b']
assert list(keys) == ['a'] and 'b' not in p
nested = P(p)
assert type(nested) is P and nested['a'] is d['a']
assert repr(nested) == "mappingproxy(mappingproxy({'a': [1, 3]}))"
assert str(p) == str(d) and repr(p) == "mappingproxy({'a': [1, 3]})"
print('P2.live', True)

assert rejected(lambda: assign(p), TypeError) == "'mappingproxy' object does not support item assignment"
assert rejected(lambda: delete(p), TypeError) == "'mappingproxy' object does not support item deletion"
rejected(lambda: attribute_assign(p), AttributeError)
rejected(lambda: attribute_delete(p), AttributeError)
for name in ['update', 'clear', 'pop', 'popitem', 'setdefault']:
    assert rejected(lambda: getattr(p, name), AttributeError) == "'mappingproxy' object has no attribute '" + name + "'"
assert rejected(lambda: inplace(p), TypeError) == "'|=' is not supported by mappingproxy; use '|' instead"
assert rejected(lambda: hash(p), TypeError) == "unhashable type: 'dict'"
assert rejected(lambda: json.dumps(p), TypeError) == 'Object of type mappingproxy is not JSON serializable'
assert isinstance(p, Mapping) and not isinstance(p, MutableMapping) and not isinstance(p, dict)
print('P2.readonly', True)

d['b'] = 2
a, b = iter(p), iter(p)
assert next(a) == 'a' and next(b) == 'a'
d['b'] = 22
assert next(a) == 'b' and next(b) == 'b'
assert next(a, 'end') == 'end' and next(a, 'again') == 'again'
a = iter(p)
d['c'] = 3
assert rejected(lambda: next(a), RuntimeError) == 'dictionary changed size during iteration'
a = reversed(p)
assert next(a) == 'c'
d['b'] = 23
assert next(a) == 'b'
d['z'] = 4
assert rejected(lambda: next(a), RuntimeError) == 'dictionary changed size during iteration'
print('P2.iteration', True)

assert dict(p) == d
out = {'old': 0}
out.update(p)
assert out == {'old': 0, 'a': [1, 3], 'b': 23, 'c': 3, 'z': 4}

def receive(**kw):
    return kw
assert receive(**p) == d
assert rejected(lambda: receive(**P({1: 'x'})), TypeError) == 'keywords must be strings'
# 只锁错误实质；函数限定名/诊断边界仍由完整stdout差分之外的专门帧合同管理。
message = rejected(lambda: receive(a=0, **P({'a': 1})), TypeError)
assert "got multiple values for keyword argument 'a'" in message
print('P2.consumers', True)

factories = []
def factory():
    factories.append('factory')
    return 8
dd = defaultdict(factory)
proxy_dd = P(dd)
assert proxy_dd.get('x') is None and factories == []
assert proxy_dd['x'] == 8 and factories == ['factory']
assert proxy_dd['x'] == 8 and factories == ['factory']
counter = Counter({'a': 2})
proxy_counter = P(counter)
assert proxy_counter['absent'] == 0 and 'absent' not in counter
assert proxy_counter.get('absent') is None
# 来源消费者必须通过代理keys/getitem；不能因实现IPyNativeMapping偷取可写存储。
updated = defaultdict(factory)
updated.update(P({'a': 5}))
assert updated['a'] == 5 and factories == ['factory']
counts = Counter({'a': 2})
counts.update(P({'a': 3, 'b': 1}))
assert counts == Counter({'a': 5, 'b': 1})
print('P2.native', True)

shared = []
left = {'a': shared, 'b': 1}
right = {'b': 9, 'c': 3}
proxy = P(left)
merged = proxy | right
assert merged is not left and merged is not right
assert list(merged) == ['a', 'b', 'c'] and merged['b'] == 9
assert merged['a'] is shared
assert list(right | proxy) == ['b', 'c', 'a']
assert (right | proxy)['b'] == 1
assert proxy | P(right) == merged
assert left | right == merged
assert proxy == left and left == proxy and proxy == P(left)
assert not (proxy != P(left))
assert left == {'a': [], 'b': 1} and right == {'b': 9, 'c': 3}
print('P2.union', True)
print('P2.complete', True)
