# 类体同名项只覆盖实例读取，不改变类本身的真实元类。
token = object()
events = []
class Constant:
    __class__ = token
class Child(Constant):
    pass
class Null:
    __class__ = None
for cls in (Constant, Child):
    obj = cls()
    assert obj.__class__ is token and type(obj) is cls
    assert cls.__class__ is type
assert Null().__class__ is None and Null.__class__ is type
print('plain-and-none', True)

class Property:
    @property
    def __class__(self):
        events.append('getter')
        return token
class PropertyChild(Property):
    pass

def read(obj):
    return obj.__class__

for cls in (Property, PropertyChild):
    obj = cls()
    before = len(events)
    assert type(obj) is cls and cls.__class__ is type
    assert len(events) == before
    for i in range(12):
        assert read(obj) is token
    assert len(events) == before + 12
print('property-each-read', len(events))

class Method:
    def __class__(self):
        events.append('method')
        return self
obj = Method()
before = len(events)
saved = read(obj)
assert len(events) == before
assert type(obj) is Method and Method.__class__ is type
assert saved() is obj and len(events) == before + 1
print('method-binding', True)

class Static:
    @staticmethod
    def __class__():
        return token
assert read(Static())() is token and Static.__class__ is type
print('static-binding', True)
class ClassMethod:
    @classmethod
    def __class__(cls):
        return cls
class ClassChild(ClassMethod):
    pass
assert read(ClassChild())() is ClassChild
assert ClassChild.__class__ is type
print('class-binding', True)

class OrdinaryOverProperty(Property):
    __class__ = 'child value'
class PropertyOverConstant(Constant):
    @property
    def __class__(self):
        return 'child property'
assert read(OrdinaryOverProperty()) == 'child value'
assert read(PropertyOverConstant()) == 'child property'
print('nearest-item', True)
