# tuple派生冷建消费hash和迭代，暖命中仅hash；保留完整CP源。
import typing as t
log=[]
class Raw(tuple):
    def __hash__(self): log.append('hash'); return tuple.__hash__(self)
    def __iter__(self): log.append('iter'); return tuple.__iter__(self)
T=t.TypeVar('T')
key=Raw((T,int))
a=t.Tuple[key]
print('cold',log)
assert log==['hash','iter']
log.clear()
assert t.Tuple[key] is a
assert log==['hash']
print('warm',log)
