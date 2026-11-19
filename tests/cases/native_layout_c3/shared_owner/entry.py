# 两条继承路径回到同一真实 dict 祖先，只分配一份映射布局。
events = []
class Storage(dict):
    def __init__(self, data):
        events.append('Storage')
        super().__init__(data)
    def chain(self):
        return ['Storage']
class Left(Storage):
    def __init__(self, data):
        events.append('Left')
        super().__init__(data)
    def chain(self):
        return ['Left'] + super().chain()
class Right(Storage):
    def __init__(self, data):
        events.append('Right')
        super().__init__(data)
    def chain(self):
        return ['Right'] + super().chain()
class Diamond(Left, Right):
    pass
class Child(Diamond):
    pass
value = Child({'x': 3})
value['y'] = 4
print(events, value.chain(), sorted(value.items()))
print([base.__name__ for base in Child.__mro__])
print(isinstance(value, Left), isinstance(value, Right), isinstance(value, Storage))
print(dict.__getitem__(value, 'x'), super(Left, value).chain())
