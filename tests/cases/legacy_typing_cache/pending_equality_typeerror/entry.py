# 同桶等价比较抛TypeError时回到未缓存body；异常不代表可吞掉未知回调。
import typing as t
log = []
class Key:
    def __init__(self, name):
        self.name = name
    def __hash__(self):
        log.append('hash:' + self.name)
        return 9001
    def __eq__(self, other):
        if isinstance(other, Key):
            log.append('eq:' + self.name + ':' + other.name)
            raise TypeError('equality marker')
        return False
    def __repr__(self):
        return 'Key(' + self.name + ')'
a = Key('a')
b = Key('b')
first = t.List[a]
log.clear()
second = t.List[b]
assert second.__args__[0] is b
print('equality-fallback', log, repr(second))
log.clear()
third = t.List[b]
assert second is not third
print('not-cached-after-equality-error', log)
log.clear()
assert t.List[a] is first
print('original-entry-preserved', log)
