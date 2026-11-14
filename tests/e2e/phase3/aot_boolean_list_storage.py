# ryupy: pending
# 布尔重复列表：单例身份、别名、异构物化、活迭代、复杂协议回退与重复回调顺序。

flags = [True] * 6
alias = flags
print("identity", alias is flags, flags[0] is True, flags[-1] is True, flags[0] == 1)
flags[2] = False
print("boolean write", flags, alias[2] is False)
flags.append(False)
flags.extend([True, False])
print("append extend", flags)
copy = flags.copy()
copy.reverse()
print("copy reverse", copy, flags)
print("pop", copy.pop(), copy.pop(0), copy)
copy.clear()
copy.append(True)
print("clear append", copy, copy[0] is True)

# 整数1必须保留自己的对象身份；不能因与True相等而压为布尔。
one = 1
flags[0] = one
print("heterogeneous write", alias is flags, alias[0] is one, alias[0] is True, alias[1] is True)
flags.append("tail")
print("heterogeneous append", flags[-1], flags[1] is True)

# iterator必须随列表实时读取；异构写入不能让游标停在旧背板。
live = [True] * 3
iterator = iter(live)
print("first", next(iterator) is True)
live[1] = "changed"
live.append(False)
print("iterator after conversion", next(iterator), next(iterator) is True, next(iterator) is False)
try:
    next(iterator)
except StopIteration:
    print("exhausted")
live.append(True)
try:
    next(iterator)
except StopIteration:
    print("stays exhausted")

# 尚未发生异构写入时，追加布尔与逆向读取仍反映当前内容。
live_bool = [False] * 2
seen = []
for value in live_bool:
    seen.append(value)
    if len(seen) == 1:
        live_bool.append(True)
print("live append", seen, live_bool)
print("reverse iterator", list(reversed(live_bool)))

# slice、排序、索引/删除与自扩展走既有容器协议，保内容与别名。
complex_flags = [True] * 5
complex_alias = complex_flags
complex_flags[1:4:2] = [False, False]
print("slice", complex_flags[::-1], complex_alias is complex_flags)
complex_flags.sort()
print("sort", complex_flags)
complex_flags.insert(1, "inserted")
print("insert index", complex_flags.index("inserted"))
complex_flags.remove("inserted")
del complex_flags[1:3]
complex_flags.extend(complex_flags)
print("delete self extend", complex_flags)

# *=沿原对象更新，不能使公开过的元素序列别名失效。
inplace = [False] * 2
inplace_alias = inplace
inplace *= 3
print("inplace", inplace is inplace_alias, inplace)
inplace *= 0
print("inplace zero", inplace_alias)
print("zero negative", [True] * 0, [False] * -3, [] * 999)

# __index__只调用一次，其对source的修改必须先于新结果存储形态判定。
trace = []
source = [True]
class RepeatCount:
    def __index__(self):
        trace.append("index")
        source[0] = False
        return 3
print("callback bool", list.__mul__(source, RepeatCount()), source, trace)

source2 = [True]
class HeterogeneousCount:
    def __index__(self):
        source2[0] = "value"
        return 2
print("callback heterogeneous", list.__mul__(source2, HeterogeneousCount()), source2)

source3 = [True]
class EmptyCount:
    def __index__(self):
        source3.clear()
        return 4
print("callback empty", list.__mul__(source3, EmptyCount()), source3)

# 混合后自引用保持同一列表身份，repr沿既有递归保护。
recursive = [True] * 2
recursive.append(recursive)
print("recursive", recursive[-1] is recursive, recursive)
