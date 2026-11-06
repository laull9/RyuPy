# ryupy: pending
# 自定义hash/equality键及tuple键在CP合法，但超出新布局scalar键域。
EVENTS = []
class Key:
    def __hash__(self):
        EVENTS.append('hash')
        return 41
    def __eq__(self, other):
        EVENTS.append('eq')
        return True
class D(dict):
    pass
k = Key()
d = D()
d[k] = 3
EVENTS.clear()
e = d.copy()
assert EVENTS == [] and type(e) is dict
assert e[k] == 3
assert EVENTS == ['hash']
d[(1, 2)] = 4
assert d[(1, 2)] == 4
print('pending.custom-keys', len(d), EVENTS)
