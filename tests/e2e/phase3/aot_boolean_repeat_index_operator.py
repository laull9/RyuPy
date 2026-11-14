# ryupy: pending
# 既有缺口：乘法运算符未对用户计数对象进入__index__，原始反例保留。

# __index__只调用一次，其对source的修改必须先于新结果存储形态判定。
trace = []
source = [True]
class RepeatCount:
    def __index__(self):
        trace.append("index")
        source[0] = False
        return 3
print("callback bool", source * RepeatCount(), source, trace)

source2 = [True]
class HeterogeneousCount:
    def __index__(self):
        source2[0] = "value"
        return 2
print("callback heterogeneous", source2 * HeterogeneousCount(), source2)

source3 = [True]
class EmptyCount:
    def __index__(self):
        source3.clear()
        return 4
print("callback empty", source3 * EmptyCount(), source3)

