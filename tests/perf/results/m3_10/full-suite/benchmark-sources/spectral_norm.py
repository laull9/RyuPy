# bench: category=numeric
# bench: desc=谱范数 n=160，列表读写 + 带标注的浮点辅助函数
def eval_a(i: int, j: int) -> float:
    return 1.0 / ((i + j) * (i + j + 1) // 2 + i + 1)


def mul_av(v, out, n):
    for i in range(n):
        s = 0.0
        for j in range(n):
            s += eval_a(i, j) * v[j]
        out[i] = s


def mul_atv(v, out, n):
    for i in range(n):
        s = 0.0
        for j in range(n):
            s += eval_a(j, i) * v[j]
        out[i] = s


def mul_atav(v, out, tmp, n):
    mul_av(v, tmp, n)
    mul_atv(tmp, out, n)


n = 160
u = [1.0] * n
v = [0.0] * n
tmp = [0.0] * n
for _ in range(10):
    mul_atav(u, v, tmp, n)
    mul_atav(v, u, tmp, n)
vbv = 0.0
vv = 0.0
for i in range(n):
    vbv += u[i] * v[i]
    vv += v[i] * v[i]
print(round((vbv / vv) ** 0.5, 9))
