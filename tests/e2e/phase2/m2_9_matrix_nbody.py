# ryupy: pending
# M2.9：容器与普通对象组成矩阵乘法、双体数值更新的集成切片。
left = [[1, 2, 3], [4, 5, 6]]
right = [[7, 8], [9, 10], [11, 12]]
result = []
for row in left:
    output = []
    for column in range(2):
        total = 0
        for inner in range(3):
            total += row[inner] * right[inner][column]
        output.append(total)
    result.append(output)
print(result)


class Body:
    def __init__(self, x, velocity):
        self.x = x
        self.velocity = velocity


bodies = [Body(-1.0, 0.0), Body(1.0, 0.0)]
for _ in range(10):
    distance = bodies[1].x - bodies[0].x
    acceleration = 1.0 / (distance * distance)
    bodies[0].velocity += acceleration * 0.1
    bodies[1].velocity -= acceleration * 0.1
    for body in bodies:
        body.x += body.velocity * 0.1
print(round(bodies[0].x, 6), round(bodies[1].x, 6))
