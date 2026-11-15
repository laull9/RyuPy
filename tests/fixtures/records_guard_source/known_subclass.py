def increment(counts, key):
    counts[key] = counts.get(key, 0) + 1


class Custom(dict):
    def get(self, key, default=None):
        print('custom-get', key[0], key[1])
        return 5

    def __setitem__(self, key, value):
        print('custom-set', value)
        dict.__setitem__(self, key, value)


key = ('ann', 'buy')
d = Custom()
try:
    increment(d, key)
    print('custom-result', d[key], next(iter(d)) is key)
except NotImplementedError as error:
    print('known-rejection', str(error), len(d))
