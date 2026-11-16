# 语义观察单独运行，不与冷进程性能混合。
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

for position in [-9223372036854775808, -4294967296, -4, -1, 0, 2, 3, 9223372036854775807]:
    values = produce([1.5, 2.5, 3.5], [1.0], 1, 3)
    alias = values
    observed = values[1]
    inserted = 9.5
    values.insert(position, inserted)
    expected = [1.5, 2.5, 3.5]
    expected.insert(position, inserted)
    assert values == expected
    assert values is alias
    saw_observed = False
    saw_inserted = False
    for value in values:
        if value is observed:
            saw_observed = True
        if value is inserted:
            saw_inserted = True
    assert saw_observed and saw_inserted

values = produce([1.5, 2.5], [1.0], 1, 2)
shared = 9.5
values.insert(0, shared)
values.insert(2, shared)
assert values[0] is values[2]
values.insert(1, 'mixed')
assert values == [9.5, 'mixed', 1.5, 9.5, 2.5]
assert values[0] is shared

values = produce([1.5, 2.5], [1.0], 1, 2)
it = iter(values)
assert next(it) == 1.5
values.insert(0, 9.5)
assert next(it) == 1.5
assert next(it) == 2.5
try:
    next(it)
    assert False
except StopIteration:
    pass
values.insert(3, 8.5)
try:
    next(it)
    assert False
except StopIteration:
    pass

calls = []
class Index:
    def __index__(self):
        calls.append('index')
        del values[0]
        return -1
values = produce([1.5, 2.5], [1.0], 1, 2)
values.insert(Index(), shared)
assert calls == ['index']
assert values == [9.5, 2.5]
assert values[0] is shared
class BadIndex:
    def __index__(self):
        calls.append('bad')
        raise ValueError('index exploded')
try:
    values.insert(BadIndex(), shared)
    assert False
except ValueError as error:
    assert str(error) == 'index exploded'
assert calls == ['index', 'bad']
assert values == [9.5, 2.5]

flags = [True] * 3
flags.insert(1, shared)
assert flags == [True, 9.5, True, True]
assert flags[0] is True
assert flags[1] is shared
# 扩容边界及尾插后头插必须保留先前对象身份。
values = produce([1.5, 2.5, 3.5, 4.5], [1.0], 1, 4)
observed = values[1]
shared_tail = 8.5
values.insert(4, shared_tail)
values.insert(9223372036854775807, shared_tail)
assert values[4] is values[5]
assert values[1] is observed
values.insert(0, 9.5)
assert values == [9.5, 1.5, 2.5, 3.5, 4.5, 8.5, 8.5]
assert values[2] is observed
assert values[5] is values[6]
values.insert(7, shared_tail)
assert values[5] is values[7]
# 回调修改后长度必须重新读取，返回的新Count才是尾部。
values = produce([1.5, 2.5], [1.0], 1, 2)
tail_calls = []
class TailIndex:
    def __index__(self):
        tail_calls.append('tail')
        del values[0]
        return len(values)
values.insert(TailIndex(), shared_tail)
assert tail_calls == ['tail']
assert len(values) == 2
assert values[0] == 2.5
assert values[1] is shared_tail

# 创建迭代器后先尾插，不用整体比较提前物化fresh生产者。
values = produce([1.5, 2.5], [1.0], 1, 2)
it = iter(values)
values.insert(2, shared_tail)
assert next(it) == 1.5
assert next(it) == 2.5
assert next(it) is shared_tail
try:
    next(it)
    assert False
except StopIteration:
    pass
values.insert(3, shared_tail)
try:
    next(it)
    assert False
except StopIteration:
    pass
print('tail insert semantics ok')
