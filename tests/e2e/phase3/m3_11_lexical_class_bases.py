# ryupy: pending
# 动态base每次重新读取；冻结当次base，普通父类方法与构造协议不可丢失。
class First:
    def __init__(self, value):
        self.value = value
    def read(self):
        return self.value + 1
class Second:
    def __init__(self, value):
        self.value = value
    def read(self):
        return self.value + 2
class Holder:
    @property
    def base(self):
        print('read base')
        return self.value
holder = Holder()
holder.value = First

def rebind():
    holder.value = Second

def make():
    class Child(holder.base):
        rebind()
        field = 4
    return Child

left = make()
right = make()
print(left(10).read(), right(10).read())
print(issubclass(left, First), issubclass(right, Second), left is not right)
print(left.field, right.field)

def root(base):
    class Child(base):
        value = 8
    return Child
print(root(object)().value)
