# ryupy: pending
# 原key物化与get/+1/set的未来差分合同；同次CP生成预期，不提交golden。
def step(counts, user, action):
    key = (user, action)
    counts[key] = counts.get(key, 0) + 1
    return key


counts = {}
first = step(counts, 'ann', 'buy')
print(first is next(iter(counts)), counts[first])
one = 1
print(id(counts[first]) == id(one))
later = step(counts, 'ann', 'buy')
print(later is next(iter(counts)), first is next(iter(counts)), counts[first])
del counts[first]
last = step(counts, 'ann', 'buy')
print(last is next(iter(counts)), first is last)
counts[last] = 9223372036854775807
step(counts, 'ann', 'buy')
print(counts[last])
counts[last] = True
step(counts, 'ann', 'buy')
print(type(counts[last]) is int, counts[last])


def observed(counts):
    key = ('ann', 'buy')
    counts[key] = counts.get(key, 0) + 1
    return locals()['key'], key


fresh = {}
a, b = observed(fresh)
print(a is b, b is next(iter(fresh)))


class Counter:
    def __add__(self, other):
        print('add', other, next(iter(counts)) is last)
        counts['side'] = 7
        return 41


counts[last] = Counter()
step(counts, 'ann', 'buy')
print(counts[last], counts['side'], next(iter(counts)) is last)


class Throwing:
    def __add__(self, other):
        print('throw', other)
        raise ValueError('counter failed')


old = Throwing()
counts[last] = old
try:
    step(counts, 'ann', 'buy')
except ValueError as error:
    print(str(error), counts[last] is old)


class Custom(dict):
    def get(self, key, default=None):
        print('custom get', key)
        return 5

    def __setitem__(self, key, value):
        print('custom set', key, value)
        super().__setitem__(key, value)


custom = Custom()
key = step(custom, 'ann', 'buy')
print(key is next(iter(custom)), custom[key])
