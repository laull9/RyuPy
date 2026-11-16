
# 精确list.extend与+=先iter再源len/hint，逐项追加；失败保副作用和已追加前缀。

EVENTS = []

class Cursor:
    def __init__(self, target, fail):
        self.target = target
        self.fail = fail
        self.position = 0

    def __iter__(self):
        EVENTS.append('cursor.iter')
        return self

    def __length_hint__(self):
        EVENTS.append('cursor.hint')
        return 99

    def __next__(self):
        EVENTS.append(('next', self.position, self.target.copy()))
        if self.position == 2:
            if self.fail:
                raise RuntimeError('source-next-failed')
            raise StopIteration
        self.position += 1
        return self.position + 10

class LenSource:
    def __init__(self, target, mode):
        self.target = target
        self.mode = mode

    def __iter__(self):
        EVENTS.append('source.iter')
        if self.mode == 'iter-error':
            self.target.append(91)
            raise ValueError('source-iter-failed')
        return Cursor(self.target, self.mode == 'next-error')

    def __len__(self):
        EVENTS.append('source.len')
        if self.mode == 'len-error':
            self.target.append(92)
            raise ValueError('source-len-failed')
        if self.mode == 'negative-len':
            return -1
        if self.mode == 'len-typeerror':
            raise TypeError('len-fallback')
        return 2

    def __length_hint__(self):
        EVENTS.append('source.hint')
        return 2

class HintSource:
    def __init__(self, target, mode):
        self.target = target
        self.mode = mode

    def __iter__(self):
        EVENTS.append('source.iter')
        return Cursor(self.target, False)

    def __length_hint__(self):
        EVENTS.append('source.hint')
        if self.mode == 'hint-error':
            self.target.append(93)
            raise ValueError('source-hint-failed')
        if self.mode == 'negative-hint':
            return -1
        if self.mode == 'invalid-hint':
            return 'bad'
        if self.mode == 'not-implemented':
            return NotImplemented
        return 2

def append_from(target, source, operation):
    if operation == 'extend':
        assert target.extend(source) is None
    else:
        alias = target
        target += source
        assert target is alias

def success(operation, source_kind, mode, expected_prefix):
    EVENTS.clear()
    target = [1]
    alias = target
    source = source_kind(target, mode)
    append_from(target, source, operation)
    assert target == [1, 11, 12] and target is alias
    assert EVENTS == expected_prefix + [('next', 0, [1]), ('next', 1, [1, 11]), ('next', 2, [1, 11, 12])]
    print('list-primitives.extend-success', operation, mode, target, EVENTS)

def failure(operation, source_kind, mode, kind, message, expected_target, expected_events):
    EVENTS.clear()
    target = [1]
    alias = target
    try:
        append_from(target, source_kind(target, mode), operation)
    except kind as error:
        assert str(error) == message
    else:
        raise AssertionError(mode)
    assert target == expected_target and target is alias
    assert EVENTS == expected_events
    print('list-primitives.extend-error', operation, mode, target, EVENTS, message)

for operation in ['extend', 'iadd']:
    success(operation, LenSource, 'len', ['source.iter', 'source.len'])
    success(operation, LenSource, 'len-typeerror', ['source.iter', 'source.len', 'source.hint'])
    success(operation, HintSource, 'hint', ['source.iter', 'source.hint'])
    success(operation, HintSource, 'not-implemented', ['source.iter', 'source.hint'])
    failure(operation, LenSource, 'iter-error', ValueError, 'source-iter-failed', [1, 91], ['source.iter'])
    failure(operation, LenSource, 'len-error', ValueError, 'source-len-failed', [1, 92], ['source.iter', 'source.len'])
    failure(operation, LenSource, 'negative-len', ValueError, '__len__() should return >= 0', [1], ['source.iter', 'source.len'])
    failure(operation, HintSource, 'hint-error', ValueError, 'source-hint-failed', [1, 93], ['source.iter', 'source.hint'])
    failure(operation, HintSource, 'negative-hint', ValueError, '__length_hint__() should return >= 0', [1], ['source.iter', 'source.hint'])
    failure(operation, HintSource, 'invalid-hint', TypeError, '__length_hint__ must be an integer, not str', [1], ['source.iter', 'source.hint'])
    failure(operation, LenSource, 'next-error', RuntimeError, 'source-next-failed', [1, 11, 12], ['source.iter', 'source.len', ('next', 0, [1]), ('next', 1, [1, 11]), ('next', 2, [1, 11, 12])])

# 直接self来源有限复制，保同一容器与浅引用；普通tuple/string/generator也走公共入口。
values = [1, 2]
alias = values
assert values.extend(values) is None
assert values == [1, 2, 1, 2] and values is alias
values += values
assert values == [1, 2, 1, 2, 1, 2, 1, 2] and values is alias
empty = []
assert empty.extend(empty) is None
empty += empty
assert empty == []
print('list-primitives.self-alias', values, empty, values is alias)

values = [0]
assert values.extend((1, 2)) is None
values += 'ab'
values.extend(number for number in [3, 4])
assert values == [0, 1, 2, 'a', 'b', 3, 4]
print('list-primitives.common-sources', values)
