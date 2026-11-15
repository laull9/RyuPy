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

# 极端步长只选首/末引用；空源与空窗口独立，避免整数步长溢出遗漏。
first = [1]
last = [3]
values = [first, None, last]
for step in [-9223372036854775808, -9223372036854775807, 9223372036854775806, 9223372036854775807]:
    result = values[::step]
    assert len(result) == 1
    if step > 0:
        assert result[0] is first
    else:
        assert result[0] is last
    assert values[1:1:step] == []
    empty_source = []
    empty_result = empty_source[::step]
    empty_result.append(first)
    assert empty_source == []
    print(len(result), len(empty_result))

for invalid in [slice("bad", None), slice(None, "bad")]:
    try:
        flags[invalid]
    except TypeError:
        print("bad bound")
assert flags == [True] * 20
