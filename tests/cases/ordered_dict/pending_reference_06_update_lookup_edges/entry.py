# 补充：OrderedDict更新是解包而非dict的完整pair物化；空查询及truth错误优先级。
from collections import OrderedDict as OD


def attempt(label, fn):
    try:
        print(label, 'OK', repr(fn()))
    except Exception as exc:
        print(label, 'ERROR', type(exc).__name__, str(exc), repr(exc.args))


class Pair:
    def __iter__(self):
        print('pair_iter')
        for value in ['x', 1, 2, 3]:
            print('pair_next', value)
            yield value
    def __len__(self):
        print('MUST_NOT_LEN')
        return 4


attempt('long_pair', lambda: OD([Pair()]))
attempt('short_pair', lambda: OD([('x',)]))
attempt('noniter_pair', lambda: OD([1]))

class KeysFail:
    def keys(self):
        print('keys_call_failed')
        raise AttributeError('keys invocation failed')
    def __iter__(self):
        print('MUST_NOT_FALLBACK')
        yield ('x', 1)

attempt('keys_call_error', lambda: OD(KeysFail()))

class MutableMapping:
    def __init__(self):
        self.names = ['a', 'b']
    def keys(self):
        return self.names
    def __getitem__(self, key):
        print('mutable_get', key)
        if key == 'a':
            self.names.append('c')
        return key

attempt('mutable_keys', lambda: OD(MutableMapping()))
for filled in (False, True):
    d = OD(a=1) if filled else OD()
    for name, fn in (
        ('item', lambda: d[[]]),
        ('get', lambda: d.get([], 'default')),
        ('contains', lambda: [] in d),
        ('pop', lambda: d.pop([], 'default')),
        ('setdefault', lambda: d.setdefault([], 2)),
        ('move', lambda: d.move_to_end([])),
        ('delete', lambda: d.__delitem__([])),
    ):
        attempt(str(filled) + '_' + name, fn)


error = RuntimeError('truth failure')

class BadTruth:
    def __bool__(self):
        print('bad_truth')
        raise error

for label, fn in (
    ('move_empty_badtruth', lambda: OD().move_to_end('x', BadTruth())),
    ('move_unhashable_badtruth', lambda: OD(a=1).move_to_end([], BadTruth())),
    ('pop_empty_badtruth', lambda: OD().popitem(BadTruth())),
    ('invalid_keyword_badtruth', lambda: OD(a=1).move_to_end('a', BadTruth(), bad=1)),
):
    try:
        fn()
    except Exception as exc:
        print(label, exc is error, type(exc).__name__, str(exc))


d = OD(a=1, b=2)

class MutatingTruth:
    def __bool__(self):
        d['c'] = 3
        print('truth_mutation', list(d))
        return True

print('truth_before_pop', d.popitem(MutatingTruth()), list(d))

class Key:
    def __init__(self, name):
        self.name = name
    def __hash__(self):
        print('key_hash', self.name)
        return 42
    def __eq__(self, other):
        print('key_eq', self.name, other.name)
        return True
    def __repr__(self):
        return 'Key(' + self.name + ')'

key = Key('first')
other = Key('equal')
d = OD()
d[key] = 'value'
d[other] = 'new'
print('same_key_identity', next(iter(d)) is key, list(d.values()))
d.move_to_end(other, False)
print('after_equal_move', next(iter(d)) is key)
print('DONE 06_update_lookup_edges')
