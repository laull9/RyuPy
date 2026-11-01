# bench: category=algorithms
# bench: desc=sorted / list.sort：整数、元组、key 函数与 reverse，输入由 LCG 生成
def lcg_list(n, seed):
    out = []
    x = seed
    for _ in range(n):
        x = (x * 1103515245 + 12345) % 2147483648
        out.append(x)
    return out


def second(pair):
    return pair[1]


data = lcg_list(200000, 42)
a = sorted(data)
b = sorted(data, reverse=True)
pairs = []
for i in range(100000):
    pairs.append((data[i] % 1000, i))
c = sorted(pairs)
d = sorted(pairs, key=second)
data.sort()
print(a[0], a[-1], b[0], c[0], c[-1], d[0], data[100])
