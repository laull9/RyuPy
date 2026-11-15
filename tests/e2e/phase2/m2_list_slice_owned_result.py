# ryupy: pending
# 切片结果只构造一次：元素引用共享，但槽、长度及迭代状态与源独立。
values = [[i] for i in range(2050)]
for begin, end, step in [(1000, 2040, 3), (2040, 1000, -3), (-9999, 9999, 1), (100, 10, 1)]:
    result = values[begin:end:step]
    indices = list(range(2050))[begin:end:step]
    assert len(result) == len(indices)
    for j in range(len(indices)):
        assert result[j] is values[indices[j]]
    if result:
        result[0].append(77)
        assert values[indices[0]][-1] == 77
        old = values[indices[0]]
        result[0] = [-1]
        assert values[indices[0]] is old
    result.append([-2])
    assert len(values) == 2050
    print(len(indices), len(result))
empty = values[0:0]
empty.append([99])
assert len(values) == 2050
copy = values[:]
values.clear()
assert len(copy) == 2050
copy.clear()
assert empty == [[99]]
flags = [True] * 20
assert flags[1:10:2] == [True] * 5
try:
    flags[::0]
except ValueError:
    print("zero step")
print("slice identity and independence ok")
