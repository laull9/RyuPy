# 本轮全新重建语义样例；不是历史丢失源码恢复。
import subject
def custom(start, stop, step):
    print("custom", start, stop, step)
    return [12, 13]
subject.range = custom
print(subject.fold(1, 2, 8, 3))
