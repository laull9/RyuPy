# ryupy: pending
# 不要求先读取私有 helper：写入后只查已暖键，命中不得消费冷函数体全局。
import typing as t
CV = t.ClassVar
T = t.TypeVar('Warm')
alias = CV[T]
arguments = alias.__args__
parameters = alias.__parameters__
native_key = list[int]
native_alias = CV[native_key]
calls = []

def forbidden(*args, **kwargs):
    calls.append('unexpected')
    raise AssertionError('warm lookup consumed helper')

t._type_check = forbidden
assert CV[T] is alias
assert calls == []
t._collect_parameters = forbidden
assert CV[T] is alias
assert alias.__args__ is arguments and alias.__parameters__ is parameters
t._GenericAlias = None
assert CV[T] is alias
assert CV[list[int]] is native_alias
assert alias.__origin__ is CV
assert alias.__args__ is arguments and alias.__parameters__ is parameters
assert calls == []
print('warm-helpers-and-native-key', True)
