# 真实包扩展诊断：不加入固定20分母，验证内联表的动态dict布局、标记混入及自定义字典。
import toml
from toml.decoder import InlineTableDict, TomlDecoder
from toml.encoder import TomlPreserveInlineDictEncoder

plain = toml.loads('title = "demo"\npoint = { x = 1, y = 2 }\n')
print(plain['title'], plain['point']['x'], plain['point']['y'])
print(isinstance(plain['point'], dict), isinstance(plain['point'], InlineTableDict))
encoded = toml.dumps(plain, encoder=TomlPreserveInlineDictEncoder())
restored = toml.loads(encoded)
print(restored == plain, isinstance(restored['point'], InlineTableDict))

class CustomDict(dict):
    def total(self):
        return sum(self.values())

decoder = TomlDecoder(_dict=CustomDict)
first = decoder.get_empty_inline_table()
second = decoder.get_empty_inline_table()
first['a'] = 3
first['b'] = 4
print(first.total(), isinstance(first, CustomDict), isinstance(first, InlineTableDict))
print(type(first) is type(second), len(second))
print([cls.__name__ for cls in type(first).__mro__])
custom = toml.loads('point = { x = 5, y = 6 }\n', _dict=CustomDict)
print(type(custom).__name__, custom['point'].total())
print(isinstance(custom['point'], CustomDict), isinstance(custom['point'], InlineTableDict))

nested = toml.loads('outer = { value = 1, inner = { value = 2 } }\n')
print(nested['outer']['value'], nested['outer']['inner']['value'])
print(isinstance(nested['outer'], InlineTableDict), isinstance(nested['outer']['inner'], InlineTableDict))
