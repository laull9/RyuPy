# dict 派生协议：普通语法调用覆盖，显式根方法绕过；复制按真实iter槽选择mapping协议。
class Ordered(dict):
    def __setitem__(self, key, value):
        print('set', key)
        dict.__setitem__(self, key, value)
        return 42
    def __delitem__(self, key):
        print('del', key)
        dict.__delitem__(self, key)
    def __iter__(self):
        print('iter')
        return iter(sorted(dict.keys(self)))
    def __reversed__(self):
        print('reversed')
        return ['custom']
    def keys(self):
        print('keys')
        return sorted(dict.keys(self))
value = Ordered({'b': 2, 'a': 1})
value['c'] = 3
del value['b']
print(list(value), reversed(value))
print(list(dict.__iter__(value)), list(dict.__reversed__(value)))
dict.__setitem__(value, 'z', 9)
dict.__delitem__(value, 'z')
print(dict(value), value.copy(), value | {'d': 4}, {'d': 4} | value)
target = {}
target.update(value)
print(target)
def consume(**kwargs):
    print(kwargs)
consume(**value)
print(Ordered.fromkeys(['b', 'a'], 7))
class Left(dict):
    def __or__(self, other):
        print('left or')
        return NotImplemented
    def __ror__(self, other):
        print('left ror')
        return 'left reflected'
    def __ior__(self, other):
        print('left ior')
        return NotImplemented
class Right(Left):
    def __ror__(self, other):
        print('right ror')
        return 'right reflected'
print(Left() | Right())
x = Left()
x |= Right()
print(x)
class Same(dict):
    def __or__(self, other):
        print('same or')
        return NotImplemented
    def __ror__(self, other):
        print('must not same ror')
        return 1
try:
    Same() | Same()
except TypeError:
    print('same type error')
class Raw(dict):
    __iter__ = dict.__iter__
    def keys(self):
        print('must not keys')
        return []
print(dict(Raw(a=1)))
class Bad(dict):
    def __iter__(self):
        return []
try:
    iter(Bad())
except TypeError:
    print('bad iterator')
# 继承同一反射槽不抢父类正向槽；原位根返回同一对象。
class Parent(dict):
    def __ror__(self, other):
        print('inherited reflected')
        return 'reflected'
class Child(Parent):
    pass
print(Parent(a=1) | Child(b=2))
plain = Child(a=1)
alias = plain
plain |= {'b': 2}
print(plain is alias, plain)
# 已发布类替换/删除槽，普通分派不能使用过期缓存。
class Late(dict):
    pass
late = Late(a=1)
def late_iter(self):
    return iter(['changed'])
Late.__iter__ = late_iter
print(list(late))
del Late.__iter__
print(list(late))
# 显式根别名可跨槽绑定，但仍使用根语义与参数验证。
class ReverseAlias(dict):
    __iter__ = dict.__reversed__
print(list(ReverseAlias(a=1, b=2)))
from types import MappingProxyType
print(MappingProxyType(Left()) | Right())
try:
    MappingProxyType(Same()) | Same()
except TypeError:
    print('proxy same type error')
