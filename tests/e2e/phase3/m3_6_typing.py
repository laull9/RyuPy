# typing仅运行时元数据；不执行前向引用，也不把注解当作调用时强制检查。
from typing import Any, Optional, Union, List, Dict, Tuple, Callable, get_origin, get_args, cast, TYPE_CHECKING

print(TYPE_CHECKING, repr(Any))
print(Optional[int], Union[str, int], List[int], Dict[str, List[int]])
print(Optional[int] == Union[int, None], Union[int, str] == Union[str, int])
print(get_origin(List[int]) is list, get_origin(Optional[int]) is Union)
print(get_args(Dict[str, int]) == (str, int), get_args(int), get_origin(int))
print(Tuple[int, str], Callable[[int, str], bool])
value = []
print(cast(List[int], value) is value)

def annotated(value: Optional[int]) -> List[int]:
    return [value]
print(annotated.__annotations__["value"] == Optional[int])
print(annotated.__annotations__["return"] == List[int])
print(annotated("unchecked"))
try:
    List[int]([1, 2])
except TypeError:
    print("typing construction rejected")
try:
    isinstance([], List[int])
except TypeError:
    print("parameterized check rejected")
from collections.abc import Iterable
print(get_origin(Iterable[int]) is Iterable, get_args(Iterable[int]) == (int,))
print(get_origin(list[int]) is list, get_args(list[int]) == (int,))
print(List[int] is List[int], Optional[int] is Optional[int])
print(Optional[int] is Union[int, None], get_origin(Union))
print(list[int]([1, 2]))
