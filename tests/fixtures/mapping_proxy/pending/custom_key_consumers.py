# ryupy: pending
# CP正例：新增dict/update/**消费者的custom键域尚NI，不能连带封禁原getitem/copy。
from types import MappingProxyType as P
class Key:
    def __hash__(self):
        return 7
    def __eq__(self, other):
        return self is other
key = Key()
value = []
source = {key: value}
proxy = P(source)
# 这两条原方法真实委托必须保持；消费NI不意味着原方法也NI。
assert proxy[key] is value
assert proxy.copy()[key] is value
assert dict(proxy)[key] is value
out = {}
out.update(proxy)
assert out[key] is value

class TextKey(str):
    pass
text = TextKey('name')
text_proxy = P({text: value})
def receive(**kw):
    return kw
assert receive(**text_proxy)['name'] is value
print(True)
