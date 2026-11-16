# ryupy: pending
# 完整原生 helper 改绑正例保留；缺少私有导出是独立边界，不删读取/恢复来假装整例通过。
import typing as t
CV = t.ClassVar
warm = CV[int]
old_check = t._type_check
calls = []

def changed(arg, message, *args, **kwargs):
    calls.append((arg, message))
    return float

t._type_check = changed
assert CV[int] is warm and calls == []
cold = CV[bytes]
assert cold.__args__ == (float,)
assert calls == [(bytes, 'typing.ClassVar accepts only single type.')]
assert CV[bytes] is cold and len(calls) == 1
t._type_check = old_check
assert CV[bytes] is cold
print('cold-type-check', calls, repr(cold))

old_alias = t._GenericAlias
t._GenericAlias = None
assert CV[int] is warm
try:
    CV[complex]
except TypeError as error:
    print('cold-alias-helper', str(error))
else:
    assert False
t._GenericAlias = old_alias
assert CV[complex].__args__ == (complex,)

old_collect = t._collect_parameters
collect_calls = []
def collect(args, *more, **kwargs):
    collect_calls.append(args)
    return ()

t._collect_parameters = collect
T = t.TypeVar('T')
changed_parameters = CV[T]
assert changed_parameters.__args__ == (T,)
assert changed_parameters.__parameters__ == ()
assert collect_calls == [(T,)]
t._collect_parameters = old_collect
assert CV[T] is changed_parameters
print('cold-collect', collect_calls, changed_parameters.__parameters__)

# _tp_cache 对函数体 TypeError 也会重跑，不仅处理原始 key 不可哈希。
retries = []
def reject(arg, message, *args, **kwargs):
    retries.append(arg)
    raise TypeError('callback rejected')

t._type_check = reject
try:
    CV[dict]
except TypeError as error:
    print('type-error-retry', str(error), retries)
else:
    assert False
assert retries == [dict, dict]
t._type_check = old_check
