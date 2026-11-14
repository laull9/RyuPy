# 本轮全新重建语义样例；不是历史丢失源码恢复。
import subject
def first(value, token):
    print("first", value, token)
    subject.TOKEN = 20
    subject.rhs = second
    return value + token
def second(value, token):
    print("second", value, token)
    return value + token
subject.rhs = first
print(subject.fold(0, [1, 2, 3]))
