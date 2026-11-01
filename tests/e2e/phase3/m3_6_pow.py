# 一等 pow 用于 partial；整模幂不构造完整巨幂，负指数求模逆。
from functools import partial
print(partial(pow, 2)(5), pow(base=3, exp=4), pow(2, 10, 7))
print(pow(3, -1, 7), pow(3, -2, -7), pow(2, 0, 1))
for values in [(2, 3, 0), (2, -1, 4)]:
    try:
        print(pow(*values))
    except ValueError:
        print('ValueError')

for base, exponent in [(0.0, -1), (1e308, 2)]:
    try:
        print(pow(base, exponent))
    except ZeroDivisionError:
        print('ZeroDivisionError')
    except OverflowError:
        print('OverflowError')
print(pow(0.0, float('-inf')), pow(float('nan'), 0))
# 负指数进入浮点域前，两项大整数转换都必须检查溢出。
for base, exponent in [(10**400, -1), (2, -(10**400)), (1, -(10**400)), (0, -(10**400))]:
    try:
        print(pow(base, exponent))
    except OverflowError:
        print('large-int-overflow')
