# ryupy: pending
# 完整CP正例：真实str子类限定名保原对象，不应假报非字符串TypeError。
class Label(str):
    pass

def target(value):
    return value + 1

label = Label('Namespace.target')
assert isinstance(label, str) and type(label) is Label
target.__qualname__ = label
assert target.__qualname__ is label
assert type(target.__qualname__) is Label
assert target.__name__ == 'target'
assert target(4) == 5
assert '__qualname__' not in target.__dict__
print(target.__qualname__, type(target.__qualname__).__name__, target(4))
