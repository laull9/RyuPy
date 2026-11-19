# 本轮全新重建语义样例；不是历史丢失源码恢复。
import subject
def rhs(value):
    print("rhs", value)
    return value
try:
    subject.fold(0, [1], rhs)
except UnboundLocalError:
    print("unbound")
