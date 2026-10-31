# M1.6：for range 的隐藏计数器与移位语义。
#
# 循环变量只是「每轮写一次」的普通变量：循环体给它重新赋值不能改变迭代次数，
# 也不能把计数器一起改掉。
for i in range(4):
    print(i)
    i = 100
print(i)

# 循环边界在进入循环时固定：循环体修改上界变量不影响迭代
n = 3
seen = 0
for j in range(n):
    seen += 1
    n = 100
print(seen, n)

# 移位量不能按 63 取模：
# 1 << 62 在 i64 内；-8 >> 100 按 Python 语义是 -1（全部移出后补符号位）
print(1 << 62)
print(-8 >> 1)
print(-8 >> 100)
print(8 >> 100)
print(-1 << 63)

# 负步长与空区间仍要正确
for k in range(10, 0, -3):
    print(k)
for k in range(5, 5):
    print("never")
