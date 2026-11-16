# ryupy: pending
# owner唯一显式instance模块；consumer必须保持静态默认域，真实共享字典不能用C#替身代替。
import owner
import consumer

shared = owner.shared
canonical = owner.original
events = []


def first(value):
    events.append('first')
    return ['first', value]


def second(value):
    events.append('second')
    return ['second', value]


def rebind():
    events.append('argument')
    shared['KeyError'] = second
    return 'value'


assert consumer.save() is canonical
try:
    shared['KeyError'] = first
    assert consumer.call('initial') == ['first', 'initial']
    saved = consumer.save()
    shared['KeyError'] = second
    assert saved is first and saved('saved') == ['first', 'saved']
    shared['KeyError'] = first
    events.clear()
    assert consumer.evaluate(rebind) == ['first', 'value']
    assert events == ['argument', 'first']
    assert consumer.call('next') == ['second', 'next']
    print('default dictionary real binding alias and callee capture')

    shared['KeyError'] = None
    events.clear()
    try:
        consumer.evaluate(rebind)
    except TypeError as caught:
        assert str(caught) == "'NoneType' object is not callable" and events == ['argument']
    else:
        raise AssertionError('None binding was replaced')
    del shared['KeyError']
    events.clear()
    try:
        consumer.evaluate(rebind)
    except NameError as caught:
        assert str(caught) == "name 'KeyError' is not defined" and events == []
    else:
        raise AssertionError('missing builtin was restored or ignored')
    print('default lookup missing before args and noncallable after args')
finally:
    shared['KeyError'] = canonical

assert consumer.save() is canonical
assert owner.old.__builtins__ is owner.first
assert owner.fresh.__builtins__ is owner.second
assert owner.old() == ['owner-first', 'owner']
assert owner.fresh() == ['owner-second', 'owner']
owner.first['KeyError'] = owner.second_error
assert owner.old() == ['owner-second', 'owner']
owner.KeyError = first
assert owner.old() == ['first', 'owner']
del owner.KeyError
assert owner.old() == ['owner-second', 'owner']
print('captured instance builtins and real globals preserved')
