# M3.3：动态数据类初始化保留 property setter，异类比较交给反射协议。
from dataclasses import dataclass


def install_property(cls):
    def get_value(self):
        return self._value

    def set_value(self, value):
        print("setter", value)
        self._value = value * 2

    cls.value = property(get_value, set_value)
    return cls


@dataclass
@install_property
class Mutable:
    value: int


item = Mutable(3)
print(item.value)


class Other:
    def __eq__(self, other):
        print("reflected")
        return True


other = Other()
print(item.__eq__(other))
print(item == other)


def install_frozen_property(cls):
    def get_value(self):
        return 99

    def set_value(self, value):
        print("frozen setter", value)

    cls.value = property(get_value, set_value)
    return cls


@dataclass(frozen=True)
@install_frozen_property
class Frozen:
    value: int


frozen = Frozen(5)
print(frozen.value)
try:
    frozen.value = 6
except AttributeError:
    print("still frozen")
