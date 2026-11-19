# 不修改内建布局协议，只保护原先已有的别名继承和构造语义。
from exports import ListBase, DictBase, StringBase
class Values(ListBase):
    def total(self):
        return sum(self)
class Entries(DictBase):
    def total(self):
        return sum(self.values())
class Text(StringBase):
    def tagged(self):
        return '<' + self + '>'
values = Values([3, 1, 2])
values.append(4)
entries = Entries({'a': 2})
entries['b'] = 5
text = Text('abc')
print(list(values), values.total(), isinstance(values, list))
print(sorted(entries.items()), entries.total(), isinstance(entries, dict))
print(str(text), text.tagged(), isinstance(text, str))
