# ryupy: pending
# CP先合并前一**再求后一显式实参；当前MIR先求所有表达式，运行时helper不能补救。
from types import MappingProxyType as P
log = []
class M:
    def keys(self):
        log.append('keys')
        return ['a']
    def __getitem__(self, key):
        log.append('getitem:' + key)
        return 1
proxy = P(M())
def later():
    log.append('later')
    return 2
def receive(**kw):
    return kw
assert receive(**proxy, tail=later()) == {'a': 1, 'tail': 2}
assert log == ['keys', 'getitem:a', 'later']
print(True)
