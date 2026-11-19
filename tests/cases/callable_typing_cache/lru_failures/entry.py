# 使用fresh TypeVar填满独立池；不读私有_caches，也不清缓存。
import typing as t
C=t.Callable
keys=[t.TypeVar('K'+str(i)) for i in range(129)]
old=[C[[key],int] for key in keys[:128]]
for i in range(140):
    try: C[[keys[i%128]],()]
    except TypeError as error:
        assert str(error)=='Callable[args, result]: result must be a type. Got ().'
    else: assert False
    try: C[[keys[i%128]],t.Generic]
    except TypeError as error:
        assert str(error)=="Plain <class 'typing.Generic'> is not valid as type argument"
    else: assert False
# 遍历全部原身份可以发现失败路径偷偷挤占容量；遍历后0仍是最旧。
for i in range(128):
    assert C[(keys[i],),int] is old[i]
assert C[[keys[0]],int] is old[0]
C[[keys[128]],int]
assert C[(keys[0],),int] is old[0]
rebuilt=C[[keys[1]],int]
assert rebuilt is not old[1] and rebuilt==old[1] and hash(rebuilt)==hash(old[1])
assert C[(keys[1],),int] is rebuilt
print('128 entries; failures no eviction; hit touches; fresh rebuild: ok')
