# 字符串有界累加 AOT 优化测试集：覆盖顶层模块绑定、多循环偏置、非 inplace 拼接、别名快照与边界

# 1. 顶层模块全局变量 += 累加（str_concat 模式）
text1 = ""
for i in range(100):
    text1 += "ab"
print("test1 len:", len(text1), "sample:", text1[:6], text1[-4:])

# 2. 顶层非首个循环且非 inplace 的字符串拼接（containers 第 4 循环模式）
# 先经历容器/数值前置循环，确保 preheader 不等于 entry 块
items = []
for i in range(50):
    items.append(i)

lookup = {}
for i in range(50):
    lookup[i] = i

total_num = 0
for i in range(50):
    total_num += lookup[i]

# 目标第 4 循环
text2 = "head"
for i in range(100):
    text2 = text2 + "x"

print("test2 len:", len(text2), "prefix:", text2[:4], "suffix:", text2[-3:], "num:", total_num)

# 3. 别名与快照不可变性：循环前别名不能被后续追加破坏
seed = "immutable_seed"
alias_before = seed
acc_seed = seed
for i in range(50):
    acc_seed += "_chunk"

print("test3 alias unchanged:", alias_before == "immutable_seed", alias_before is not acc_seed)
print("test3 acc_seed len:", len(acc_seed))

# 4. 空追加与单元素追加
empty_acc = ""
for i in range(20):
    empty_acc += ""
print("test4 empty:", empty_acc == "", len(empty_acc))

# 5. Unicode 与多字节字符无损追加
unicode_acc = "起点"
for i in range(30):
    unicode_acc = unicode_acc + "🌟测试"
print("test5 unicode len:", len(unicode_acc), "starts:", unicode_acc.startswith("起点"), "ends:", unicode_acc.endswith("测试"))

# 6. 函数局部非 inplace 拼接（常规函数模式）
def local_concat(prefix, count):
    buf = prefix
    for i in range(count):
        buf = buf + "z"
    return buf

print("test6 local func:", local_concat("base_", 25))

# 7. 循环内有观察点时保守走标准动态路径，保持一致性
observed_trace = []
obs_text = "start"
for i in range(5):
    observed_trace.append(obs_text)
    obs_text += "!"
print("test7 observed steps:", len(observed_trace), observed_trace[0], observed_trace[-1], obs_text)

# 8. 已绑定目标的零次循环保留初值引用。
zero_text = "zero_seed"
zero_alias = zero_text
for i in range(0):
    zero_text += "suffix"
print("test8 zero identity:", zero_alias is zero_text)

# 9. 动态左值的非原地与原地协议须分别调用，不进入精确字符串快路。
protocol_events = []


class CustomConcat:
    def __add__(self, right):
        protocol_events.append("add:" + right)
        return self

    def __iadd__(self, right):
        protocol_events.append("iadd:" + right)
        return self


custom_text = CustomConcat()
for i in range(3):
    custom_text = custom_text + "x"
for i in range(2):
    custom_text += "y"
print("test9 protocols:", protocol_events)
