# ClassVar=list后暖键保留，新raw形状用真实List alias触发冷错误。
import typing as t
# 原 16 使用 TypeVar，ClassVar=list 不会令其非法。这里使用 origin=list 的真实 alias。
key=t.List[int]
root=t.Tuple
a=root[key]
t.ClassVar=list
assert root[key] is a
try:
    root[(key,)]
except TypeError as e:
    print('classvar-new-raw-cold',str(e))
else:
    assert False
