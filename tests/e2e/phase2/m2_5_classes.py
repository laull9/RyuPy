# ryupy: pending
# M2.5：实例 / 类属性、绑定方法与常用描述符。
class Box:
    category = "box"

    def __init__(self, value):
        self.value = value

    def add(self, amount):
        self.value += amount
        return self.value

    @property
    def doubled(self):
        return self.value * 2

    @staticmethod
    def kind():
        return "static"

    @classmethod
    def named(cls):
        return cls.category


box = Box(3)
method = box.add
print(method(2), box.doubled, Box.kind(), Box.named())
box.category = "instance"
print(box.category, Box.category)
del box.category
print(box.category, hasattr(box, "missing"))
setattr(box, "extra", 7)
print(getattr(box, "extra"), getattr(box, "missing", 0))
