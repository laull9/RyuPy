# 构造先后顺序与真值副作用；所有错误消息取固定 CP 原流。
import typing as t

def check(label, call):
    try:
        call()
    except Exception as error:
        print(label, type(error).__name__, str(error))
    else:
        assert False

shared = []
V = t.TypeVar('V', None, 'X', 7, shared, int, int)
assert V.__constraints__ == (None, 'X', 7, shared, int, int)
assert V.__constraints__[3] is shared and V.__constraints__ is V.__constraints__
W = t.TypeVar('W', bound=shared)
assert W.__bound__ is shared
shared.append(8)
assert V.__constraints__[3] == [8] and W.__bound__ == [8]
for bound in (None, int, 1, t.Any, V, t.List[V]):
    assert t.TypeVar('B', bound=bound).__bound__ is bound
forward = t.TypeVar('F', bound='Missing').__bound__
assert forward.__forward_arg__ == 'Missing'
assert t.TypeVar('F2', bound=forward).__bound__ is forward
print('references', True)
check('missing-name', lambda: t.TypeVar())
check('bad-name', lambda: t.TypeVar(1))
check('duplicate-name', lambda: t.TypeVar('V', name='W'))
check('unknown-keyword', lambda: t.TypeVar('V', other=True))
check('single-constraint', lambda: t.TypeVar('V', int))
check('single-before-conflict', lambda: t.TypeVar('V', int, bound=int))
check('constraint-conflict', lambda: t.TypeVar('V', int, str, bound=int))
check('bound-before-single', lambda: t.TypeVar('V', int, bound=(int, str)))
check('bound-before-conflict', lambda: t.TypeVar('V', int, str, bound=(int, str)))
check('flags-before-bound', lambda: t.TypeVar('V', bound=(int, str), covariant=True, contravariant=True))
check('infer-conflict', lambda: t.TypeVar('V', covariant=True, infer_variance=True))
check('tuple-bound', lambda: t.TypeVar('V', bound=(int, str)))
events = []
class Flag:
    def __init__(self, label, value):
        self.label = label
        self.value = value
    def __bool__(self):
        events.append(self.label)
        return self.value
class Boom:
    def __bool__(self):
        events.append('boom')
        raise ValueError('flag exploded')
value = t.TypeVar('V', infer_variance=Flag('infer', False), contravariant=Flag('contra', False), covariant=Flag('co', False))
assert events == ['co', 'contra', 'infer']
print('flag-order', events)
events.clear()
check('all-flags', lambda: t.TypeVar('V', infer_variance=Flag('infer', True), covariant=Flag('co', True), contravariant=Flag('contra', True)))
assert events == ['co', 'contra', 'infer']
print('conflict-events', events)
events.clear()
check('name-before-flags', lambda: t.TypeVar(1, covariant=Boom()))
check('keyword-before-flags', lambda: t.TypeVar('V', covariant=Boom(), unexpected=1))
assert events == []
check('raising-flag', lambda: t.TypeVar('V', covariant=Boom(), contravariant=Flag('later', True)))
assert events == ['boom']
events.clear()
check('nonbool-flag', lambda: t.TypeVar('V', covariant=Flag('bad', 1)))
assert events == ['bad']
print('flag-errors', events)
# 原生binder先验证未知关键字，但缺少必填name优先；不借通用函数绑定器猜测。
check('bad-name-unknown', lambda: t.TypeVar(1, spam=True))
check('missing-unknown', lambda: t.TypeVar(spam=True))
check('duplicate-name-unknown', lambda: t.TypeVar('T', name='U', spam=True))
