# 默认目录只验证真实用户属性；完整内建dunder目录另有pending，不用过滤掩盖该边界。
class Base:
    inherited = 1
    @property
    def property_value(self):
        raise ValueError('must not execute property')
class Child(Base):
    value = 2
first = Child()
second = Child()
first.local = 3
second.other = 4
print([name for name in dir(first) if not name.startswith('_')])
print([name for name in dir(Child) if not name.startswith('_')])
del first.local
print('local' in dir(first), 'other' in dir(first))
Child.new = 5
print('new' in dir(first))
class Custom:
    def __dir__(self):
        return iter(['z', 'a', 'a'])
print(dir(Custom()))
# 返回项沿sorted的比较协议，不先强制转换字符串。
class Numbers:
    def __dir__(self):
        return [3, 1, 2]
print(dir(Numbers()))
class Bad:
    def __dir__(self):
        return [1, 'a']
try:
    dir(Bad())
except TypeError:
    print('mixed error')
class Raising:
    def __dir__(self):
        raise ValueError('directory')
try:
    dir(Raising())
except ValueError:
    print('user error')
print('__dir__' in dir(Custom))
try:
    dir(first, second)
except TypeError:
    print('arity')
try:
    dir(object=first)
except TypeError:
    print('keyword')
custom = Custom()
custom.__dir__ = lambda: ['wrong instance override']
print(dir(custom))
class PropertyDir:
    @property
    def __dir__(self):
        return lambda: ['property hook']
print(dir(PropertyDir()))
class StaticDir:
    @staticmethod
    def __dir__():
        return ('static hook',)
print(dir(StaticDir()))
class ClassDir:
    @classmethod
    def __dir__(cls):
        return [cls.__name__]
class DerivedDir(ClassDir):
    pass
print(dir(DerivedDir()))
class InvalidDir:
    __dir__ = None
try:
    dir(InvalidDir())
except TypeError:
    print('disabled hook')
class UnicodeDir:
    def __dir__(self):
        return ['𐀀', '\ue000']
print(dir(UnicodeDir()))
