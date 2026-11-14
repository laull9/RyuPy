# 多维索引中的 slice 复用通用对象协议，边界表达式从左至右各求值一次。
events = []

def bound(value):
    events.append(value)
    return value

class Indexed:
    def __getitem__(self, key):
        first = key[0]
        second = key[1]
        print(first.start, first.stop, first.step)
        print(second.start, second.stop, second.step)
        return 42

print(Indexed()[bound(1):bound(4):bound(-1), :bound(7)])
print(events)
