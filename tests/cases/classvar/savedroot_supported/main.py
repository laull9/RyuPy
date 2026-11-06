# ryupy: pending
# 独立保存根支持域：全新 TypeVar 保证冷键，不依赖 complex 或 Python 模块属性删除。
import typing as t
CV = t.ClassVar
U = t.Union
O = t.Optional
warm_key = t.TypeVar('Warm')
cold_key = t.TypeVar('Cold')
optional_key = t.TypeVar('OptionalCold')
warm = CV[warm_key]
inner = CV[int]
t.ClassVar = 'changed'
assert CV[warm_key] is warm
cold = CV[cold_key]
assert cold.__origin__ is CV and t.get_origin(cold) is CV
nested = CV[inner]
assert nested.__args__ == (inner,)
assert O[optional_key] is U[optional_key, type(None)]
t.ClassVar = CV
assert CV[inner] is nested
assert CV[cold_key] is cold and CV[warm_key] is warm
try:
    CV[CV[bytes]]
except TypeError as error:
    print('restored-cold-rejection', str(error))
else:
    assert False
print('saved-root-supported', repr(cold), repr(nested))

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
