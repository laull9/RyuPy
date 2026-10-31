# ryupy: pending
# M3.9：特化失配、大整数升级和临时对象逃逸不能改变结果。
def twice(value: int) -> int:
    return value + value


for value in [1, 2, "x", 3.5, 9223372036854775807]:
    print(twice(value))


def make_pair(value):
    return value, value


items = []
pair = make_pair(items)
pair[0].append(9)
print(pair[1], pair[0] is pair[1])
