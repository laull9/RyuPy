# 现有guard的宽回退与实体身份，未引入新产品协议。
def increment(d, key):
    d[key] = d.get(key, 0) + 1

first = tuple(['ann', 'buy'])
later = tuple(['ann', 'buy'])
d = {}
alias = d
increment(d, first)
one = 1
print('first', next(iter(d)) is first, d[first] is one, alias is d)
increment(d, later)
print('equal', next(iter(d)) is first, d[first])
del d[first]
increment(d, later)
print('replace', next(iter(alias)) is later)
d.clear()
increment(d, first)
print('clear', next(iter(alias)) is first)
for value in [True, 1.5, None, 9223372036854775807]:
    d[first] = value
    try:
        increment(d, later)
        print('value', d[first], type(d[first]).__name__, next(iter(d)) is first)
    except TypeError:
        print('type-error', d[first] is value)
d[5] = 7
increment(d, first)
print('mixed', d[5], d[first], next(iter(d)) is first)
big = {}
for i in range(300):
    increment(big, (str(i), 'buy'))
big[('external', 'buy')] = 0
increment(big, ('0', 'buy'))
print('budget', len(big), big[('0', 'buy')])
big.clear()
increment(big, first)
print('recover', len(big), next(iter(big)) is first)
for limit in [256, 257]:
    domain = {}
    for i in range(limit):
        domain[(str(i), 'buy')] = 0
    increment(domain, ('0', 'buy'))
    del domain[(str(limit - 1), 'buy')]
    domain[('replacement', 'buy')] = 0
    increment(domain, ('0', 'buy'))
    print('slots', limit, len(domain), domain[('0', 'buy')])
    domain.clear()
    increment(domain, first)
    print('slots-clear', next(iter(domain)) is first)
cache = {}
increment(cache, first)
cache[first] = 255
before = cache[first]
increment(cache, later)
print('cache-value', before, cache[first], next(iter(cache)) is first)
live = {}
second = ('bob', 'buy')
third = ('cyd', 'buy')
increment(live, first)
increment(live, second)
iterator = iter(live)
print('iter-first', next(iterator) is first)
increment(live, first)
print('iter-update', next(iterator) is second, live[first])
iterator = iter(live)
next(iterator)
increment(live, third)
try:
    next(iterator)
except RuntimeError:
    print('iter-insert-invalid')
iterator = iter(live)
next(iterator)
live.clear()
try:
    next(iterator)
except RuntimeError:
    print('iter-clear-invalid')

events = []
class ClearReturn:
    def __add__(self, other):
        events.append('return')
        d.clear()
        d[later] = 99
        return 7
class ClearRaise:
    def __add__(self, other):
        events.append('raise')
        d.clear()
        d[later] = 99
        raise ValueError('after-clear')
d = {first: ClearReturn()}
alias = d
increment(d, first)
print('callback-clear', alias is d, d[first], next(iter(d)) is later, events)
d = {first: ClearRaise()}
alias = d
try:
    increment(d, first)
except ValueError as error:
    print('callback-error', str(error), alias is d, d[first], next(iter(d)) is later, events)
