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

values = produce([1.5, 2.5, 3.5, 4.5], [1.0], 1, 4)
observed = values[1]
part = values[1:3]
reverse = values[2:0:-1]
assert len(values[2:2]) == 0
assert part[0] is observed
assert part[0] is reverse[1]
assert part[1] is reverse[0]
copy = values.copy()
assert copy[1] is part[0]
items = list(values)
assert items[1] is part[0]
values[1] = 8.5
assert part[0] is observed
part[1] = 9.5
assert values[2] == 3.5
assert copy[2] is reverse[0]
assert values[:] == [1.5, 8.5, 3.5, 4.5]
print('slice lifecycle ok')
