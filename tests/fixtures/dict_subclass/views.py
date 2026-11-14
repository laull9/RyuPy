
# 活视图与游标：覆值可见、大小变更重复报错、正常耗尽永久停止。
class Table(dict):
    pass

t = Table(a=1, b=2)
keys, values, items = t.keys(), t.values(), t.items()
iterator = iter(items)
assert next(iterator) == ('a', 1)
t['b'] = 20
assert next(iterator) == ('b', 20)
assert list(values) == [1, 20] and ('b', 20) in items and 20 in values
assert list(reversed(keys)) == ['b', 'a']
assert list(reversed(values)) == [20, 1]
assert list(reversed(items)) == [('b', 20), ('a', 1)]
assert next(iterator, 'done') == 'done'
t['c'] = 3
assert next(iterator, 'done') == 'done' and list(keys) == ['a', 'b', 'c']
assert values == values and values != t.values()
print('dict-subclass.live-views', list(items), list(reversed(t)))

for kind in ('keys', 'values', 'items', 'reverse'):
    t = Table(a=1, b=2)
    if kind == 'keys':
        cursor = iter(t)
    elif kind == 'values':
        cursor = iter(t.values())
    elif kind == 'items':
        cursor = iter(t.items())
    else:
        cursor = reversed(t)
    next(cursor)
    t['c'] = 3
    for attempt in range(2):
        try:
            next(cursor)
        except RuntimeError as error:
            assert str(error) == 'dictionary changed size during iteration'
        else:
            raise AssertionError('mutation failure not sticky')
    print('dict-subclass.size-sticky', kind)
