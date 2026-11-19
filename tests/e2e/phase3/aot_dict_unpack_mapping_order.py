# 字典**仅消费mapping，连续普通项分块求值后合并，keys全取完再逐个getitem并即时覆盖。
events = []
def touch(label, value):
    events.append(label)
    return value
class Ledger:
    def __init__(self, entries):
        self.entries = entries
    def keys(self):
        events.append('ledger.keys')
        for key, value in self.entries:
            events.append('ledger.key:' + key)
            yield key
    def __getitem__(self, key):
        events.append('ledger.get:' + key)
        for candidate, value in self.entries:
            if candidate == key:
                return value
        raise KeyError(key)
first = Ledger([('a', 10), ('b', 20), ('a', 30)])
second = Ledger([('c', 40), ('b', 50)])
result = {touch('key0', 'z'): touch('value0', 0), **touch('source1', first),
          touch('key1', 'a'): touch('value1', 60), **touch('source2', second),
          touch('key2', 'tail'): touch('value2', 70)}
print(list(result.items()))
print(events)
print({**{}, **{'a': 1}, **{'a': 2, 'b': 3}})
for bad in [None, 3, [], [('a', 1)], iter([('b', 2)])]:
    try:
        print({**bad})
    except TypeError as error:
        print(type(bad).__name__, str(error))

class Changing:
    def keys(self):
        events.append('changing.keys')
        return ['same', 'later']
    def __getitem__(self, key):
        events.append('changing.get:' + key)
        if key == 'same':
            second.entries = [('changed', 80)]
            return {**{'nested': 9}}['nested']
        return 11
events.clear()
print({**Changing(), **second})
print(events)

old = {'old': 1}
marker = LookupError('getitem marker')
class Failed(Ledger):
    def __getitem__(self, key):
        events.append('failed:' + key)
        raise marker
try:
    old = {'prefix': touch('before-failure', 5), **Failed([('x', 1)])}
except LookupError as error:
    print('failure', error is marker, old, events[-4:])
