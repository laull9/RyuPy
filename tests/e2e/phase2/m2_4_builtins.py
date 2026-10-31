# M2.4：稳定排序、key 调用与短路消费。
def second(item):
    print("key", item[0])
    return item[1]


items = [("a", 2), ("b", 1), ("c", 2)]
print(sorted(items, key=second), items)
items.sort(key=second, reverse=True)
print(items)
print(sum([1, 2, 3], 10), min([3, 1]), max([], default=9))
print(any([0, "yes", 0]), all([1, 2]), all([]), any([]))
print(list(range(5, -1, -2)))
