# ryupy: pending
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
