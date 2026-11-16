# 两个用户声明类未必是两个物理布局；共同的 solid base 仍可能是 dict。
class First(dict):
    def one(self):
        return 1
class Second(dict):
    def two(self):
        return 2
class Combined(First, Second):
    pass
value = Combined({'x': 3})
print(value.one(), value.two(), sorted(value.items()))
print([base.__name__ for base in Combined.__mro__])
