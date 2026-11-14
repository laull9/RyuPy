# 单一审查反例：ClassVar 导出重绑不得重新验证已暖 typing.List 的原始参数。
import typing as t
key = t.List[int]
warm = t.List[key]
t.ClassVar = list
again = t.List[key]
print('warm-retained', again is warm, repr(again))
assert again is warm
