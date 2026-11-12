# ryupy: pending
# 精确集合的方法族：实参绑定、流式消费、短路、部分写入与别名。
s = {1, 2, 3}
print(s.isdisjoint([4, 5]), s.issubset([1, 2, 3, 4]), s.issuperset([2, 2]))
print(sorted(s.union()), sorted(s.union([4], (5, 6))))
print(sorted(s.intersection()), sorted(s.intersection([2, 3, 4], [3])))
print(sorted(s.difference()), sorted(s.difference([1], [3])))
print(sorted(s.symmetric_difference([3, 4, 4])))
copy = s.copy()
copy.add(9)
print(sorted(s), sorted(copy), copy is s)
for method in [s.update, s.intersection_update, s.difference_update]:
    print(method() is None)
t = {1, 2}
print(t.update([3], [4]) is None, sorted(t))
print(t.difference_update([1], [4]) is None, sorted(t))
print(t.intersection_update([2, 3], [3]) is None, sorted(t))
print(t.symmetric_difference_update([3, 4, 4]) is None, sorted(t))
print(t.update(t) is None, sorted(t))
print(t.difference_update(t) is None, sorted(t))

def stream():
    print('first')
    yield 2
    print('second')
    yield 8
    print('third')

print({2}.isdisjoint(stream()))
print({1}.issuperset(stream()))
print(sorted({2}.intersection(stream())))
print({2}.issubset(stream()))

def failing():
    yield 5
    raise ValueError('end')

u = {1}
try:
    u.update(failing())
except ValueError:
    print('partial', sorted(u))
u = {1, 5}
try:
    u.intersection_update(failing())
except ValueError:
    print('atomic', sorted(u))
u = {1, 5}
try:
    u.difference_update(failing())
except ValueError:
    print('partial deletion', sorted(u))
for method in [s.isdisjoint, s.issubset, s.issuperset, s.symmetric_difference]:
    try:
        method()
    except TypeError:
        print('arity')
    try:
        method([1], bad=2)
    except TypeError:
        print('keyword')
