
# 精确list的有界切片删除、反向空窗口写入与index未命中；不扩展超大索引协议。

values = [0, 1, 2, 3, 4, 5]
alias = values
del values[1:4]
assert values == [0, 4, 5] and values is alias
values[1:1] = [8, 9]
assert values == [0, 8, 9, 4, 5]
del values[-2:]
assert values == [0, 8, 9]
print('list-primitives.contiguous-delete', values, values is alias)

values = [0, 1, 2, 3, 4, 5, 6, 7]
alias = values
del values[6:0:-2]
assert values == [0, 1, 3, 5, 7] and values is alias
del values[::-2]
assert values == [1, 5]
del values[::-1]
assert values == []
print('list-primitives.negative-step-delete', values, values is alias)

values = ['a', 'b', 'c', 'd', 'e']
alias = values
del values[4:1]
assert values == ['a', 'b', 'c', 'd', 'e']
values[4:1] = ['x', 'y']
assert values == ['a', 'b', 'c', 'd', 'x', 'y', 'e']
values[99:-99] = ['z']
assert values == ['a', 'b', 'c', 'd', 'x', 'y', 'e', 'z']
del values[99:-99]
assert values == ['a', 'b', 'c', 'd', 'x', 'y', 'e', 'z']
values[-2:-5] = ['q']
assert values == ['a', 'b', 'c', 'd', 'x', 'y', 'q', 'e', 'z']
print('list-primitives.empty-window', values, values is alias)

# 空窗口自源必须先有限复制；普通扩展切片保持数量检查与不写入边界。
values = [1, 2, 3]
alias = values
values[2:1] = values
assert values == [1, 2, 1, 2, 3, 3] and values is alias
values[::-2] = [30, 20, 10]
assert values == [1, 10, 1, 20, 3, 30]
try:
    values[::2] = [9]
except ValueError as error:
    assert str(error) == 'attempt to assign sequence of size 1 to extended slice of size 3'
else:
    raise AssertionError('missing slice size error')
assert values == [1, 10, 1, 20, 3, 30]
print('list-primitives.slice-self-and-size', values, values is alias)

values = [10, 20, 10]
assert values.index(10) == 0 and values.index(20) == 1
try:
    values.index(99)
except ValueError as error:
    assert str(error) == '99 is not in list'
    print('list-primitives.index-missing', type(error).__name__, str(error), values)
else:
    raise AssertionError('missing index ValueError')
try:
    [].index('gone')
except ValueError as error:
    assert str(error) == "'gone' is not in list"
    print('list-primitives.index-empty', type(error).__name__, str(error))
else:
    raise AssertionError('missing empty index ValueError')
