# 用户hash返回值单独归约；所有数值期待固定，不借primitive hash旧实现互证。
calls = []
class Result:
    def __hash__(self):
        calls.append('hash')
        return self.value
class Inherited(Result):
    pass
class Finalized:
    __hash__ = None
    if True:
        __hash__ = Result.__hash__

cases = [(False, 0), (True, 1), (-2, -2), (-1, -2), (0, 0), (1, 1),
         (2305843009213693950, 2305843009213693950),
         (2305843009213693951, 2305843009213693951),
         (2305843009213693952, 2305843009213693952),
         (-2305843009213693950, -2305843009213693950),
         (-2305843009213693951, -2305843009213693951),
         (-2305843009213693952, -2305843009213693952),
         (-9223372036854775809, -5),
         (-9223372036854775808, -9223372036854775808),
         (-9223372036854775807, -9223372036854775807),
         (9223372036854775806, 9223372036854775806),
         (9223372036854775807, 9223372036854775807),
         (9223372036854775808, 4),
         (1267650600228229401496703205376, 549755813888),
         (-1267650600228229401496703205376, -549755813888),
         (18446744073709551609, 1), (-18446744073709551609, -2),
         (18446744073709551608, 0), (-18446744073709551608, 0)]
for cls in [Result, Inherited, Finalized]:
    value = cls()
    for returned, expected in cases:
        value.value = returned
        calls.clear()
        assert hash(value) == expected
        assert calls == ['hash']
        # 显式调用原方法不是隐式hash槽；不能改写用户返回值本身。
        assert value.__hash__() == returned
        print(cls.__name__, returned, expected)

class Late:
    pass
late = Late()
root_hash = hash(late)
assert root_hash == object.__hash__(late)
Late.__hash__ = Result.__hash__
late.value = -1
assert hash(late) == -2
late.value = 1267650600228229401496703205376
assert hash(late) == 549755813888
del Late.__hash__
assert hash(late) == root_hash
assert hash(late) == object.__hash__(late)

conversions = []
class Convertible:
    def __index__(self):
        conversions.append('index')
        return 7
    def __int__(self):
        conversions.append('int')
        return 8
value = Result()
for returned in [None, 1.5, '7', [], Convertible()]:
    value.value = returned
    calls.clear()
    try:
        raise ValueError('active-result')
    except ValueError as active:
        try:
            hash(value)
        except TypeError as error:
            assert str(error) == '__hash__ method should return an integer'
            assert error.__context__ is active
            assert calls == ['hash']
            assert conversions == []
        else:
            raise AssertionError('bad result accepted')
print('hash-result-values-ok')
