# 精确整数比较快速返回bool；混合数值和用户比较仍保持原始结果。
values = [-9223372036854775808, -1, 0, 1, 9223372036854775807]
for a in values:
    for b in values:
        print(a == b, a != b, a < b, a <= b, a > b, a >= b)
print(1 == True, 1 < 1.5, 9223372036854775807 < 9223372036854775808)
class Result:
    def __bool__(self):
        print("truth")
        return True
result = Result()
class Compared:
    def __lt__(self, other):
        print("compare", other)
        return result
    def __gt__(self, other):
        print("reflected", other)
        return result
obj = Compared()
print((obj < 1) is result, (1 < obj) is result)
if obj < 2:
    print("branch")
def count(n):
    i = 0
    while i < n:
        yield i
        i += 1
print(list(count(4)))
