# ryupy: pending
# 旧bool比较ABI不能保结果身份；保CP正例，不能把Truthy当完成。
from types import MappingProxyType
sentinel = object()
class M:
    def __getitem__(self, key):
        return 1
    def __eq__(self, other):
        return sentinel
    def __ne__(self, other):
        return sentinel
proxy = MappingProxyType(M())
assert (proxy == proxy) is sentinel
assert (proxy != proxy) is sentinel
print(True)
