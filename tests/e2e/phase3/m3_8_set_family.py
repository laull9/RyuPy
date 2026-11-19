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

for name in ['isdisjoint', 'issubset', 'issuperset', 'intersection', 'difference_update']:
    try:
        getattr(set(), name)([[]])
    except TypeError:
        print('empty unhashable', name)

# 精确集合交集保留较小一方的元素；等长保留other，一般iterable仍消费other。
for left, right in [({1}, {True, 2}), ({1, 2}, {True}), ({1}, {True}), ({1}, {1.0})]:
    for other in [right, frozenset(right), list(right)]:
        result = left.intersection(other)
        target = left.copy()
        target.intersection_update(other)
        value = next(iter(result))
        updated = next(iter(target))
        print(type(other).__name__, len(left), len(right), type(value).__name__,
              type(updated).__name__, value is updated)
original = (1000,)
equal = (1000.0,)
for other in [{equal, 2}, {equal}, frozenset([equal, 2]), [equal, 2]]:
    result = {original}.intersection(other)
    actual = next(iter(result))
    print(actual is original, actual is equal)
