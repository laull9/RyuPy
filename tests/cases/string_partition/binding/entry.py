# 动态绑定与调用顺序：精确错误、无隐式转换、直接/保存/根/暂停入口和属性捕获。
events = []


class Convert:
    def __str__(self):
        events.append('str')
        raise AssertionError('不得隐式转换sep')
    def __index__(self):
        events.append('index')
        raise AssertionError('不得转换sep索引')
    def __iter__(self):
        events.append('iter')
        raise AssertionError('不得迭代sep')


class Text(str):
    pass


def mark(label, value):
    events.append(label)
    return value


def failure(label, call, kind, expected_events):
    events.clear()
    try:
        call()
    except Exception as error:
        assert type(error).__name__ == kind
        assert events == expected_events
        print(label, type(error).__name__, str(error), events)
    else:
        raise AssertionError('调用应失败')


for method in ('partition', 'rpartition'):
    root = getattr(str, method)
    source = ''.join(['source_', 'payload_long'])
    saved = getattr(source, method)
    for label, call in [
        ('bound-zero', lambda: saved()),
        ('bound-two', lambda: saved('x', 'y')),
        ('bound-keyword', lambda: saved(sep='x')),
        ('bound-extra-keyword', lambda: saved('x', bad=1)),
        ('bound-zero-keyword', lambda: saved(bad=1)),
        ('bound-two-keyword', lambda: saved('', None, bad=1)),
        ('root-zero', lambda: root()),
        ('root-one', lambda: root(source)),
        ('root-three', lambda: root(source, 'x', 'y')),
        ('root-self-keyword', lambda: root(self=source, sep='x')),
        ('root-sep-keyword', lambda: root(source, sep='x')),
        ('root-badself', lambda: root(42, 'x')),
        ('root-badself-keyword', lambda: root(42, sep='x')),
        ('root-badself-extra', lambda: root(42, 'x', 'y')),
        ('root-empty-keyword', lambda: root(source, '', bad=1)),
    ]:
        failure(method + '-' + label, call, 'TypeError', [])
    for bad in (None, 1, True, b'x', bytearray(b'x'), ['x'], ('x',), object(), Convert()):
        failure(method + '-badsep-' + type(bad).__name__, lambda: saved(bad), 'TypeError', [])
    for empty_source in ('', 'abc', Text(''), Text('abc')):
        for empty_sep in ('', Text('')):
            failure(method + '-empty-' + type(empty_source).__name__ + '-' + type(empty_sep).__name__,
                    lambda: root(empty_source, empty_sep), 'ValueError', [])


# 保留源码直接调用形态，避免只测getattr+call而漏过编译器入口。
failure('direct-partition-keyword', lambda: 'abc'.partition(sep=mark('kw', 'a')), 'TypeError', ['kw'])
failure('direct-rpartition-arity', lambda: 'abc'.rpartition(mark('one', ':'), mark('two', ':')),
        'TypeError', ['one', 'two'])
failure('root-direct-self-first', lambda: str.partition(42, '', bad=mark('kw', 1)), 'TypeError', ['kw'])
failure('root-direct-keyword-first', lambda: str.rpartition('abc', '', bad=mark('kw', 1)), 'TypeError', ['kw'])


for method in ('partition', 'rpartition'):
    class Override(str):
        pass

    def old(self, sep):
        events.append('old')
        return 'old-result'

    def new(self, sep):
        events.append('new')
        return 'new-result'

    def mutate_arg():
        events.append('arg-rebind')
        setattr(Override, method, new)
        return ':'

    setattr(Override, method, old)
    source = Override('a:b:c')
    saved = getattr(source, method)
    events.clear()
    if method == 'partition':
        result = source.partition(mutate_arg())
    else:
        result = source.rpartition(mutate_arg())
    assert result == 'old-result' and events == ['arg-rebind', 'old']
    print(method + '-capture-before-arg', result, events)
    events.clear()
    assert saved(mark('arg', ':')) == 'old-result'
    assert events == ['arg', 'old']
    print(method + '-saved-before-rebind', events)
    events.clear()
    if method == 'partition':
        result = str.partition(source, ':')
    else:
        result = str.rpartition(source, ':')
    assert result == (('a', ':', 'b:c') if method == 'partition' else ('a:b', ':', 'c'))
    assert events == []
    print(method + '-root-bypasses-override', result, events)

    def target(sep):
        events.append('call')
        return 'property-result'

    def getter(self):
        events.append('get')
        return self.target

    setattr(Override, method, property(getter))
    source.target = target
    events.clear()
    if method == 'partition':
        result = source.partition(mark('arg', ':'))
    else:
        result = source.rpartition(mark('arg', ':'))
    assert result == 'property-result' and events == ['get', 'arg', 'call']
    print(method + '-property-order', result, events)

    # 参数暂停前已经固定getter结果；恢复后不能重新读取替换的target。
    def paused():
        if method == 'partition':
            return source.partition((yield 'sep'))
        return source.rpartition((yield 'sep'))

    events.clear()
    pending = paused()
    assert next(pending) == 'sep' and events == ['get']
    source.target = lambda sep: 'replacement-result'
    try:
        pending.send(':')
    except StopIteration as done:
        assert done.value == 'property-result'
    else:
        raise AssertionError('生成器应结束')
    assert events == ['get', 'call']
    print(method + '-prepared', events)

    source.target = 7
    if method == 'partition':
        failure(method + '-noncallable', lambda: source.partition(mark('arg', ':')), 'TypeError', ['get', 'arg'])
    else:
        failure(method + '-noncallable', lambda: source.rpartition(mark('arg', ':')), 'TypeError', ['get', 'arg'])

    marker = ValueError('getter-failure')
    def bad_getter(self):
        events.append('get-raise')
        raise marker

    setattr(Override, method, property(bad_getter))
    events.clear()
    try:
        if method == 'partition':
            source.partition(mark('unexpected-arg', ':'))
        else:
            source.rpartition(mark('unexpected-arg', ':'))
    except ValueError as caught:
        assert caught is marker
    else:
        raise AssertionError('getter应失败')
    assert events == ['get-raise']
    print(method + '-getter-error', events)

    # 普通属性直接赋值建立实例覆盖；不借用尚未支持的__dict__反射。
    setattr(Override, method, old)
    if method == 'partition':
        source.partition = lambda sep: 'instance-result'
        assert source.partition(':') == 'instance-result'
    else:
        source.rpartition = lambda sep: 'instance-result'
        assert source.rpartition(':') == 'instance-result'
    source.target = target
    setattr(Override, method, property(getter))
    events.clear()
    if method == 'partition':
        result = source.partition(mark('arg', ':'))
    else:
        result = source.rpartition(mark('arg', ':'))
    assert result == 'property-result' and events == ['get', 'arg', 'call']
    print(method + '-property-over-instance', result, events)
    delattr(Override, method)
    assert getattr(source, method)(':') == 'instance-result'
    print(method + '-instance-over-root', getattr(source, method)(':'))

    # 保存的内建方法也不能因后续类重绑改变接收者或实现。
    class Fresh(str):
        pass
    fresh = Fresh('a:b:c')
    bound = getattr(fresh, method)
    assert bound.__self__ is fresh
    setattr(Fresh, method, new)
    events.clear()
    result = bound(':')
    assert result == (('a', ':', 'b:c') if method == 'partition' else ('a:b', ':', 'c'))
    assert events == []
    print(method + '-saved-builtin', result, bound.__self__ is fresh)
print('binding-ok')
