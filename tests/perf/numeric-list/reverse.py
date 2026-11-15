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

# 未观察浮点槽批量反转，最后只输出稳定校验值。
inputs = [float(i) for i in range(50000)]
total = 0.0
for repeat in range(40):
    values = produce(inputs, [2.0], 1, 50000)
    values.reverse()
    total += values[0] * 3.0 + values[-1]
print(total)
