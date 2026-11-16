# ryupy: pending
# 工厂绑定、名称规则、逐str/迭代交错、rename重复真值及显式module引用。
# 既有33项参考仅作设计依据；此源码尚未执行，实际CP-first输出才是本应用oracle。
from collections import namedtuple


def expect_error(label, action, expected_name):
    try:
        action()
    except Exception as error:
        assert type(error).__name__ == expected_name
        print(label, type(error).__name__, str(error))
    else:
        raise AssertionError(label + ' did not raise')


text_fields = namedtuple('TextFields', 'left, middle\t right\n', module='fixture.rules')
list_fields = namedtuple('ListFields', ['left', 'middle', 'right'], module='fixture.rules')
tuple_fields = namedtuple(typename='TupleFields', field_names=('left', 'middle', 'right'),
                         module='fixture.rules')
assert text_fields._fields == list_fields._fields == tuple_fields._fields
assert text_fields._fields == ('left', 'middle', 'right')
assert tuple(text_fields(1, 2, 3)) == (1, 2, 3)
assert tuple(list_fields(right=3, left=1, middle=2)) == (1, 2, 3)
assert tuple(tuple_fields(1, middle=2, right=3)) == (1, 2, 3)
print('B2.sources', text_fields._fields, list_fields._fields, tuple_fields._fields)

Empty = namedtuple('_Empty', (), defaults=[], module='fixture.rules')
assert Empty._fields == () and Empty._field_defaults == {} and tuple(Empty()) == ()
NoDefault = namedtuple('NoDefault', ['x'], defaults=(), module='fixture.rules')
NoneDefault = namedtuple('NoneDefault', ['x'], defaults=None, module='fixture.rules')
AllDefault = namedtuple('AllDefault', ['x', 'y'], defaults=(None, 3), module='fixture.rules')
assert NoDefault._field_defaults == {} and NoneDefault._field_defaults == {}
assert tuple(AllDefault()) == (None, 3)
assert list(AllDefault._field_defaults) == ['x', 'y']
print('B2.default-forms', Empty.__name__, NoDefault._field_defaults, AllDefault())

Shadow = namedtuple('Shadow', 'self cls count index match case', module='fixture.rules')
shadow = Shadow(self=1, cls=2, count=3, index=4, match=5, case=6)
assert (shadow.self, shadow.cls, shadow.count, shadow.index) == (1, 2, 3, 4)
assert Shadow._fields == ('self', 'cls', 'count', 'index', 'match', 'case')
print('B2.shadow', shadow)

Renamed = namedtuple('Renamed', ['x', 'x', '_1', 'x'], rename=True,
                     module='fixture.rules')
InvalidRenamed = namedtuple('InvalidRenamed', ['bad-name', 'class', '_private', 'ok'],
                            rename=True, module='fixture.rules')
assert Renamed._fields == ('x', '_1', '_2', '_3')
assert InvalidRenamed._fields == ('_0', '_1', '_2', 'ok')
assert tuple(Renamed(1, 2, 3, 4)) == (1, 2, 3, 4)
print('B2.rename', Renamed._fields, InvalidRenamed._fields)

expect_error('B2.typename-invalid', lambda: namedtuple('bad-name', ['x']), 'ValueError')
expect_error('B2.typename-keyword', lambda: namedtuple('class', ['x']), 'ValueError')
expect_error('B2.field-invalid', lambda: namedtuple('Bad', ['bad-name']), 'ValueError')
expect_error('B2.field-keyword', lambda: namedtuple('Bad', ['class']), 'ValueError')
expect_error('B2.field-underscore', lambda: namedtuple('Bad', ['_x']), 'ValueError')
expect_error('B2.field-duplicate', lambda: namedtuple('Bad', ['x', 'x']), 'ValueError')
expect_error('B2.name-before-default-count',
             lambda: namedtuple('Bad', ['bad-name'], defaults=[1, 2]), 'ValueError')
expect_error('B2.defaults-too-many', lambda: namedtuple('Bad', ['x'], defaults=[1, 2]),
             'TypeError')
expect_error('B2.factory-missing', lambda: namedtuple(), 'TypeError')
expect_error('B2.factory-one-missing', lambda: namedtuple('Missing'), 'TypeError')
expect_error('B2.factory-too-many', lambda: namedtuple('Bad', 'x', True), 'TypeError')
expect_error('B2.factory-duplicate', lambda: namedtuple('Bad', 'x', typename='Again'),
             'TypeError')
expect_error('B2.factory-unknown', lambda: namedtuple('Bad', 'x', extra=1), 'TypeError')


class Text:
    def __init__(self, events, label, text, failure=None):
        self.events = events
        self.label = label
        self.text = text
        self.failure = failure

    def __str__(self):
        self.events.append('str:' + self.label)
        if self.failure is not None:
            raise self.failure
        return self.text


class Finite:
    def __init__(self, events, label, values, failure=None):
        self.events = events
        self.label = label
        self.values = values
        self.failure = failure

    def __iter__(self):
        self.events.append('iter:' + self.label)
        for index, value in enumerate(self.values):
            self.events.append('yield:' + self.label + ':' + str(index))
            yield value
        if self.failure is not None:
            self.events.append('raise:' + self.label)
            raise self.failure
        self.events.append('end:' + self.label)


class Rename:
    def __init__(self, events, values):
        self.events = events
        self.values = values
        self.calls = 0

    def __bool__(self):
        self.events.append('bool:rename:' + str(self.calls))
        result = self.values[self.calls]
        self.calls += 1
        return result


def order_case(mode):
    events = []
    original = RuntimeError(mode + '-stop')
    field_failure = original if mode == 'fields' else None
    conversion_failure = original if mode == 'field-str' else None
    type_failure = original if mode == 'typename' else None
    default_failure = original if mode == 'defaults' else None
    first_name = 'bad-name' if mode == 'bad-field' else 'x'
    fields = Finite(events, 'fields',
                    [Text(events, 'field0', first_name, conversion_failure),
                     Text(events, 'field1', 'y')],
                    field_failure)
    typename = Text(events, 'typename', 'Ordered', type_failure)
    default_values = [7, 8, 9] if mode == 'extra-defaults' else [7]
    defaults = Finite(events, 'defaults', default_values, default_failure)
    rename = Rename(events, [False])
    outcome = None
    try:
        if mode == 'binding':
            result = namedtuple(typename, fields, rename=rename, defaults=defaults,
                                unexpected=True, module='fixture.rules')
        else:
            result = namedtuple(typename, fields, rename=rename, defaults=defaults,
                                module='fixture.rules')
        assert mode == 'ok'
        assert result._fields == ('x', 'y') and result._field_defaults == {'y': 7}
        assert tuple(result(1)) == (1, 7)
        outcome = ('ok', result._fields)
    except Exception as error:
        assert mode != 'ok'
        if mode in ('fields', 'field-str', 'typename', 'defaults'):
            assert error is original
        elif mode == 'bad-field':
            assert type(error) is ValueError
        else:
            assert mode in ('binding', 'extra-defaults') and type(error) is TypeError
        outcome = (type(error).__name__, str(error))

    field_prefix = ['iter:fields', 'yield:fields:0', 'str:field0',
                    'yield:fields:1', 'str:field1']
    through_type = field_prefix + ['end:fields', 'str:typename']
    through_rename = through_type + ['bool:rename:0']
    if mode == 'binding':
        assert events == []
    elif mode == 'field-str':
        assert events == ['iter:fields', 'yield:fields:0', 'str:field0']
    elif mode == 'fields':
        assert events == field_prefix + ['raise:fields']
    elif mode == 'typename':
        assert events == through_type
    elif mode == 'bad-field':
        assert events == through_rename
    elif mode == 'defaults':
        assert events == through_rename + ['iter:defaults', 'yield:defaults:0', 'raise:defaults']
    elif mode == 'extra-defaults':
        assert events == through_rename + ['iter:defaults', 'yield:defaults:0',
                                          'yield:defaults:1', 'yield:defaults:2', 'end:defaults']
    else:
        assert events == through_rename + ['iter:defaults', 'yield:defaults:0', 'end:defaults']
    print('B2.order-' + mode, events, outcome)


for mode in ('ok', 'bad-field', 'fields', 'field-str', 'typename', 'defaults',
             'extra-defaults', 'binding'):
    order_case(mode)

# 原始rename对象保留到后面的下划线校验；稳定True也须被重复求真值。
rename_events = []
Stable = namedtuple('Stable', ['x', 'x', '_1', 'x'],
                    rename=Rename(rename_events, [True, True, True, True]),
                    module='fixture.rules')
assert Stable._fields == ('x', '_1', '_2', '_3')
assert rename_events == ['bool:rename:0', 'bool:rename:1', 'bool:rename:2', 'bool:rename:3']
print('B2.rename-repeated', Stable._fields, rename_events)
changed_events = []
expect_error('B2.rename-changed',
             lambda: namedtuple('Changed', ['bad-name'],
                                rename=Rename(changed_events, [True, False]),
                                module='fixture.rules'), 'ValueError')
assert changed_events == ['bool:rename:0', 'bool:rename:1']
print('B2.rename-changed-events', changed_events)


class Poison:
    def __bool__(self):
        raise AssertionError('explicit module must not request truth')

    def __str__(self):
        raise AssertionError('explicit module must not request text')


module_values = ['custom.module', '', 0, False, [], Poison()]
for index, value in enumerate(module_values):
    result = namedtuple('Explicit', ['x'], module=value)
    assert result.__module__ is value and result(1).__module__ is value
    print('B2.module-' + str(index), result.__module__ is value)
