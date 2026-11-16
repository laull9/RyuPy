# 静态可见的def省略参数也必须消费当前默认值，并在显式实参求值后读取状态。
def value(a=1, *, b=2):
    return a, b


print(value())
value.__defaults__ = (3,)
value.__kwdefaults__["b"] = 4
print(value())


def change():
    value.__kwdefaults__["b"] = 8
    return 7


print(value(change()))
del value.__defaults__
try:
    value()
except TypeError as error:
    print(type(error).__name__, str(error))
value.__defaults__ = (9,)
del value.__kwdefaults__
try:
    value()
except TypeError as error:
    print(type(error).__name__, str(error))
print(value(10, b=11))


def typed(a: int = 12) -> int:
    return a


typed.__defaults__ = (13,)
print(typed(), typed(14))


def factory():
    def nested(a=[]):
        return a
    return nested


left = factory()
right = factory()
print(left.__defaults__ is right.__defaults__, left.__defaults__[0] is right.__defaults__[0])
left.__defaults__ = ([15],)
print(left(), right())
