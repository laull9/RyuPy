# 本轮全新重建语义样例；不是历史丢失源码恢复。
import subject
def replacement(a, b, c):
    print("replacement", a, b, c)
    return [40]
def arg(value):
    print("arg", value)
    subject.range = replacement
    return value
print(subject.fold(0, arg))
print(subject.fold(0, arg))
