# 动态参数描述符只在真正cold采集，旧alias元数据是稳定快照。
import typing as t
T=t.TypeVar('T')
U=t.TypeVar('U')
log=[]
class Key:
    current=(T,)
    @property
    def __parameters__(self): log.append('parameters'); return self.current
k=Key()
a=t.Tuple[k]
assert log==['parameters']
log.clear()
k.current=(U,)
assert t.Tuple[k] is a and a.__parameters__==(T,) and log==[]
b=t.Tuple[(k,)]
assert a is not b and b.__parameters__==(U,) and log==['parameters']
print('cold-snapshot-warm-retention-new-raw',True)
