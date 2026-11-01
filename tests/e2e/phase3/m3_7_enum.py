# 普通Enum的规范成员、别名和类发布时序；不冒充IntEnum/Flag或完整元类。
from enum import Enum as BaseEnum
import enum as enums

class Color(BaseEnum):
    RED = 1
    ALSO_RED = RED
    BLUE = 2
    def label(self):
        return self.name.lower()

class Values(enums.Enum):
    YES = True
    ONE = 1
    NONE = None
    TEXT = 'text'
    NEGATIVE = -2

print(Color(1) is Color.RED, Color(Color.RED) is Color.RED)
print(Color.ALSO_RED is Color.RED, Color.ALSO_RED.name, Color.BLUE.value)
print([item.name for item in Color], len(Color), Color['BLUE'] is Color.BLUE)
print(list(Color.__members__), Color.__members__['ALSO_RED'] is Color.RED)
print(str(Color.RED), repr(Color.RED), Color.BLUE.label())
print(isinstance(Color.RED, Color), isinstance(Color.RED, BaseEnum))
print(Color.RED == Color.BLUE, hash(Color.RED) == hash(Color.ALSO_RED))
print(Values.YES is Values.ONE, Values(None) is Values.NONE, Values('text').name, Values.NEGATIVE.value)
for invalid in [0, 'missing']:
    try:
        Color(invalid)
    except ValueError:
        print('missing-value')
try:
    Color['MISSING']
except KeyError:
    print('missing-name')
try:
    Color.RED.name = 'changed'
except AttributeError:
    print('readonly-name')
try:
    Color.RED = Color.BLUE
except AttributeError:
    print('readonly-member')

old = []
Published = old
def reject(cls):
    print(Published is old, cls.ONE.name)
    raise ValueError('decorator')
try:
    @reject
    class Published(BaseEnum):
        ONE = 1
except ValueError:
    print(Published is old)
