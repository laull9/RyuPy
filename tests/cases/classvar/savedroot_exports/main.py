# ryupy: pending
# 模块导出重绑与保存根分离，恢复导出不应清空已建立的合法缓存结果。
import typing as t
CV = t.ClassVar
U = t.Union
O = t.Optional
warm_key = t.TypeVar('Warm')
warm = CV[warm_key]
nested = CV[int]
t.ClassVar = 'changed'
assert CV[warm_key] is warm
cold = CV[str]
assert cold.__origin__ is CV and t.get_origin(cold) is CV
# _type_check 比较当前 ClassVar 导出，故重绑后旧 ClassVar alias 可以成为参数。
allowed_nested = CV[nested]
assert allowed_nested.__args__ == (nested,)
assert allowed_nested.__origin__ is CV
assert U[bytes, float].__origin__ is U
assert O[complex] is U[complex, type(None)]
t.ClassVar = CV
assert CV[nested] is allowed_nested
assert CV[str] is cold and CV[warm_key] is warm
# 新键仍按恢复后的导出拒绝，不能因旧暖键存在而放宽整类输入。
try:
    CV[CV[bytes]]
except TypeError as error:
    print('restored-cold-rejection', str(error))
else:
    assert False
print('saved-root', repr(cold), repr(allowed_nested))

# 删除公开导出仍不影响暖命中，普通冷键真正执行函数体时才读取名称。
del t.ClassVar
assert CV[warm_key] is warm
try:
    CV[dict]
except NameError as error:
    print('deleted-cold', str(error))
else:
    assert False
t.ClassVar = CV
assert CV[dict].__origin__ is CV
print('restored-export', True)

# 当前 ClassVar 导出也参与 typing alias 的 origin 比较；原生 GenericAlias 不走该检查。
T = t.TypeVar('ReboundList')
typing_key = t.List[T]
native_key = list[T]
t.ClassVar = list
try:
    CV[typing_key]
except TypeError as error:
    print('rebound-origin-rejection', str(error))
else:
    assert False
assert CV[native_key].__args__ == (native_key,)
t.ClassVar = CV
assert CV[typing_key].__args__ == (typing_key,)
print('native-origin-not-type-check', True)
