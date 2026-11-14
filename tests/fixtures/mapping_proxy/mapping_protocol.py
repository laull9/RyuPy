# 活跃源应用：首次固定CP/JIT差分已通过；完整门禁仍由父任务验收。
# 普通mapping回调及迭代身份；所有循环有限，非bool表达式比较另存pending。
from types import MappingProxyType as P
from collections import Counter

log = []
sentinel = object()
class M:
    def __getitem__(self, key):
        log.append(('getitem', key))
        return 41
    def __len__(self):
        log.append(('len',))
        return 1
    def __iter__(self):
        log.append(('iter',))
        return iter(['k'])
    def __contains__(self, key):
        log.append(('contains', key))
        return True
    def get(self, *args):
        log.append(('get', args))
        return sentinel
    def keys(self):
        log.append(('keys',))
        return sentinel
    def values(self):
        log.append(('values',))
        return sentinel
    def items(self):
        log.append(('items',))
        return sentinel
    def copy(self):
        log.append(('copy',))
        return self
    def __reversed__(self):
        log.append(('reverse',))
        return sentinel
    def __hash__(self):
        log.append(('hash',))
        return 123
    def __or__(self, other):
        log.append(('or', other is m))
        return sentinel
    def __ror__(self, other):
        log.append(('ror', other is m))
        return sentinel
    def __ior__(self, other):
        log.append(('ior',))
        return self
    def __eq__(self, other):
        log.append(('eq', other is q))
        return sentinel

m = M()
q = P(m)
assert log == []
assert q['k'] == 41 and len(q) == 1 and next(iter(q)) == 'k' and 'k' in q
assert log == [('getitem', 'k'), ('len',), ('iter',), ('contains', 'k')]
print('P3.read', log)
log.clear()
assert q.get('k') is sentinel and q.get('k', 7) is sentinel
assert q.keys() is sentinel and q.values() is sentinel and q.items() is sentinel
assert q.copy() is m and reversed(q) is sentinel
assert log == [('get', ('k', None)), ('get', ('k', 7)), ('keys',), ('values',), ('items',), ('copy',), ('reverse',)]
print('P3.methods', log)
old_get = M.get
M.get = lambda self, *args: ('new', args)
assert q.get('k') == ('new', ('k', None))
M.get = old_get
print('P3.rebind', True)
log.clear()
assert hash(q) == 123 and hash(q) == 123
assert log == [('hash',), ('hash',)]
print('P3.hash', log)
log.clear()
assert (q | {}) is sentinel and ({} | q) is sentinel and (q | q) is sentinel
assert log == [('or', False), ('ror', False), ('or', True)]
print('P3.union', log)
log.clear()
try:
    q |= {}
except TypeError as error:
    assert str(error) == "'|=' is not supported by mappingproxy; use '|' instead"
else:
    raise AssertionError('ior must fail')
assert log == []
assert q.__eq__(q) is sentinel and q.__ne__(q) is False
assert log == [('eq', True), ('eq', True)]
print('P3.compare', log)
class ExplicitNe(M):
    def __ne__(self, other):
        return sentinel
assert P(ExplicitNe()).__ne__(q) is sentinel
print('P3.explicit-ne', True)

class ReadFailure(Exception):
    pass
context = ValueError('inner-context')
def contextual_error(message):
    try:
        raise context
    except ValueError:
        try:
            raise ReadFailure(message)
        except ReadFailure as error:
            return error
read_error = contextual_error('read-error')
get_error = contextual_error('get-error')
class Failing:
    def __getitem__(self, key):
        raise read_error
    def get(self, key, default=None):
        raise get_error
failed = P(Failing())
for action, expected in [(lambda: failed['k'], read_error), (lambda: failed.get('k'), get_error)]:
    try:
        action()
    except ReadFailure as error:
        assert error is expected and error.__context__ is context
        assert error.__cause__ is None and not error.__suppress_context__
    else:
        raise AssertionError('callback must fail')
print('P3.errors', True)

class K:
    def keys(self):
        log.append(('keys',))
        return ['a', 'b']
    def __getitem__(self, key):
        log.append(('getitem', key))
        return {'a': 10, 'b': 20}[key]
    def __iter__(self):
        raise AssertionError('mapping copy must use keys')
k = P(K())
def update():
    target = {}
    target.update(k)
    return target
for action in [lambda: dict(k), update, lambda: (lambda **kw: kw)(**k)]:
    log.clear()
    assert action() == {'a': 10, 'b': 20}
    assert log == [('keys',), ('getitem', 'a'), ('getitem', 'b')]
print('P3.copy', log)

class Indexed:
    def __getitem__(self, index):
        log.append(('index', index))
        if index >= 3:
            raise IndexError('end')
        return ['a', 'b', 'c'][index]
indexed = P(Indexed())
log.clear()
assert list(iter(indexed)) == ['a', 'b', 'c']
assert log == [('index', 0), ('index', 1), ('index', 2), ('index', 3)]
print('P3.indexed', log)
log.clear()
assert 'a' in indexed and log == [('index', 0)]
print('P3.hit', log)
log.clear()
assert 'z' not in indexed
assert log == [('index', 0), ('index', 1), ('index', 2), ('index', 3)]
print('P3.miss', log)
class Disabled(Indexed):
    __iter__ = None
log.clear()
try:
    iter(P(Disabled()))
except TypeError:
    pass
else:
    raise AssertionError('explicit None must disable iteration')
assert log == []
print('P3.disabled', True)

class IdentityIterator:
    def __iter__(self):
        log.append(('unexpected-iter',))
        return self
    def __next__(self):
        raise StopIteration
identity_iterator = IdentityIterator()
class IterMapping:
    def __getitem__(self, key):
        raise KeyError(key)
    def __iter__(self):
        return identity_iterator
raw = P(IterMapping())
log.clear()
assert raw.__iter__() is identity_iterator and iter(raw) is identity_iterator
assert log == []
print('P3.raw', True)

# 以下日志由本应用首次固定CP/JIT差分验证；不是用早先84项参考替新时序背书。
class KeywordMapping:
    def __init__(self, keys):
        self.key_list = keys
    def keys(self):
        log.append(('keys',))
        return self.key_list
    def __getitem__(self, key):
        log.append(('getitem', key))
        return 4

def receive(**kw):
    return kw

# None必须在重复查找中保持同一键；重复错误先于非str最终检查，第二次不读值。
for label, keys in [('P3.kw-none-duplicate', [None, None]), ('P3.kw-string-duplicate', ['a', 'a'])]:
    log.clear()
    try:
        receive(**P(KeywordMapping(keys)))
    except TypeError as error:
        assert 'got multiple values for keyword argument' in str(error)
    else:
        raise AssertionError('duplicate keyword must fail')
    assert log == [('keys',), ('getitem', keys[0])]
    print(label, log)

log.clear()
try:
    receive(a=0, **P(KeywordMapping(['a'])))
except TypeError as error:
    assert "got multiple values for keyword argument 'a'" in str(error)
else:
    raise AssertionError('explicit keyword duplicate must fail')
assert log == [('keys',)]
print('P3.kw-before-read', log)

log.clear()
try:
    receive(**P(KeywordMapping([1, 'later'])))
except TypeError as error:
    assert str(error) == 'keywords must be strings'
else:
    raise AssertionError('non-string keyword must fail')
assert log == [('keys',), ('getitem', 1), ('getitem', 'later')]
print('P3.kw-after-merge', log)

class LaterReadFailure(Exception):
    pass
later_error = LaterReadFailure('later-read')
class FailingKeywordMapping(KeywordMapping):
    def __getitem__(self, key):
        log.append(('getitem', key))
        if key == 'later':
            raise later_error
        return 4
log.clear()
try:
    receive(**P(FailingKeywordMapping([1, 'later'])))
except LaterReadFailure as error:
    assert error is later_error
else:
    raise AssertionError('later read precedes final non-string check')
assert log == [('keys',), ('getitem', 1), ('getitem', 'later')]
print('P3.kw-read-error', log)

class CounterMapping:
    def keys(self):
        log.append(('keys',))
        return ['a']
    def __getitem__(self, key):
        log.append(('getitem', key))
        return 2
    def items(self):
        log.append(('items',))
        return [('a', 3)]
counter_source = P(CounterMapping())
counter = Counter()
log.clear()
counter.update(counter_source)
assert counter['a'] == 2 and log == [('keys',), ('getitem', 'a')]
print('P3.counter-empty', log)
log.clear()
counter.update(counter_source)
assert counter['a'] == 5 and log == [('items',)]
print('P3.counter-nonempty', log)
log.clear()
counter.subtract(counter_source)
assert counter['a'] == 2 and log == [('items',)]
print('P3.counter-subtract', log)
print('P3.complete', True)
