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

# 未观察数值背板先reverse，随后核数值；NaN用真实producer生成。
raw = produce([1.5, 2.5, 3.5], [2.0], 1, 3)
raw.reverse()
print(raw)
raw.clear()
print(raw)
# 独立未观察producer，清空之前不打印/迭代/切片，避免先物化。
unobserved = produce([1.5, 2.5, 3.5], [2.0], 1, 3)
unobserved.clear()
print(len(unobserved))
raw_nan = produce([float("nan"), float("nan")], [1.0], 1, 2)
raw_nan.reverse()
observed = raw_nan[0]
print(observed != observed, raw_nan[0] is observed)
# 数值槽反转/清空的公开语言语义；不依赖内部反射。
values = produce([float(i) for i in range(20)], [2.0], 1, 20)
first = values[0]
last = values[-1]
it = iter(values)
print(next(it) is first)
values.reverse()
print(values[0] is last, values[-1] is first, next(it), len(values))
values.clear()
values.extend([3.5, 4.5, 5.5])
print(next(it), list(it))
values.clear()
values.append(9.5)
print(list(it), values)
zero = -0.0
nan = float("nan")
shared = [zero, nan, zero]
shared.reverse()
print(shared[0] is zero, shared[1] is nan, shared[2] is zero, str(shared[0]))
# 切片/异构值强迫普通表示，保留别名和身份。
alias = shared
shared.append("x")
shared.reverse()
print(alias is shared, shared[0], shared[-1] is zero)
shared.clear()
print(alias, shared)
