# slice有限比较、哈希、indices回调顺序及完整异常cause/context用户帧合同。
a = slice(1, 4, 2)
b = slice(True, 4.0, 2)
assert a == b and not (a != b) and a <= b and a >= b
assert not (a < b) and not (a > b)
assert slice(1, 9, 8) < slice(2, 0, 0)
assert slice(1, 2, 9) < slice(1, 3, 0)
assert slice(1, 2, 3) < slice(1, 2, 4)
assert slice('a', b'a', None) < slice('b', b'b', None)
assert slice(bytearray(b'a')) == slice(bytearray(b'a'))
assert slice(slice(1, 2)) == slice(slice(1, 2))
assert {a: 'found'}[b] == 'found' and b in {a}
assert hash(a) == hash(b) and a.__hash__() == slice.__hash__(a)
print('public.compare', a == b, b in {a})
class Shared:
    def __eq__(self, other):
        raise AssertionError('identity called eq')
shared = Shared()
assert slice(shared, 1) == slice(shared, 1)
assert slice(shared, 1) < slice(shared, 2)
assert slice(1, shared) < slice(2, [])
print('public.short-circuit')
events = []
class Hashed:
    def __init__(self, name, result):
        self.name = name
        self.result = result
    def __hash__(self):
        events.append(self.name)
        return self.result
h = slice(Hashed('start', 1), Hashed('stop', 2), Hashed('step', 3))
hash(h)
hash(h)
assert events == ['start', 'stop', 'step', 'start', 'stop', 'step']
events.clear()
try:
    hash(slice(Hashed('first', 1), [], Hashed('unvisited', 3)))
except TypeError as caught:
    assert str(caught) == "unhashable type: 'list'"
else:
    raise AssertionError('unhashable component accepted')
assert events == ['first']
print('public.hash', events)
events.clear()
class Index:
    def __init__(self, name, value):
        self.name = name
        self.value = value
    def __index__(self):
        events.append(self.name)
        return self.value
s = slice(Index('start', 1), Index('stop', 8), Index('step', 2))
assert events == []
assert s.indices(Index('length', 10)) == (1, 8, 2)
assert events == ['length', 'step', 'start', 'stop']
print('public.indices-order', events)
events.clear()
try:
    s.indices(Index('length', -1))
except ValueError as caught:
    assert str(caught) == 'length should not be negative'
assert events == ['length']
events.clear()
try:
    slice(Index('start', 1), Index('stop', 8), Index('step', 0)).indices(Index('length', 10))
except ValueError as caught:
    assert str(caught) == 'slice step cannot be zero'
assert events == ['length', 'step']
print('public.indices-stop', events)
marker = LookupError('same-marker')
class Fails:
    def __index__(self):
        raise marker
try:
    slice(None).indices(Fails())
except LookupError as caught:
    assert caught is marker
print('public.exception-identity')
class ChainIndex:
    def __index__(self):
        raise LookupError('slice-index')
def slice_middle():
    slice(None).indices(ChainIndex())
def slice_outer():
    slice_middle()
try:
    raise ValueError('slice-cause')
except ValueError as cause:
    saved_cause = cause
try:
    raise RuntimeError('slice-ambient') from saved_cause
except RuntimeError:
    try:
        print('public.chain')
        slice_outer()
    finally:
        print('public.finally')
