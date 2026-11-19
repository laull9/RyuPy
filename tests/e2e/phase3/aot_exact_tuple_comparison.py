# 精确tuple键相等比较保留元素身份、回调顺序、异常与跨数值类型。
events = []
class Element:
    def __init__(self, value):
        self.value = value
    def __eq__(self, other):
        events.append((self.value, other.value))
        return self.value == other.value
    def __hash__(self):
        return 7

a = Element(1)
b = Element(1)
c = Element(2)
print((a, "x") == (b, "x"), events)
events.clear()
print((a, a) == (a, a), events)
print((a,) == (b, c), events)
print((1, True, 2.0) == (1.0, 1, 2))
nan = float("nan")
print((nan,) == (nan,), (nan,) == (float("nan"),))
print({(a, "k"): "value"}[(b, "k")], events)
print(("user", "buy") == ("user", "buy"))
print((1, (2, 3)) == (1, (2, 3)))
class Broken:
    def __eq__(self, other):
        print("raise")
        raise ValueError("element")
try:
    print((Broken(),) == (Broken(),))
except ValueError as error:
    print(str(error))
