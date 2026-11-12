# 函数内类按当次 self._dict 真实对象创建，不按包名或工厂名称猜布局。
class Marker:
    pass
class CustomStorage(dict):
    def tag(self):
        return 'custom'
class Factory:
    def __init__(self, base):
        self._dict = base
    def make(self):
        class Local(self._dict, Marker):
            pass
        return Local()
factory = Factory(dict)
first = factory.make()
first['a'] = 1
second = factory.make()
second['b'] = 2
print(sorted(first.items()), sorted(second.items()), type(first) is type(second))
print(isinstance(first, dict), isinstance(first, Marker))
factory._dict = CustomStorage
third = factory.make()
third['c'] = 3
print(third.tag(), sorted(third.items()), isinstance(third, CustomStorage))
print([base.__name__ for base in type(third).__mro__])
