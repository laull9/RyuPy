# ryupy: pending
# 定义位置普通类合同：同一代码模板的身份、默认值、闭包和递归互相独立。
def factory(value):
    class Item:
        tag = value
        def read(self, initial=value):
            return initial, value, __class__
        @property
        def current(self):
            return value
        @staticmethod
        def twice(x):
            return x * 2
        @classmethod
        def identity(cls):
            return cls
    def update(new_value):
        nonlocal value
        value = new_value
    return Item, update

left, update = factory(3)
right, unused = factory(5)
update(9)
a = left().read()
b = right().read()
print(left is not right, left.tag, right.tag)
print(a[0], a[1], a[2] is left, b[0], b[1], b[2] is right)
print(left().current, left.twice(4), left.identity() is left)

def recursive(depth):
    class Node:
        level = depth
        def owner(self):
            return __class__
    if depth:
        child = recursive(depth - 1)
        print(Node is not child, Node.level, child.level, Node().owner() is Node)
    return Node

recursive(2)
classes = []
for index in range(2):
    classes.append(factory(index)[0])
print(classes[0] is not classes[1], classes[0]().read()[0], classes[1]().read()[0])
