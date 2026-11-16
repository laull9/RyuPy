# 两次同模板定义的身份与方法环境独立，公开__name__允许改变而代码归属不变。
import owner

first = owner.factory(3)
print(first.__module__, first.__qualname__)
owner.__name__ = 'renamed owner'
__name__ = 'unrelated caller'
second = owner.factory(7)
print(second.__module__, second.__qualname__)
print(first is not second, first().read()[0], second().read()[0])
print(first().read()[2] is first, second().read()[2] is second)
owner.__name__ = 17
third = owner.factory(11)
print(third.__module__, third().read()[0])
del owner.__name__
fourth = owner.factory(13)
print(fourth.__module__, fourth().read()[0])
