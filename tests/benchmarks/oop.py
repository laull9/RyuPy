# bench: category=objects
# bench: desc=多态方法分派（4 个子类轮换，超出 PIC 容量）、对象创建
class Shape:
    def __init__(self, size):
        self.size = size

    def area(self):
        return 0


class Square(Shape):
    def area(self):
        return self.size * self.size


class Rect(Shape):
    def __init__(self, size):
        Shape.__init__(self, size)
        self.other = size + 1

    def area(self):
        return self.size * self.other


class Tri(Shape):
    def area(self):
        return self.size * self.size // 2


class Line(Shape):
    pass


shapes = []
for i in range(1000):
    k = i % 4
    if k == 0:
        shapes.append(Square(i))
    elif k == 1:
        shapes.append(Rect(i))
    elif k == 2:
        shapes.append(Tri(i))
    else:
        shapes.append(Line(i))

total = 0
for _ in range(800):
    for s in shapes:
        total += s.area()

made = 0
for i in range(300000):
    p = Rect(i)
    made += p.other
print(total, made)
