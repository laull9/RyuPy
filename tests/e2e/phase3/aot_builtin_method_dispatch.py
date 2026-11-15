# 小参数内建方法保持身份、参数展开、子类覆盖与哈希回调重入。
items = []
marker = []
print(items.append(marker), items.pop() is marker)
items.append(*[1])
items.append(*(2,))
print(items.pop(*[0]), items)
try:
    items.append(*[1, 2])
except TypeError:
    print("append arity")
try:
    items.pop(0, 1)
except TypeError:
    print("pop arity")
try:
    items.pop(unexpected=1)
except TypeError:
    print("pop keyword")
d = {"x": marker}
print(d.get("x") is marker, d.get("missing", marker) is marker, d.get(*["x"]) is marker)
s = set()
print(s.add(1), s.add(True), len(s))
class L(list):
    def append(self, value):
        print("override append", value)
    def pop(self):
        return "override pop"
l = L()
l.append(3)
print(l.pop())
class D(dict):
    def get(self, key, default=None):
        return "override get"
print(D().get("x", 1))
class Key:
    def __hash__(self):
        items.append("hash")
        return 8
    def __eq__(self, other):
        print("eq")
        return True
key = Key()
d[key] = marker
print(d.get(Key(), None) is marker, items)
s.add(key)
print(len(s))

# 同一get调用点在__hash__中重入，外层默认值必须仍是原引用。
def lookup(key, default):
    return d.get(key, default)
class Reentrant:
    def __hash__(self):
        print(lookup("nested missing", "nested"))
        return 1234567
print(lookup(Reentrant(), marker) is marker)
