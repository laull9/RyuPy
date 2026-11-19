# ryupy: pending
# 已有唯一CP参考证实合法；首切未承诺GenericAlias，不运行。
from types import MappingProxyType
assert repr(MappingProxyType[str, int]) == 'mappingproxy[str, int]'
print(True)
