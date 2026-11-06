# tuple子类hash/迭代未知协议必须保留CP原行为，不能当exact tuple无条件展开。
import typing as t
log = []
class Params(tuple):
    def __hash__(self):
        log.append('hash')
        return tuple.__hash__(self)
    def __iter__(self):
        log.append('iter')
        return tuple.__iter__(self)
T = t.TypeVar('T')
p = Params((T,))
a = t.List[p]
print('tuple-subclass-cold', log, repr(a))
log.clear()
assert t.List[p] is a
print('tuple-subclass-hot', log)
