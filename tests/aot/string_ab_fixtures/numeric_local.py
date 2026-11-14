# 数值holder控制组：同一函数局部形状，检查字符串优化没有改变数值结果。
def accumulate(initial, count, piece):
    total = initial
    for i in range(count):
        total += piece
    return total

print(accumulate(0, 100000, 3))
