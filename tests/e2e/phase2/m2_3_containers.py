# M2.3：容器构造、共享引用、解包与循环 repr。
values = [1, "two", None]
alias = values
alias.append(4)
print(values, alias is values)
first, *middle, last = values
print(first, middle, last)
pair = (values, 9)
pair[0][0] = 7
print(values, pair[1])
mapping = {"a": 1, "b": 2}
mapping["a"] = 3
del mapping["b"]
mapping["b"] = 4
print(list(mapping.items()), mapping.get("missing", 0))
print(sorted({3, 1, 3} | {2}), sorted(frozenset([2, 1])))
cycle = []
cycle.append(cycle)
print(cycle)
print(pair == (values, 9), (1, 2) < (1, 3), [1, 2] < [1, 3], (1, 2) <= (1, 2))
try:
    print(values < (1, 2))
except TypeError:
    print("list vs tuple")
mapping.update({"c": 5})
print(list(mapping.values()), mapping.pop("a", 0), mapping.pop("missing", -1))
duplicate = values
values.insert(0, 0)
print(duplicate is values, values.index(0), values.count(0))
del values[0]
print(values, duplicate is values, values.remove(7) is None)
alias.extend((10, 11))
print(values, tuple(values), list(values) is not values)
pairelt = (1, [2])
pairinner = pairelt[1]
pairinner.append(3)
print(pairelt, pairelt[1] is pairinner)
