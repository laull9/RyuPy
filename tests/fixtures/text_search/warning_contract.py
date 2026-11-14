# ryupy: pending
# 五搜索的完整警告来源、过滤、stderr和showwarning重入/原异常身份；不放宽既存失败边界。
import sys
import warnings

methods = ('count', 'find', 'rfind', 'index', 'rindex')
events = []
message = '__index__ returned non-int (type bool).  The ability to return an instance of a strict subclass of int is deprecated, and may be removed in a future version of Python.'


class BooleanIndex:
    def __init__(self, label):
        self.label = label

    def __index__(self):
        events.append(self.label)
        return True


def site(name, text, sub, start, end):
    if name == 'count':
        return text.count(sub, start, end)
    if name == 'find':
        return text.find(sub, start, end)
    if name == 'rfind':
        return text.rfind(sub, start, end)
    if name == 'index':
        return text.index(sub, start, end)
    return text.rindex(sub, start, end)


# 固定真实调用行；改变上方源码必须一起复审，不使用消息子串断言。
source_lines = (22, 24, 26, 28, 29)
for active, source_line in zip(methods, source_lines):
    events.clear()
    with warnings.catch_warnings(record=True) as captured:
        warnings.simplefilter('always')
        assert site(active, 'aa', 'a', BooleanIndex('start'), 2) == 1
        assert site(active, 'aa', 'a', 0, BooleanIndex('end')) == (1 if active == 'count' else 0)
        assert events == ['start', 'end']
        assert len(captured) == 2
        for record in captured:
            assert record.category is DeprecationWarning
            assert str(record.message) == message
            assert record.filename == __file__
            assert record.lineno == source_line
        assert site(active, 'aa', 'a', True, 2) == 1
        assert len(captured) == 2
        warnings.simplefilter('ignore')
        assert site(active, 'aa', 'a', BooleanIndex('ignore'), 2) == 1
        assert len(captured) == 2
        warnings.simplefilter('error')
        events.clear()
        try:
            site(active, 'aa', 'a', BooleanIndex('error'), BooleanIndex('unreached'))
        except DeprecationWarning as error:
            assert str(error) == message
        else:
            raise AssertionError('error过滤必须中止')
        assert events == ['error']

marker = LookupError('warning-marker')


class Sink:
    def __init__(self, fail):
        self.fail = fail

    def write(self, text):
        events.append(text)
        assert site(active, 'zz', 'z', 1, 2) == 1
        if self.fail:
            raise marker


original = sys.stderr
try:
    for active, source_line in zip(methods, source_lines):
        expected = __file__ + ':' + str(source_line) + ': DeprecationWarning: ' + message + '\n  return text.' + active + '(sub, start, end)\n'
        with warnings.catch_warnings():
            warnings.simplefilter('always')
            events.clear()
            sys.stderr = Sink(False)
            assert site(active, 'aa', 'a', BooleanIndex('start'), 2) == 1
            assert events == ['start', expected]
            events.clear()
            sys.stderr = Sink(True)
            try:
                site(active, 'aa', 'a', BooleanIndex('start'), BooleanIndex('unreached'))
            except LookupError as error:
                assert error is marker
            else:
                raise AssertionError('stderr原异常必须传播')
            assert events == ['start', expected]
finally:
    sys.stderr = original


def hook(warning, category, filename, lineno, file=None, line=None):
    assert str(warning) == message
    assert category is DeprecationWarning
    assert filename == __file__
    assert lineno == source_line
    assert file is None
    assert line is None
    events.append('hook')
    assert site(active, 'zz', 'z', 1, 2) == 1
    raise marker


# 当前runtime可能在showwarning赋值就明确拒绝；保留失败，不改成其它钩子冒充通过。
for active, source_line in zip(methods, source_lines):
    events.clear()
    with warnings.catch_warnings():
        warnings.simplefilter('always')
        warnings.showwarning = hook
        try:
            site(active, 'aa', 'a', BooleanIndex('start'), BooleanIndex('unreached'))
        except LookupError as error:
            assert error is marker
        else:
            raise AssertionError('hook原异常必须传播')
    assert events == ['start', 'hook']
print('text search warnings')
