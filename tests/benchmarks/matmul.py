# bench: category=numeric
# bench: desc=列表嵌套列表的 100×100 浮点矩阵乘法，重复 3 次
def make_matrix(n, seed):
    m = []
    x = seed
    for i in range(n):
        row = []
        for j in range(n):
            x = (x * 1103515245 + 12345) % 2147483648
            row.append((x % 1000) / 1000.0)
        m.append(row)
    return m


def matmul(a, b, n):
    result = []
    for i in range(n):
        row_a = a[i]
        row = []
        for j in range(n):
            s = 0.0
            for k in range(n):
                s += row_a[k] * b[k][j]
            row.append(s)
        result.append(row)
    return result


n = 100
a = make_matrix(n, 1)
b = make_matrix(n, 2)
c = a
for _ in range(3):
    c = matmul(c, b, n)
total = 0.0
for row in c:
    for v in row:
        total += v
print(round(total, 3))
