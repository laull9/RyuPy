# Ryu有限边界，不与CP合法槽历史混称一致；拒绝之前不得部分修改。
from dataclasses import dataclass
from enum import Enum
from unittest import TestCase
class Case(TestCase):
    pass
assert isinstance(Case(), TestCase)
class Ordinary:
    def __new__(cls):
        return object.__new__(cls)
original = Ordinary.__dict__['__new__']
def remove_new():
    del Ordinary.__new__
for change in [lambda: setattr(Ordinary, '__new__', object.__new__), remove_new]:
    try:
        change()
    except NotImplementedError:
        assert Ordinary.__dict__['__new__'] is original
    else:
        raise AssertionError('rare slot history silently accepted')
assert type(Ordinary()) is Ordinary
@dataclass
class Data:
    value: int
class Error(Exception):
    pass
class Color(Enum):
    RED = 1
def custom(cls, *args):
    return object.__new__(cls)
assert Data(1).value == 1 and Color(1) is Color.RED
assert isinstance(Error('message'), Exception)
for cls in [Data, Error, Color]:
    try:
        cls.__new__ = custom
        cls(1)
    except NotImplementedError:
        pass
    else:
        raise AssertionError('special layout custom new ignored')
print('boundaries-ok')
