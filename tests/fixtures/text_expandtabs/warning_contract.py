# ryupy: pending
# 真实caller位置、过滤、完整stderr文本、同站点重入与原异常身份；不改warnings内部。
import sys
import warnings

events = []
message = '__index__ returned non-int (type bool).  The ability to return an instance of a strict subclass of int is deprecated, and may be removed in a future version of Python.'


class BooleanIndex:
    def __index__(self):
        events.append('index')
        return True


def site(text, size):
    return text.expandtabs(size)


# 此常量指向上面的真实调用行；移动源码必须显式复审该合同。
source_line = 17
with warnings.catch_warnings(record=True) as captured:
    warnings.simplefilter('always')
    assert site('outer\t', BooleanIndex()) == 'outer '
    assert len(captured) == 1
    assert captured[0].category is DeprecationWarning
    assert str(captured[0].message) == message
    assert captured[0].filename == __file__
    assert captured[0].lineno == source_line
    assert site('\t', True) == ' '
    assert len(captured) == 1
    warnings.simplefilter('ignore')
    assert site('', BooleanIndex()) == ''
    assert len(captured) == 1
    warnings.simplefilter('error')
    try:
        site('', BooleanIndex())
    except DeprecationWarning as error:
        assert str(error) == message
    else:
        raise AssertionError('error过滤必须中止')
assert events == ['index', 'index', 'index']
marker = LookupError('stderr-marker')


class Sink:
    def __init__(self, fail):
        self.fail = fail

    def write(self, text):
        events.append(text)
        if self.fail:
            raise marker
        events.append(site('inner\t', 8))


expected = __file__ + ':' + str(source_line) + ': DeprecationWarning: ' + message + '\n  return text.expandtabs(size)\n'
original = sys.stderr
try:
    with warnings.catch_warnings():
        warnings.simplefilter('always')
        events.clear()
        sys.stderr = Sink(False)
        assert site('outer\t', BooleanIndex()) == 'outer '
        assert events == ['index', expected, 'inner   ']
        events.clear()
        sys.stderr = Sink(True)
        try:
            site('outer\t', BooleanIndex())
        except LookupError as error:
            assert error is marker
        else:
            raise AssertionError('write异常不能被重包')
        assert events == ['index', expected]
finally:
    sys.stderr = original
print('expandtabs warnings')
