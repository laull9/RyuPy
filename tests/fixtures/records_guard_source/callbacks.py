def increment(counts, key):
    counts[key] = counts.get(key, 0) + 1


class Key:
    def __hash__(self):
        print('key-hash')
        return 7

    def __eq__(self, other):
        print('key-eq')
        return True


first = Key()
second = Key()
d = {'seed': 0}
increment(d, first)
increment(d, second)
print('key-value', list(d.values()))


class Receiver:
    def get(self, key, default):
        print('receiver-get', key, default)
        return 5

    def __setitem__(self, key, value):
        print('receiver-set', key, value)


increment(Receiver(), 'a')
