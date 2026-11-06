# ryupy: pending
# CP合法而当前竖切明确NI；此正例不列当前四应用，不执行。
from types import MappingProxyType
proxy = MappingProxyType(range(2))
assert proxy[1] == 1
print(type(proxy) is MappingProxyType)
