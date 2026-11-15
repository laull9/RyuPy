# 同一真实数值生产者；语义逐项观察与性能输入分离。
def produce(a, b, count, size):
    rows = []
    for outer in range(1):
        row = []
        for column in range(size):
            total = 0.0
            for index in range(count):
                total += a[column] * b[index]
            row.append(total)
        rows.append(row)
    return rows[0]

values = produce([1.5, 2.5, 3.5, 4.5, 5.5], [1.0], 1, 5)
alias = values
observed = values[2]
del values[0]
assert values[1] is observed
assert alias is values
del alias[-1]
assert values == [2.5, 3.5, 4.5]
del values[1]
assert values == [2.5, 4.5]
del values[-2]
del values[0]
assert len(alias) == 0
for bad in [0, -1, 9223372036854775807, -9223372036854775808]:
    try:
        del values[bad]
        assert False
    except IndexError:
        assert len(values) == 0
values.append(9.5)
assert values == [9.5]

values = produce([1.5, 2.5, 3.5], [1.0], 1, 3)
for bad in [3, -4, 9223372036854775807, -9223372036854775808]:
    try:
        del values[bad]
        assert False
    except IndexError:
        assert values == [1.5, 2.5, 3.5]
it = iter(values)
assert next(it) == 1.5
del values[0]
assert next(it) == 3.5
try:
    next(it)
    assert False
except StopIteration:
    pass
values.append(8.5)
try:
    next(it)
    assert False
except StopIteration:
    pass

# 本段在迭代前不执行列表整体相等或切片观察，独立覆盖紧凑输入。
fresh = produce([1.5, 2.5, 3.5], [1.0], 1, 3)
fresh_it = iter(fresh)
assert next(fresh_it) == 1.5
del fresh[1]
assert next(fresh_it) == 3.5
try:
    next(fresh_it)
    assert False
except StopIteration:
    pass
fresh.append(9.5)
try:
    next(fresh_it)
    assert False
except StopIteration:
    pass

shared = [1]
objects = [shared, 'middle', shared]
del objects[1]
assert objects[0] is objects[1]
flags = [True] * 4
flag_alias = flags
del flags[-2]
assert flags == [True, True, True]
assert flag_alias is flags
assert flags[0] is True
print('delete semantics ok')
