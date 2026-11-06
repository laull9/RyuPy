# 保存相同键也必须重新hash；等价非同一键必须执行原eq，不能暖命中跳过。
import typing as t
T = t.TypeVar('T')
key = t.Literal[17]
a = t.List[key]
original_values = t._value_and_type_iter
log = []
def values(args):
    log.append('literal-hash')
    return original_values(args)
t._value_and_type_iter = values
assert t.List[key] is a
print('hot-raw-hash', log)
t._value_and_type_iter = original_values
first = t.Set[T]
second = t.Set[(T,)]
assert first is not second and first == second
outer = t.List[first]
original_alias = t._GenericAlias
t._GenericAlias = None
assert t.List[first] is outer
try:
    t.List[second]
except TypeError as error:
    print('hot-raw-equality', str(error))
else:
    assert False
t._GenericAlias = original_alias
