# ryupy: pending
# M1.1：带类型标注的函数、局部变量推导、函数间前向引用。
def main_calc(x: int) -> int:
    y = helper(x) * 2
    return y + 1


def helper(n: int) -> int:
    t = n - 3
    return t


print(main_calc(10))
print(helper(0))
