def produce(a, b, count, size):
    rows = []
    for outer in range(1):
        row = []
        for column in range(size):
            total = 0.0
            for index in range(count):
                total += a[index] * b[index]
            row.append(total)
        rows.append(row)
    return rows[0]

# 清空后重新使用背板；不能将少装箱等同总驻留内存下降。
total = 0
values = produce([1.5], [2.0], 1, 50000)
values.clear()
for repeat in range(40):
    for i in range(50000):
        values.append(float(i))
    total += len(values)
    values.clear()
print(total, len(values))
