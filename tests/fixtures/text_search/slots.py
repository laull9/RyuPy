# ryupy: pending
# 类型槽、求值顺序、end变更、真实显式三参站点及原异常身份；不借普通实例字段实现槽。
methods = ('count', 'find', 'rfind', 'index', 'rindex')
events = []


class Index:
    def __init__(self, label, value):
        self.label, self.value = label, value

    def __index__(self):
        events.append(self.label)
        return self.value


class PropertyIndex:
    @property
    def __index__(self):
        events.append('get')
        return lambda: 0


class StaticIndex:
    @staticmethod
    def __index__():
        events.append('static')
        return 0


class ClassIndex:
    @classmethod
    def __index__(cls):
        assert cls is ClassIndex
        events.append('class')
        return 0


class Descriptor:
    def __get__(self, obj, cls):
        assert cls is ChildIndex
        assert type(obj) is ChildIndex
        events.append('descriptor')
        return lambda: 0


class ParentIndex:
    __index__ = Descriptor()


class ChildIndex(ParentIndex):
    pass


marker = LookupError('marker')


class ThrowIndex:
    def __index__(self):
        events.append('throw')
        raise marker


class ThrowGetter:
    @property
    def __index__(self):
        events.append('getter')
        raise marker


for name in methods:
    call = getattr('abc', name)
    for start, trace in ((PropertyIndex(), ['get']), (StaticIndex(), ['static']),
                         (ClassIndex(), ['class']), (ChildIndex(), ['descriptor'])):
        events.clear()
        assert call('a', start, 3) == (1 if name == 'count' else 0)
        assert events == trace
    value = Index('type-slot', 0)
    value.__index__ = lambda: 99
    events.clear()
    assert call('a', value, 3) == (1 if name == 'count' else 0)
    assert events == ['type-slot']
    events.clear()
    assert call('a', Index('wide-start', -(1 << 100)), Index('wide-end', 1 << 100)) == (1 if name == 'count' else 0)
    assert events == ['wide-start', 'wide-end']
    # 旧245未单独覆盖坏needle+自定义end；此处明确补充并待一次CP核实。
    events.clear()
    try:
        call(42, Index('start', 0), Index('end', 3))
    except TypeError as error:
        assert str(error) == 'must be str, not int'
    else:
        raise AssertionError('应拒绝坏needle')
    assert events == ['start', 'end']
    for value, label in ((ThrowIndex(), 'throw'), (ThrowGetter(), 'getter')):
        events.clear()
        try:
            call(42, Index('start', 0), value)
        except LookupError as error:
            assert error is marker
        else:
            raise AssertionError('end原异常必须优先')
        assert events == ['start', label]
        events.clear()
        try:
            call('a', value, Index('must-not-run', 3))
        except LookupError as error:
            assert error is marker
        else:
            raise AssertionError('start原异常必须传播')
        assert events == [label]
    events.clear()
    try:
        call('a', Index('bad-result', 1.5))
    except TypeError as error:
        assert str(error) == '__index__ returned non-int (type float)'
    else:
        raise AssertionError('应拒绝float结果')
    assert events == ['bad-result']


# 每种方法都有固定CallMethod3语法站点；start递归调用原站点而非getattr旁路。
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


class End:
    def __index__(self):
        raise AssertionError('旧end槽不应执行')


class Start:
    def __index__(self):
        events.append('outer-start')
        assert site(active, 'zz', 'z', 1, 2) == 1
        End.__index__ = new_end
        return 0


def new_end(self):
    events.append('new-end')
    return 5


for active, wanted in zip(methods, (2, 0, 4, 0, 4)):
    events.clear()
    assert site(active, 'a😀bca', 'a', Start(), End()) == wanted
    assert events == ['outer-start', 'new-end']
print('text search slots')
