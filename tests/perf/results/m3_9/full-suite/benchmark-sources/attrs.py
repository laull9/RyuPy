# bench: category=objects
# bench: desc=实例 / 类属性读写、property、继承方法查找与 super()
class Base:
    scale = 2

    def __init__(self, x):
        self.x = x
        self.y = 0

    def get(self):
        return self.x

    def bump(self, d):
        self.x = self.x + d


class Child(Base):
    def __init__(self, x):
        super().__init__(x)
        self.z = 1

    @property
    def doubled(self):
        return self.x * self.scale

    def bump(self, d):
        super().bump(d)
        self.y += 1


obj = Child(0)
total = 0
for i in range(1000000):
    obj.x = i
    total += obj.x + obj.z
for i in range(500000):
    total += obj.get()
for i in range(500000):
    total += obj.doubled
for i in range(300000):
    obj.bump(1)
print(total, obj.x, obj.y)
