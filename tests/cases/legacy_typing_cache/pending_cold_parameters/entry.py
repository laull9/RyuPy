# 参数描述符在冷建时读取，暖命中和元数据反射不重新采集；新raw形状才产生新快照。
import typing as t
T = t.TypeVar('T')
U = t.TypeVar('U')
log = []
class Key:
    def __init__(self):
        self.current = (T,)
    @property
    def __parameters__(self):
        log.append('parameters')
        return self.current
    def __repr__(self):
        return 'Key()'
k = Key()
a = t.List[k]
print('cold-collection', log)
log.clear()
k.current = (U,)
assert a.__parameters__ == (T,)
assert t.List[k] is a
assert log == []
b = t.List[(k,)]
assert a is not b and a == b
assert b.__parameters__ == (U,)
print('new-raw-collection', log)
log.clear()
assert b.__parameters__ is b.__parameters__
assert a.__parameters__ == (T,) and log == []
print('stored-parameter-snapshots', True)
