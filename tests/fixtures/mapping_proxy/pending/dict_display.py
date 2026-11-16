# ryupy: pending
# HIR现直接拒绝dict展开，保留CP正例；本切不扩大compiler来转正。
from types import MappingProxyType as P
d = {'a': [1]}
p = P(d)
assert {**p} == d
log = []
class K:
    def keys(self):
        log.append(('keys',))
        return ['a', 'b']
    def __getitem__(self, key):
        log.append(('getitem', key))
        return {'a': 10, 'b': 20}[key]
k = P(K())
assert {**k} == {'a': 10, 'b': 20}
assert log == [('keys',), ('getitem', 'a'), ('getitem', 'b')]
print(True)
