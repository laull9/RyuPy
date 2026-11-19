# 普通hash结果与必要整数hash闭包；固定数值oracle，不碰空容器查询次数旧债。
calls = []
class Key:
    def __hash__(self):
        calls.append('hash')
        return self.returned
    def __eq__(self, other):
        if isinstance(other, Key):
            return self.number == other.number
        return self.number == other

# 用户返回有独立fits-i64规则；number是比较值，wanted是固定CP hash。
for returned, number, wanted in [(-1, -2, -2), (True, 1, 1),
        (1267650600228229401496703205376, 549755813888, 549755813888),
        (-1267650600228229401496703205376, -549755813888, -549755813888),
        (0, False, 0), (-2, -1, -2), (17, 17, 17),
        (0, 2305843009213693951, 0), (0, -2305843009213693951, 0),
        (1, 2305843009213693952, 1), (-2, -2305843009213693952, -2),
        (-5, -9223372036854775809, -5), (-4, -9223372036854775808, -4),
        (-3, -9223372036854775807, -3), (2, 9223372036854775806, 2),
        (3, 9223372036854775807, 3), (4, 9223372036854775808, 4),
        (549755813888, 1267650600228229401496703205376, 549755813888),
        (-549755813888, -1267650600228229401496703205376, -549755813888),
        (1, 18446744073709551609, 1), (-2, -18446744073709551609, -2),
        (0, 18446744073709551608, 0), (0, -18446744073709551608, 0)]:
    key = Key()
    key.returned = returned
    key.number = number
    alias = Key()
    alias.returned = returned
    alias.number = number
    calls.clear()
    assert hash(key) == wanted
    assert calls == ['hash']
    assert hash(number) == wanted
    mapping = {key: 'first'}
    calls.clear()
    mapping[alias] = 'second'
    assert calls == ['hash']
    assert len(mapping) == 1
    assert next(iter(mapping)) is key
    assert mapping[number] == 'second'
    assert {number: 'builtin'}[key] == 'builtin'
    assert number in {key}
    assert key in {number}
    values = {key}
    calls.clear()
    values.add(alias)
    assert calls == ['hash']
    assert len(values) == 1
    assert next(iter(values)) is key
    if number == 1:
        for equivalent in [True, 1.0]:
            assert mapping[equivalent] == 'second'
            assert key in {equivalent}
            assert {equivalent: 'builtin'}[key] == 'builtin'
    print('mixed', returned, number, wanted)

# 只纳入double可精确表示的整数；原始等值关系先断言，不能把float(P)当P。
for number, wanted in [(-1, -2), (0, 0), (1, 1),
        (9007199254740992, 9007199254740992),
        (-9007199254740992, -9007199254740992),
        (2305843009213693952, 1), (-2305843009213693952, -2),
        (-9223372036854775808, -4), (9223372036854775808, 4),
        (1267650600228229401496703205376, 549755813888),
        (-1267650600228229401496703205376, -549755813888)]:
    floating = float(number)
    assert floating == number
    assert hash(number) == wanted
    assert hash(floating) == wanted
    key = Key()
    key.returned = wanted
    key.number = number
    assert hash(key) == wanted
    assert {floating: 'float'}[number] == 'float'
    assert {number: 'integer'}[floating] == 'integer'
    assert {floating: 'float'}[key] == 'float'
    assert {key: 'user'}[floating] == 'user'
    assert len({key, number, floating}) == 1
    assert len({floating, number, key}) == 1
    assert key in {floating} and floating in {key}
    print('float-mixed', number, wanted)
assert float(9223372036854775807) != 9223372036854775807
assert float(2305843009213693951) != 2305843009213693951

# 用户自己返回不合equal合同的宽i64值保持raw；不能为修混键强行归约它。
for number, primitive in [(2305843009213693951, 0),
                          (-2305843009213693951, 0),
                          (-9223372036854775808, -4),
                          (9223372036854775807, 3)]:
    key = Key()
    key.number = number
    key.returned = number
    assert hash(key) == number
    assert hash(number) == primitive
    assert hash(key) != hash(number)

# 每次操作用新异常，但该次抛出与捕获保持同一对象；复用异常旧债仍单列pending。
class Failing:
    def __hash__(self):
        calls.append('failure')
        raise self.error
failing = Failing()
for operation in ['hash', 'dict-set', 'set-add']:
    failing.error = RuntimeError('hash-body')
    calls.clear()
    try:
        raise ValueError('active-callback')
    except ValueError as active:
        try:
            if operation == 'hash':
                hash(failing)
            elif operation == 'dict-set':
                mapping = {1: 1}
                mapping[failing] = 2
            else:
                values = {1}
                values.add(failing)
        except RuntimeError as error:
            assert error is failing.error
            assert error.__context__ is active
            assert calls == ['failure']
        else:
            raise AssertionError(operation)
print('hash-result-containers-ok')
