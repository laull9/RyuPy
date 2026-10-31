# M2.9：有界 PIC 在类型变化、方法替换、模块重绑定与超限回退下保持语义。
import fixtures.pic_state as pic_state


def add(a, b):
    return a + b


print(add(1, 2), add(1.5, 2.5), add("a", "b"))
# 同一调用点反复切换形状，迫使 2 条目的 PIC 反复淘汰。
for _ in range(20):
    add(1, 2)
    add(1.5, 2.5)
    add("a", "b")
    add(True, True)
    add(10**30, 10**30)
print(add(3, 4), add(0.5, 0.25), add("x", "y"), add(False, True))


class Counter:
    def __init__(self):
        self.value = 0

    def bump(self):
        self.value += 1
        return self.value


counter = Counter()
for _ in range(10):
    counter.bump()
print(counter.value)


def replacement(self):
    self.value += 10
    return self.value


# 预热后替换类方法：类版本失效，不能继续用旧绑定。
Counter.bump = replacement
print(counter.bump(), counter.value)
# 实例属性遮蔽方法，再删除后恢复类方法。
counter.bump = 5
print(counter.bump)
del counter.bump
print(counter.bump())


def replacement_base():
    return "replacement"


def call(fn):
    return fn()


print(call(pic_state.base))
# 模块属性重绑定必须立即生效（模块属性不做 PIC 缓存）。
pic_state.base = replacement_base
print(call(pic_state.base))
print(call(counter.bump))
