# 普通Enum成员、别名、值构造及类协议；不把IntEnum/Flag混入此范围。
from enum import Enum
class Color(Enum):
    RED = 1
    CRIMSON = RED
    BLUE = 2
    def label(self):
        return self.name.lower()
print(Color.RED.name, Color.RED.value, Color.RED.label())
print(Color.CRIMSON is Color.RED, Color(1) is Color.RED, Color(Color.RED) is Color.RED)
print([member.name for member in Color], len(Color))
print(Color['CRIMSON'] is Color.RED)
print(list(Color.__members__))
print([member.name for member in Color.__members__.values()])
print(dict(Color.__members__)['RED'] is Color.RED)
print(str(Color.RED), repr(Color.RED), repr(Color))
print(hash(Color.RED) == hash('RED'), Color.RED == 1, Color.RED is Color.BLUE)
print(isinstance(Color.RED, Color), isinstance(Color.RED, Enum), issubclass(Color, Enum))
class Mixed(Enum):
    ONE = 1
    YES = True
    TEXT = 'x'
    NOTHING = None
print(Mixed.YES is Mixed.ONE, Mixed(None).name, Mixed(value='x').name)
print(1 in Color, Color.RED in Color, 99 in Color)
try:
    Color(99)
except ValueError:
    print('value error')
try:
    Color['missing']
except KeyError:
    print('key error')
for action in range(4):
    try:
        if action == 0:
            Color.RED = 3
        elif action == 1:
            Color.RED.name = 'changed'
        elif action == 2:
            Color.RED.value = 3
        else:
            del Color.RED
    except AttributeError:
        print('readonly')
try:
    Color.__members__['RED'] = Color.BLUE
except TypeError:
    print('mapping readonly')
# 类成员名恰好为name/value，实例仍读Enum规范描述符；反复访问锁PIC路径。
class OddNames(Enum):
    name = 'n'
    value = 'v'
for _ in range(3):
    print(OddNames.name.name, OddNames.name.value, OddNames.value.name, OddNames.value.value)
print(type(Color) is type(Enum), type(Color).__name__, isinstance(Color, type(Enum)))
from enum import EnumType, EnumMeta
print(type(Color) is EnumType, EnumMeta is EnumType, issubclass(EnumType, type))
copy = Color.__members__.copy()
copy['RED'] = Color.BLUE
print(Color.__members__['RED'] is Color.RED, isinstance(Color.__members__, dict))
print(type(Color.__members__).__name__, len(Color.__members__), 'CRIMSON' in Color.__members__)
import json
try:
    json.dumps(Color.__members__)
except TypeError:
    print('proxy not JSON dict')
try:
    Color([])
except ValueError:
    print('unhashable value missing')
print(Color.RED.__class__ is Color, Color.__class__ is EnumType)
