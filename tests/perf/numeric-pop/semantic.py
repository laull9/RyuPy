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

values = produce([1.5, 2.5, 3.5], [2.0], 1, 3)
print(values.pop(1), values.pop(-1), values.pop(0), values)
try:
    values.pop()
except IndexError as error:
    print(type(error).__name__, str(error))
values = produce([float("nan"), 2.5, 3.5], [1.0], 1, 3)
observed = values[0]
print(values.pop(0) is observed, observed != observed, values)
shared = float("123.5")
values.append(shared)
values.append(shared)
print(values.pop() is shared, values[-1] is shared)
for index in [-9223372036854775808, 9223372036854775807]:
    before = values[:]
    try:
        values.pop(index)
    except IndexError as error:
        print(type(error).__name__, str(error), values == before)
values = produce([1.5, 2.5, 3.5], [2.0], 1, 3)
iterator = iter(values)
print(next(iterator), values.pop(0), list(iterator))
values.clear()
values.append(8.5)
print(list(iterator), values.pop())
