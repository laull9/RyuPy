# M2.1：静态函数里的默认值、关键字绑定与默认值求值时机。

def add(x: int, y: int = 5) -> int:
    return x + y


print(add(1), add(1, 2), add(y=3, x=4), add(x=2))


def scaled(x: int, factor: int = 2 * 3) -> int:
    return x * factor


print(scaled(2), scaled(2, 4))


def choose(a: int, b: int = 1, c: int = 2) -> int:
    return a * 100 + b * 10 + c


print(choose(1), choose(1, 2), choose(1, c=9), choose(c=8, b=7, a=6))

# 默认值在定义时求值：修改名字不影响已保存的默认值
base = 10


def from_name(x: int, extra: int = base) -> int:
    return x + extra


base = 100
print(from_name(1))
