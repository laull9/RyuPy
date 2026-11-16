# 完整helper读取与回调顺序：所有prefix先检查，之后读取sentinel，空参数仍收集。
import typing as t
root=t.Tuple
original_check=t._type_check
original_collect=t._collect_parameters
log=[]
def checking(value,message):
    log.append(('check',repr(value),message))
    return original_check(value,message)
def collecting(values):
    log.append(('collect',tuple(repr(x) for x in values)))
    return original_collect(values)
t._type_check=checking
t._collect_parameters=collecting
a=root[int,str,...]
print('trailing-prefix-order',log)
assert [x[0] for x in log]==['check','check','collect']
assert log[0][2]=='Tuple[t, ...]: t must be a type.'
log.clear()
b=root[()]
assert [x[0] for x in log]==['collect']
print('empty-order',log)
log.clear()
c=root[Ellipsis]
assert [x[0] for x in log]==['check','collect']
assert log[0][2]=='Tuple[t0, t1, ...]: each t must be a type.'
print('single-ellipsis-order',log)
# prefix _type_check 可改变尾 sentinel；尾特殊处理在前缀全部检查完成后才读。
marker=object()
def replacing(value,message):
    t._TypingEllipsis=marker
    return original_check(value,message)
t._type_check=replacing
c=root[t.TypeVar('Changing'),...]
assert c.__args__[-1] is Ellipsis
print('trailing-sentinel-read-after-prefix-check',True)
