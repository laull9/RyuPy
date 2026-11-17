# 独立mapping布局：动态keys属性、非可迭代错误、异常身份、unhashable顺序和yield中途字典状态。
events = []
class Catalog:
    def __init__(self):
        self.keys = lambda: ['right', 'left', 'right']
    def __getitem__(self, key):
        events.append('get:' + key)
        return len(events)
print({**Catalog()})
print(events)
class BadKeys:
    def keys(self):
        return 4
try:
    print({**BadKeys()})
except TypeError as error:
    print('bad-keys', str(error))
marker = TypeError('keys body')
class FailedKeys:
    def keys(self):
        raise marker
try:
    print({**FailedKeys()})
except TypeError as error:
    print('keys-body', error is marker)
iterator_marker = TypeError('iterator body')
class KeySequence:
    def __iter__(self):
        raise iterator_marker
class FailedIterator:
    def keys(self):
        return KeySequence()
try:
    print({**FailedIterator()})
except TypeError as error:
    print('iterator', error is iterator_marker, str(error))
class Unhashable:
    def keys(self):
        return [['not-hashable']]
    def __getitem__(self, key):
        print('unhashable get first')
        return 7
try:
    print({**Unhashable()})
except TypeError as error:
    print('unhashable', str(error))
class NoCall:
    keys = None
try:
    print({**NoCall()})
except TypeError as error:
    print('no-call', str(error))

def key(label, value):
    events.append(label)
    return value
def suspended():
    result = {key('key-before-yield', 'head'): (yield 'first-value'),
              **(yield 'mapping'), key('key-after-merge', 'tail'): (yield 'last-value')}
    yield list(result.items())
events.clear()
sequence = suspended()
print(next(sequence), events)
print(sequence.send(1), events)
print(sequence.send(Catalog()), events)
print(sequence.send(2), events)
