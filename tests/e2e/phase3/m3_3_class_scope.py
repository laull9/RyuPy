# 类命名空间只作用于定义位置默认值/装饰表达式，不成为函数体的闭包。
value = 10


class Scope:
    value = 3
    read = lambda self: value
    captured = lambda self, default=value: default

    def method(self, factory=lambda current=value: current):
        return factory()


instance = Scope()
print(instance.read(), instance.captured(), instance.method())
