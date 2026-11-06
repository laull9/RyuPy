# ryupy: pending
# 真实namedtuple消费者闭合Dynamic函数/别名/闭包/生成器的caller-module来源。
# 相关函数分类必须另以实际Types/MIR结构验证，不靠源码是否有注解猜测。
from collections import namedtuple
import origin_provider as provider
from origin_provider import make_record as imported_alias

entry_name = __name__
EntryRecord = namedtuple('EntryRecord', 'value')
EntryNone = namedtuple('EntryNone', 'value', module=None)
assert EntryRecord.__module__ == EntryNone.__module__ == '__main__'
assert provider.ModuleRecord.__module__ == 'origin_provider'
print('B5.module-top', EntryRecord.__module__, EntryNone.__module__,
      provider.ModuleRecord.__module__)


def direct(value):
    return namedtuple('DirectRecord', 'value')(value)


def direct_none(value):
    return namedtuple('DirectNone', 'value', module=None)(value)


saved_direct = direct
assert direct(1).__module__ == '__main__'
assert saved_direct(2).__module__ == '__main__'
assert direct_none(3).__module__ == '__main__'
__name__ = 'entry.changed'
provider.set_name('provider.changed')
assert saved_direct(4).__module__ == '__main__'
assert imported_alias(5).__module__ == 'origin_provider'
assert provider.make_none(6).__module__ == 'origin_provider'


def late(value):
    return namedtuple('LateRecord', 'value')(value)


closure = provider.make_closure(10)
assert late(1).__module__ == 'entry.changed'
assert closure(2).__module__ == 'provider.changed' and closure(2).value == 12
__name__ = entry_name
print('B5.definition-capture', saved_direct(4).__module__, imported_alias(5).__module__,
      late(1).__module__, closure(2).__module__)

direct.__module__ = 'direct.current'
imported_alias.__module__ = 'imported.current'
closure.__module__ = 'closure.current'
assert saved_direct(1).__module__ == 'direct.current'
assert provider.make_record(1).__module__ == 'imported.current'
assert provider.invoke_callback(direct, 1).__module__ == 'direct.current'
assert closure(1).__module__ == 'closure.current'
print('B5.current-member', saved_direct(1).__module__, provider.make_record(1).__module__,
      provider.invoke_callback(direct, 1).__module__, closure(1).__module__)


def selected(value):
    return namedtuple('SelectedRecord', 'value')(value)


def replacement(value):
    return namedtuple('ReplacementRecord', 'value')(value)


old_selected = selected
selected.__module__ = 'old.before'
replacement.__module__ = 'replacement.current'


def mutate_argument():
    global selected
    selected = replacement
    old_selected.__module__ = 'old.during.argument'
    return 7


frozen = selected(mutate_argument())
assert frozen.__module__ == 'old.during.argument' and frozen.value == 7
assert selected is replacement and old_selected is not selected
assert selected(8).__module__ == 'replacement.current'
print('B5.callee-freeze', frozen.__module__, selected(8).__module__, frozen.value)

# 同一真实generator源函数引用在暂停后观察当前成员，不取next调用者模块。
stream = provider.make_stream(20)
stream.__module__ = 'generator.first'
iterator = stream()
first = next(iterator)
assert first.value == 20 and first.__module__ == 'generator.first'
stream.__module__ = 'generator.second'
provider.set_name('provider.after.suspend')
__name__ = 'caller.after.suspend'
second = next(iterator)
assert second.value == 21 and second.__module__ == 'generator.second'
assert next(iterator, 'done') == 'done'
__name__ = entry_name
print('B5.generator', first.__module__, second.__module__, first.value, second.value)

throwing = stream()
assert next(throwing).__module__ == 'generator.second'
injected = ValueError('generator-injected')
try:
    throwing.throw(injected)
except ValueError as error:
    assert error is injected and next(throwing, 'closed') == 'closed'
else:
    raise AssertionError('generator throw did not propagate')
assert direct(9).__module__ == 'direct.current'
print('B5.generator-unwind', direct(9).__module__, next(throwing, 'closed'))

for index, value in enumerate(['', 0, False, None, []]):
    direct.__module__ = value
    direct_none.__module__ = value
    assert direct(1).__module__ == '__main__'
    assert direct_none(2).__module__ == '__main__'
    print('B5.falsey-' + str(index), direct(1).__module__, direct_none(2).__module__)
del direct.__module__
assert direct(1).__module__ == '__main__'
print('B5.deleted-member', direct(1).__module__)

truth_events = []


class ModuleValue:
    def __init__(self, label, truth, failure=None):
        self.label = label
        self.truth = truth
        self.failure = failure

    def __bool__(self):
        truth_events.append('truth:' + self.label)
        if self.failure is not None:
            raise self.failure
        return self.truth

    def __str__(self):
        raise AssertionError('module should not request string conversion')


truthy = ModuleValue('yes', True)
falsey = ModuleValue('no', False)
direct.__module__ = truthy
truth_result = direct(1)
assert truth_result.__module__ is truthy and truth_events == ['truth:yes']
direct_none.__module__ = falsey
false_result = direct_none(2)
assert false_result.__module__ == '__main__'
assert truth_events == ['truth:yes', 'truth:no']
print('B5.module-truth', truth_result.__module__ is truthy,
      false_result.__module__, truth_events)

original = LookupError('module-truth-failed')
poison = ModuleValue('poison', True, original)
direct.__module__ = poison
try:
    direct(3)
except LookupError as error:
    assert error is original
else:
    raise AssertionError('module truth error did not propagate')
assert truth_events == ['truth:yes', 'truth:no', 'truth:poison']
direct.__module__ = 'after.truth.error'
assert direct(4).__module__ == 'after.truth.error'
print('B5.module-truth-error', truth_events, direct(4).__module__)


def ordered(fields, defaults=None, explicit=None):
    return namedtuple('OrderedRecord', fields, defaults=defaults, module=explicit)


class DefaultFailure:
    def __iter__(self):
        truth_events.append('defaults:raise')
        raise original


ordered.__module__ = poison
before = list(truth_events)
try:
    ordered(['bad-name'])
except ValueError as error:
    assert str(error) == "Type names and field names must be valid identifiers: 'bad-name'"
else:
    raise AssertionError('name validation did not run first')
assert truth_events == before
try:
    ordered(['x'], defaults=DefaultFailure())
except LookupError as error:
    assert error is original
else:
    raise AssertionError('defaults exception did not precede module truth')
assert truth_events == before + ['defaults:raise']
print('B5.validation-before-module', truth_events)

# 显式非None原对象包括falsey/毒化对象都绕过调用来源及真值。
for index, value in enumerate(['', 0, False, [], poison]):
    explicit_result = ordered(['x'], explicit=value)
    assert explicit_result.__module__ is value
    print('B5.explicit-' + str(index), explicit_result.__module__ is value)
assert truth_events == before + ['defaults:raise']
ordered.__module__ = 'ordered.restored'
assert ordered(['x']).__module__ == 'ordered.restored'
print('B5.restored', ordered(['x']).__module__, truth_events)
