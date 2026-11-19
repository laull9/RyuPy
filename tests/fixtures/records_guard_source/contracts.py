def increment(counts, key):
    counts[key] = counts.get(key, 0) + 1


def observed(counts, key):
    counts[key] = counts.get(key, 0) + 1
    return locals()['key'] is key


first = ('ann', 'buy')
later = tuple(['ann', 'buy'])
d = {}
increment(d, first)
increment(d, later)
print('identity', d[first], next(iter(d)) is first, next(iter(d)) is later)
del d[first]
increment(d, later)
print('reinsert', d[later], next(iter(d)) is later)
d[later] = True
increment(d, first)
print('bool', d[first], type(d[first]).__name__)
d[later] = 9223372036854775807
increment(d, first)
print('overflow', d[first], type(d[first]).__name__)
print('observed', observed({}, first))


class ReplacingCounter:
    def __add__(self, other):
        print('replace-add', other, next(iter(d)) is first)
        del d[first]
        d[later] = 100
        return 41


d = {first: ReplacingCounter()}
increment(d, first)
print('replace-result', d[first], next(iter(d)) is later)


class ThrowingCounter:
    def __add__(self, other):
        print('throw-add', other)
        raise ValueError('counter failed')


old = ThrowingCounter()
d = {first: old}
try:
    increment(d, first)
except ValueError as error:
    print('exception', str(error), d[first] is old)
