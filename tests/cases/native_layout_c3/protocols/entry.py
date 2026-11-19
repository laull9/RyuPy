# native 根描述符、普通覆盖与数据描述符都沿同一真实 MRO；属性不是映射键。
events = []
class Mixin:
    def __getitem__(self, key):
        events.append(('get', key))
        return super().__getitem__(key) + 10
    def __setitem__(self, key, value):
        events.append(('set', key, value))
        super().__setitem__(key, value)
    @property
    def read_only(self):
        return 'property'
    @classmethod
    def owner(cls):
        return cls.__name__
    @staticmethod
    def stable():
        return 'static'
    def describe(self):
        return 'method'
class Table(Mixin, dict):
    pass
value = Table({'a': 2})
value['b'] = 5
print(value['a'], value['b'], dict.__getitem__(value, 'a'))
print(events, value.owner(), Table.stable())
value['read_only'] = 'mapping'
print(value.read_only, dict.__getitem__(value, 'read_only'))
try:
    value.read_only = 'attribute'
except AttributeError:
    print('property remains data descriptor')
value.note = 'attribute'
value['note'] = 'mapping'
print(value.note, dict.__getitem__(value, 'note'))
saved = value.describe
value.describe = lambda: 'instance'
print(saved(), value.describe())
del value.describe
print(value.describe(), sorted(value.keys()))
