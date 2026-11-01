# 集合推导式保持去重、独立作用域和副作用顺序；不依赖集合遍历顺序。
item = "外层"
print(sorted({item % 3 for item in range(7)}))
print(item)
print(sorted({left + right for left in range(3) if left > 0
              for right in range(left) if right % 2 == 0}))
print(len({x for x in []}))
print(sorted({a + b for a, b in [(1, 2), (2, 1), (3, 4)]}))


def build(seed):
    return {lambda: seed + item for item in range(3)}


print(sorted([f() for f in build(10)]))
log = []


def mark(value):
    log.append(value)
    return value % 2


print(sorted({mark(x) for x in range(4)}), log)
try:
    print({[] for x in [1]})
except TypeError as error:
    print(str(error))
