# 显式object基类原语绕过用户hook和冻结限制，仍然执行property描述符。
from dataclasses import dataclass


class Holder:
    def __delattr__(self, name):
        raise AssertionError('不应进入用户删除hook')


item = Holder()
object.__setattr__(item, 'value', 42)
object.__delattr__(item, 'value')
print(hasattr(item, 'value'))


@dataclass(frozen=True)
class Frozen:
    value: int


frozen = Frozen(3)
object.__setattr__(frozen, 'value', 4)
print(frozen.value)
object.__delattr__(frozen, 'value')
print(hasattr(frozen, 'value'))
