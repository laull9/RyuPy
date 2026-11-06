# ryupy: pending
# 合法核心覆盖只冻结CP合同；NI绝不是与下面输出等价的成功。
EVENTS = []
class Hook(dict):
    def __iter__(self):
        EVENTS.append('iter')
        return iter(['virtual'])
    def keys(self):
        EVENTS.append('keys')
        return ['virtual']
    def __getitem__(self, key):
        EVENTS.append('getitem')
        return 9
    def __setitem__(self, key, value):
        EVENTS.append(('set', key))
        dict.__setitem__(self, key, value)
h = Hook(real=1)
assert dict(h) == {'virtual': 9} and EVENTS == ['keys', 'getitem']
EVENTS.clear()
x = Hook.fromkeys(['a', 'b'], 4)
assert EVENTS == [('set', 'a'), ('set', 'b')]
assert dict.__getitem__(x, 'a') == 4
print('pending.core-override', EVENTS)
