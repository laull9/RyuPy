# 本轮全新重建语义样例；不是历史丢失源码恢复。
TOKEN = 1
def fold(seed, values):
    total = seed
    for value in values:
        total += rhs(value, TOKEN)
    return total
def rhs(value, token):
    return value + token
