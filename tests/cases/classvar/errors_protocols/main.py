# ryupy: pending
# 只把 CP 确实拒绝的域写成 TypeError；根与别名的调用、类检查和基类错误分别冻结。
import typing as t
CV = t.ClassVar
T = t.TypeVar('T')

def error(label, call, expected_type=TypeError):
    try:
        call()
    except expected_type as value:
        print(label, type(value).__name__, str(value))
    else:
        assert False, label

for value in [(), (int,), (int, str), ((int,),)]:
    error('tuple', lambda: CV[value])
error('tuple-unicode-repr-limit', lambda: CV[('😀' * 110,)])
for value in [CV, CV[int], t.Generic, t.Generic[T], t.Union, t.Optional, t.Literal]:
    error('invalid-type-argument', lambda: CV[value])
for name in ['__origin__', '__args__', '__parameters__']:
    error('root-missing', lambda: getattr(CV, name), AttributeError)
for value in [CV, CV[int]]:
    error('call', lambda: value())
    error('call-args', lambda: value(1, name='value'))
    error('instance', lambda: isinstance(1, value))
    error('subclass', lambda: issubclass(int, value))
    error('mro-entries', lambda: value.__mro_entries__((value,)))
    assert isinstance(1, (int, value))
    assert issubclass(bool, (int, value))
    error('instance-tuple-reached', lambda: isinstance('s', (int, value)))
    error('subclass-tuple-reached', lambda: issubclass(str, (int, value)))
    body = []
    try:
        class Bad(value):
            body.append('unexpected')
    except TypeError as caught:
        print('class-before-body', str(caught), body)
    else:
        assert False
    assert body == []
error('closed-resubscribe', lambda: CV[int][str])
error('typing-list-rejects', lambda: t.List[CV[int]])
error('union-rejects', lambda: t.Union[CV[int], str])
error('optional-rejects', lambda: t.Optional[CV[int]])
# 原生泛型不调用 typing._type_check，不能把上述禁止规则误加到所有下标路径。
native = list[CV[int]]
assert native.__args__ == (CV[int],)
assert t.get_origin(native) is list
assert list[CV].__args__ == (CV,)
print('native-nesting', repr(native))
error('typevar-bound-root', lambda: t.TypeVar('BadRoot', bound=CV))
error('typevar-bound-alias', lambda: t.TypeVar('BadAlias', bound=CV[int]))
error('namedtuple-annotation', lambda: t.NamedTuple('Bad', [('x', CV[int])]))
