# 动态别名、跨模块对象槽与静态dataclass字段不抹身份。
import helpers
from helpers import identity
from dataclasses import dataclass

class Token:
    def __str__(self):
        raise AssertionError('unexpected conversion')

@dataclass
class Box:
    value: str

value = Token()
alias = identity
for item in [value, None, 42, 'text']:
    assert identity(item) is item
    assert alias(item) is item
    assert helpers.identity(item) is item
    assert helpers.declared_int(item) is item
    helpers.slot = item
    assert helpers.slot is item
    assert helpers.read() is item
    box = Box(item)
    assert box.value is item
    box.value = value
    assert box.value is value
assert helpers.branch(value, None, True) is value
assert helpers.branch(value, None, False) is None
assert helpers.numeric_loop(value, 10) == 45
assert helpers.numeric_loop(None, 0) == 0
# actualbuild 的 __file__ 是公开对象槽，外部写与内部函数读同一身份。
saved_file = helpers.__file__
for metadata_value in [value, None]:
    helpers.__file__ = metadata_value
    assert helpers.__file__ is metadata_value
    assert helpers.read_file() is metadata_value
helpers.__file__ = saved_file
assert helpers.__file__ is saved_file
assert helpers.read_file() is saved_file
print('opaque-identity-fields-loop')
