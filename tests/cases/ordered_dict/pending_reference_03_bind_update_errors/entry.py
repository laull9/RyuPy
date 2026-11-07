# CPython 3.12.13 OrderedDict 参数绑定、truth调用、流式更新及异常身份的完整参考程序。
from collections import OrderedDict as OD


def attempt(label, fn):
    try:
        result = fn()
        print(label, 'OK', repr(result))
    except Exception as exc:
        print(label, 'ERROR', type(exc).__name__, str(exc), repr(exc.args))


class Truth:
    def __init__(self, value, label):
        self.value = value
        self.label = label
    def __bool__(self):
        print('truth', self.label)
        return self.value


for last in (True, False, 1, 0, None, [], [1]):
    d = OD(a=1, b=2)
    attempt('move_' + repr(last), lambda: d.move_to_end('a', last))
    print('state', list(d.items()))
    attempt('pop_' + repr(last), lambda: d.popitem(last))
for kwargs in ({'key': 'a'}, {'key': 'a', 'last': False}, {'last': False}, {'other': 1}):
    d = OD(a=1, b=2)
    attempt('move_kwargs_' + repr(kwargs), lambda: d.move_to_end(**kwargs))
for name, fn in (
    ('move_missing', lambda: OD(a=1).move_to_end('missing', Truth(False, 'move-missing'))),
    ('move_existing', lambda: OD(a=1, b=2).move_to_end('a', Truth(False, 'move-existing'))),
    ('pop_empty', lambda: OD().popitem(Truth(False, 'pop-empty'))),
    ('pop_existing', lambda: OD(a=1).popitem(Truth(False, 'pop-existing'))),
    ('move_unhashable', lambda: OD().move_to_end([])),
    ('pop_unhashable_empty', lambda: OD().pop([], 'default')),
    ('pop_unhashable_filled', lambda: OD(a=1).pop([], 'default')),
    ('empty_popitem', lambda: OD().popitem()),
    ('move_many', lambda: OD().move_to_end('a', True, False)),
    ('popitem_many', lambda: OD().popitem(True, False)),
    ('root_receiver', lambda: OD.move_to_end({}, 'a')),
    ('constructor_many', lambda: OD([], [])),
    ('update_many', lambda: OD().update([], [])),
):
    attempt(name, fn)

shared_error = RuntimeError('source failed')
d = OD(seed=0)

def source():
    print('source_yield_a')
    yield ('a', 1)
    print('source_after_a', list(d.items()))
    raise shared_error

try:
    d.update(source(), keyword=9)
except RuntimeError as exc:
    print('update_error', exc is shared_error, exc.__cause__, exc.__context__, list(d.items()))


def malformed():
    print('pair0')
    yield ('ok', 1)
    print('pair1')
    yield ('bad', 2, 3)
    print('MUST_NOT_CONSUME')
    yield ('late', 4)

d = OD()
attempt('malformed', lambda: d.update(malformed(), latekw=5))
print('malformed_prefix', list(d.items()))


class Keys:
    def __iter__(self):
        print('keys_iter')
        yield 'a'
        print('keys_after_a')
        yield 'b'
        print('keys_after_b')


class Mapping:
    def keys(self):
        print('mapping_keys')
        return Keys()
    def __getitem__(self, key):
        print('mapping_get', key)
        return key.upper()
    def __iter__(self):
        raise AssertionError('mapping must use keys')

d = OD()
d.update(Mapping(), z='Z')
print('mapping_result', list(d.items()))


class InnerTypeError:
    def __iter__(self):
        print('inner_iter')
        raise TypeError('inner type failure')


class BadKeys:
    @property
    def keys(self):
        raise AttributeError('no keys here')
    def __iter__(self):
        yield ('fallback', 1)

attempt('inner_type_error', lambda: OD([InnerTypeError()]))
attempt('keys_attribute_error', lambda: OD(BadKeys()))
print('DONE 03_bind_update_errors')
