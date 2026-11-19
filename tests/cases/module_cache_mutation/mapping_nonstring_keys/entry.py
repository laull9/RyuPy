# ryupy: pending
# sys.modules本体的完整dict长尾；不将受控字符串view宣称为完整dict。
import sys
import mc_subject
cache = sys.modules
before = len(cache)
cache[928471] = mc_subject
cache[('cache-probe', 1)] = mc_subject
assert cache[928471] is mc_subject and cache[('cache-probe', 1)] is mc_subject
assert len(cache) == before + 2
for operation in (lambda: cache.__setitem__([], mc_subject), lambda: cache.__delitem__([])):
    try:
        operation()
    except TypeError as exc:
        assert str(exc) == "unhashable type: 'list'"
    else:
        raise AssertionError('unhashable key must fail')
del cache[928471]
del cache[('cache-probe', 1)]
assert len(cache) == before and sys.modules is cache
print('general mapping keys/size/failure preserve cache identity', True)
