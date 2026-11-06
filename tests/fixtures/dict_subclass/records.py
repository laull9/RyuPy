
# 业务记录表：生成器输入、共享对象、重复初始化和失败前缀必须保留公开身份。
class Record(dict):
    pass

class AuditRecord(Record):
    def total(self):
        return self['price'] * self['count']

shared = ['original']
r = AuditRecord((pair for pair in [('price', 7), ('count', 3), ('tags', shared)]), state='new')
alias = r
r.owner = 'warehouse'
keys = r.keys()
assert type(r) is AuditRecord and isinstance(r, (Record, dict))
assert issubclass(AuditRecord, dict) and not isinstance({}, Record)
assert r.__class__ is AuditRecord and AuditRecord.__base__ is Record
assert r.total() == 21 and r['tags'] is shared
assert len(r) == 4 and bool(r) and 'state' in r
print('dict-subclass.records', list(r), r.total(), r.owner)

assert dict.__init__(r, [('count', 4)], state='ready') is None
assert dict.__init__(r) is None
assert dict.__init__(r, r) is None
assert r is alias and r.owner == 'warehouse' and r['tags'] is shared
assert list(keys) == ['price', 'count', 'tags', 'state'] and r.total() == 28
print('dict-subclass.reinit', list(r.items()), r is alias)

log = []
def broken():
    log.append('first')
    yield ('committed', 8)
    log.append('fail')
    raise ValueError('source stopped')
try:
    r.update(broken(), not_written=True)
except ValueError as error:
    assert str(error) == 'source stopped'
else:
    raise AssertionError('source failure swallowed')
assert r['committed'] == 8 and 'not_written' not in r and log == ['first', 'fail']
try:
    r.update(broken(), broken())
except TypeError:
    pass
else:
    raise AssertionError('arity was accepted')
assert log == ['first', 'fail']
print('dict-subclass.prefix', list(r), log)

for copy in (r.copy(), dict.copy(r), dict(r), r | {'extra': 9}, {'extra': 9} | r):
    assert type(copy) is dict and copy is not r and copy['tags'] is shared
    assert not hasattr(copy, 'owner')
r |= [('count', 5)]
assert r is alias and r.total() == 35
assert dict.__ior__(r, {'count': 6}) is r
assert r.setdefault('count', 99) == 6 and r.setdefault('default', 11) == 11
assert r.pop('default') == 11 and r.pop('absent', 12) == 12
assert r.popitem() == ('committed', 8)
del r['state']
assert list(reversed(r)) == ['tags', 'count', 'price']
assert dict.clear(r) is None and not r and r is alias and list(keys) == []
print('dict-subclass.copy-union-methods', r, r.owner)
