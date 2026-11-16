# 保存的根和模块导出是两个对象；热Optional先于导出读取。
import typing as t
U = t.Union
O = t.Optional
key = t.TypeVar('Warm')
a = O[key]
t.Union = 'changed'
assert O[key] is a
cold = t.TypeVar('Cold')
b = U[cold, int]
assert b.__origin__ is U
try:
    O[bytes]
except TypeError as error:
    print('cold-current-union', str(error))
else:
    assert False
t.Union = U
assert O[key] is a
assert b is U[cold, int]
print('saved-root', True)
