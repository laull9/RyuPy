# 数据结构：有序容器。
from sortedcontainers import SortedDict, SortedList, SortedSet

values = SortedList([5, 1, 3])
values.add(2)
print(list(values), values.index(3))

mapping = SortedDict({"b": 2, "a": 1})
mapping["c"] = 3
print(list(mapping.items()))

unique = SortedSet([3, 1, 3, 2])
print(list(unique), unique[-1])
