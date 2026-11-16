# ryupy: pending
# 每步属性重读、有限duck/str子类/终值身份和quote迟真值；不强转用户对象。
from html import escape

steps = [('&', '&amp;'), ('<', '&lt;'), ('>', '&gt;'),
         ('"', '&quot;'), ("'", '&#x27;')]


class PropertySelf:
    @property
    def replace(self):
        events.append(('get', len(events)))
        def invoke(old, new):
            events.append(('call', old, new))
            return self
        return invoke

    def __str__(self):
        raise AssertionError('escape must not call str')


class GetattrSelf:
    def __getattr__(self, name):
        events.append(('getattr', name))
        if name != 'replace':
            raise AttributeError(name)
        return self.apply

    def apply(self, old, new):
        events.append(('call', old, new))
        return self


# 同一site交替类/布局；property与普通getattr必须均保真实查找。
def consume(value):
    return escape(value)


for kind, value in [('property', PropertySelf()), ('getattr', GetattrSelf()),
                    ('property-again', PropertySelf())]:
    events = []
    assert consume(value) is value
    assert len(events) == 10
    assert [event[0] for event in events] == [
        ('getattr' if kind == 'getattr' else 'get'), 'call'] * 5
    assert [(event[1], event[2]) for event in events if event[0] == 'call'] == steps
    print('H2.' + kind, events)


class Link:
    def __init__(self, index, following):
        self.index = index
        self.following = following

    def replace(self, old, new):
        events.append((self.index, old, new))
        return self.following


def chain(count, final):
    current = final
    for index in reversed(range(count)):
        current = Link(index, current)
    return current


for label, count, quote, final in [('false-list', 3, False, []),
                                  ('false-none', 3, False, None),
                                  ('true-dict', 5, True, {}),
                                  ('true-none', 5, True, None)]:
    events = []
    result = escape(chain(count, final), quote)
    assert result is final
    assert events == [(i, steps[i][0], steps[i][1]) for i in range(count)]
    print('H2.' + label, events, result is final, type(final).__name__)


class Rebinding:
    def replace(self, old, new):
        events.append(('old', old, new))
        Rebinding.replace = replacement
        return self


def replacement(self, old, new):
    events.append(('new', old, new))
    return self


events = []
obj = Rebinding()
assert escape(obj) is obj
assert [event[0] for event in events] == ['old'] + ['new'] * 4
assert [(event[1], event[2]) for event in events] == steps
print('H2.class-rebind', events)


class InstanceOverride:
    def replace(self, old, new):
        raise AssertionError('instance attribute should shadow this method')


class Callable:
    def __call__(self, old, new):
        events.append(('instance-callable', old, new))
        return current


current = InstanceOverride()
current.replace = Callable()
events = []
assert escape(current) is current
assert events == [('instance-callable', old, new) for old, new in steps]
print('H2.instance-callable', events)


class StringProperty(str):
    @property
    def replace(self):
        events.append(('subclass-get',))
        return patched_string


def patched_string(old, new):
    events.append(('subclass-call', old, new))
    return current


current = StringProperty('&')
events = []
assert escape(current) is current
assert [event[0] for event in events] == ['subclass-get', 'subclass-call'] * 5
print('H2.str-property', events)


class StringInstance(str):
    pass


current = StringInstance('&')
current.replace = patched_string
events = []
assert escape(current) is current
assert events == [('subclass-call', old, new) for old, new in steps]
print('H2.str-instance', events)


class Quote:
    def __init__(self, target, value):
        self.target = target
        self.value = value

    def __bool__(self):
        events.append(('bool',))
        self.target.replace = patched
        return self.value

    def __len__(self):
        raise AssertionError('bool takes precedence')


def patched(old, new):
    events.append(('patched', old, new))
    return current


for label, truth in [('true', True), ('false', False)]:
    current = Link(0, None)
    current.following = current
    events = []
    assert escape(current, Quote(current, truth)) is current
    expected = [(0, old, new) for old, new in steps[:3]] + [('bool',)]
    if truth:
        expected += [('patched', old, new) for old, new in steps[3:]]
    assert events == expected
    print('H2.quote-' + label, events)


class LengthQuote:
    def __len__(self):
        events.append(('len',))
        return 1


events = []
current = Link(0, None)
current.following = current
assert escape(current, LengthQuote()) is current
assert events == [(0, old, new) for old, new in steps[:3]] + [('len',)] + [
    (0, old, new) for old, new in steps[3:]]
print('H2.quote-length', events)


class DefaultQuote:
    pass


events = []
assert escape(current, DefaultQuote()) is current
assert events == [(0, old, new) for old, new in steps]
print('H2.quote-default', events)
