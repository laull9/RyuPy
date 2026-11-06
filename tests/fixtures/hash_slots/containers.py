# 普通None hash槽的容器准入：预检不额外调用用户hash，保空dict.pop例外。
def operation(name, target, key):
    if name == 'dict-get':
        return target[key]
    if name == 'dict-method-get':
        return target.get(key)
    if name == 'dict-setdefault':
        return target.setdefault(key, 8)
    if name == 'dict-contains':
        return key in target
    if name == 'dict-keys-contains':
        return key in target.keys()
    if name == 'dict-items-contains':
        return (key, 1) in target.items()
    if name == 'dict-set':
        target[key] = 8
        return None
    if name == 'dict-delete':
        del target[key]
        return None
    if name == 'set-add':
        return target.add(key)
    if name == 'set-contains':
        return key in target
    if name == 'set-remove':
        return target.remove(key)
    return target.discard(key)

class Disabled:
    def __eq__(self, other):
        raise AssertionError('unhashable key must not compare')
disabled = Disabled()
for state in ['fresh', 'nonempty', 'cleared', 'deleted']:
    for name in ['dict-get', 'dict-method-get', 'dict-setdefault', 'dict-contains',
                 'dict-keys-contains', 'dict-items-contains', 'dict-set', 'dict-delete',
                 'set-add', 'set-contains', 'set-remove', 'set-discard']:
        target = {}
        if name in ['set-add', 'set-contains', 'set-remove', 'set-discard']:
            # set.clear尚未开放；只测已支持的fresh/nonempty/deleted，不为本切新增API。
            if state == 'cleared':
                continue
            target = set()
            if state != 'fresh':
                target.add(1)
            if state == 'deleted':
                target.remove(1)
        elif state != 'fresh':
            target[1] = 1
            if state == 'cleared':
                target.clear()
            if state == 'deleted':
                del target[1]
        try:
            raise ValueError('active')
        except ValueError as active:
            try:
                operation(name, target, disabled)
            except TypeError as error:
                assert str(error) == "unhashable type: 'Disabled'"
                assert error.__context__ is active
            else:
                raise AssertionError(name)
        print('disabled', state, name)

# frozenset的membership有独立入口，不能从可变set覆盖推断。
for target in [frozenset(), frozenset([1])]:
    try:
        disabled in target
    except TypeError as error:
        assert str(error) == "unhashable type: 'Disabled'"
    else:
        raise AssertionError('frozenset-contains')

# CPython空dict.pop先按长度返回/报KeyError，既不看hash槽也不调用用户hash。
fresh = {}
cleared = {1: 1}
cleared.clear()
deleted = {1: 1}
del deleted[1]
for target in [fresh, cleared, deleted]:
    assert target.pop(disabled, 'missing') == 'missing'
    try:
        target.pop(disabled)
    except KeyError as error:
        assert error.args[0] is disabled
    else:
        raise AssertionError('empty-pop')
try:
    {1: 1}.pop(disabled, 'missing')
except TypeError as error:
    assert str(error) == "unhashable type: 'Disabled'"
else:
    raise AssertionError('nonempty-pop')

calls = []
class Key:
    def __hash__(self):
        calls.append('hash')
        return 31
key = Key()
assert hash(key) == 31
assert calls == ['hash']
calls.clear()
mapping = {key: 1}
assert calls == ['hash']
calls.clear()
values = {key}
assert calls == ['hash']

# 仅非空有效键路径作为本切计数门。旧空CLR查询漏hash副作用独立pending，不能改CP期望。
for name in ['dict-get', 'dict-method-get', 'dict-contains', 'dict-set', 'dict-delete',
             'set-add', 'set-contains', 'set-remove', 'set-discard']:
    target = {key: 1}
    if name in ['set-add', 'set-contains', 'set-remove', 'set-discard']:
        target = {key}
    calls.clear()
    operation(name, target, key)
    assert calls == ['hash']
    print('once', name)

# 混键仍用现有相等性/身份短路；相等覆盖保原始键对象，不比跨进程hash数值。
class NumericKey:
    def __hash__(self):
        calls.append('numeric-hash')
        return 1
    def __eq__(self, other):
        calls.append('numeric-eq')
        if isinstance(other, NumericKey):
            return True
        return other == 1
numeric = NumericKey()
equal_numeric = NumericKey()
mapping = {numeric: 'first'}
calls.clear()
mapping[equal_numeric] = 'second'
assert calls == ['numeric-hash', 'numeric-eq']
assert len(mapping) == 1
assert next(iter(mapping)) is numeric
for number in [True, 1, 1.0]:
    assert mapping[number] == 'second'
    assert numeric in {number}
    assert number in {numeric}
    assert {number: 'builtin'}[numeric] == 'builtin'

# 现成异常实例不得包装成新的TypeError；当前活动异常必须成为context。
class Failing:
    def __hash__(self):
        calls.append('failure')
        raise shared
failing = Failing()
for name in ['hash', 'dict-set', 'set-add']:
    shared = RuntimeError('hash-body')
    calls.clear()
    try:
        raise ValueError('active-callback')
    except ValueError as active:
        try:
            if name == 'hash':
                hash(failing)
            elif name == 'dict-set':
                operation(name, {}, failing)
            else:
                operation(name, set(), failing)
        except RuntimeError as error:
            assert error is shared
            assert error.__context__ is active
            assert calls == ['failure']
        else:
            raise AssertionError(name)
print('containers-ok')
