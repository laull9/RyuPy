# 动态单基旧Native能力：实际普通类/内建布局仍成功，不宣称MI可AOT。
import bases
from exports import Reexport as Alias
from bases import ListBase, DictBase, StrBase

class Child(Alias):
    def read(self):
        return super().read() + 1

    def twice(self):
        return self.read() * 2

class AttrChild(bases.Plain):
    pass

class ListChild(ListBase):
    pass

class DictChild(DictBase):
    pass

class StrChild(StrBase):
    pass

print(Child(4).twice(), AttrChild(7).read())
values = ListChild([3, 1])
values.append(2)
print(list(values), len(values))
mapping = DictChild({'x': 4})
mapping['y'] = 2
print(mapping['x'], mapping['y'], len(mapping))
text = StrChild('ab')
print(text + 'c', len(text), text[1])
