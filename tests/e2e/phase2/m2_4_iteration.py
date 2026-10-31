# ryupy: pending
# M2.4：迭代耗尽、循环 else 与惰性消费。
iterator = iter([2, 4])
print(next(iterator), next(iterator), next(iterator, "end"))
try:
    next(iterator)
except StopIteration:
    print("exhausted")
for item in []:
    print(item)
else:
    print("empty else")
for item in [1, 2]:
    if item == 1:
        break
else:
    print("unexpected else")
print(list(enumerate("ab", 3)), list(zip([1, 2], [3])))
print(list(reversed([1, 2, 3])), 2 in [1, 2], "x" not in "abc")
