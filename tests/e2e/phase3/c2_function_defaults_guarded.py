# 未观察默认值可直调；观察/修改和名字重绑都保callee先冻结及实参只求值一次。
import sys

seed = []
events = []


def value(a=seed, b=seed, c=seed):
    events.append('body')
    return a, b, c


result = value()
print(result[0] is seed, result[1] is seed, result[2] is seed)
seed.append('shared')
print(value(1))


def observe():
    events.append('observe')
    old = value.__defaults__
    return old[0]


print(value(observe()))


def update():
    events.append('update')
    value.__defaults__ = (2, 3, 4)
    return 8


print(value(update()))
print(events)


def replace(a=10, b=20):
    return 'old', a, b


def replacement(a=30, b=40):
    return 'new', a, b


def rebind():
    events.append('rebind')
    sys.modules['__main__'].replace = replacement
    return 9


print(replace(rebind()))
print(replace())


def factory(label):
    def nested(a=[]):
        a.append(label)
        return a
    return nested


left = factory('left')
right = factory('right')
print(left(), right(), left())
left.__defaults__ = (['changed'],)
print(left(), right())

current = ['before']


def swap():
    global current
    current = ['after']
    return 'second'


def capture(first, second, third=seed):
    return first, second, third


result = capture(current, swap())
print(result[0], current, result[2] is seed)
