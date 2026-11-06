# 等价但非同一别名作为原始key时必须结构命中；与raw键区分不是引用键。
import typing as t
T = t.TypeVar('T')
a = t.Union[T, int]
b = t.Union[int, T]
assert a is not b and a == b
outer = t.Optional[a]
assert t.Optional[b] is outer
# 外项淘汰不代表结果新建：内Union尚热时，Optional重建应返回它。
keys = [t.TypeVar('K') for i in range(65)]
aliases = [t.Optional[key] for key in keys[:64]]
assert t.Union[keys[0], type(None)] is aliases[0]
t.Optional[keys[64]]
assert t.Optional[keys[0]] is aliases[0]
print('equivalent-key-inner-retained', True)
