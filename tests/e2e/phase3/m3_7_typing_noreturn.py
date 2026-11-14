# ryupy: pending
# NoReturn 的公开合同与跨消费者冷/暖语义；CP 原流独立冻结，产品待 hosted 门禁。
import typing as t

n = t.NoReturn
print(n is t.NoReturn, repr(n), type(n) is type(t.Union))
print(n.__name__, n.__qualname__, n.__module__, t.get_origin(n), t.get_args(n))

def report(label, operation):
    try:
        result = operation()
        print(label, repr(result))
    except Exception as error:
        print(label, type(error).__name__, str(error))

report('call', lambda: n(1, x=2))
report('explicit-call', lambda: n.__call__(1, x=2))
report('subscribe', lambda: n[int])
report('instance', lambda: isinstance(1, n))
report('subclass', lambda: issubclass(int, n))
report('base', lambda: n.__mro_entries__((n,)))
for root in (t.List, t.Set, t.FrozenSet, t.Iterable, t.Iterator, t.Sequence, t.Collection, t.Container):
    alias = root[n]
    print(root.__name__, t.get_args(alias)[0] is n, root[n] is alias)
for root in (t.Dict, t.Mapping, t.MutableMapping):
    print(root.__name__, t.get_args(root[int, n])[1] is n)
print(t.Union[n] is n, repr(t.Optional[n]), repr(t.ClassVar[n]))
print(repr(t.Tuple[n, ...]), repr(t.Callable[[n], n]))
print(t.TypeVar('Bound', bound=n).__bound__ is n)
print(t.NamedTuple('Point', [('x', n)]).__annotations__['x'] is n)
print(t.TypedDict('Record', {'x': n}).__annotations__['x'] is n)
print(t.get_args(list[n])[0] is n, t.get_args(t.Literal[n])[0] is n)
print(repr(n | int), repr(int | n))

warm = t.List[t.Any]
del t.NoReturn
print('warm', t.List[t.Any] is warm)
report('deleted-list', lambda: t.List[(t.Any,)])
report('deleted-bound', lambda: t.TypeVar('Missing', bound=t.Any))
report('deleted-named', lambda: t.NamedTuple('Missing', [('x', t.Any)]))
report('deleted-typed', lambda: t.TypedDict('Missing', {'x': t.Any}))
t.NoReturn = int
report('saved-union', lambda: t.Union[(n,)])
report('saved-bound', lambda: t.TypeVar('Saved', bound=n))
report('saved-named', lambda: t.NamedTuple('Saved', [('x', n)]))
report('saved-typed', lambda: t.TypedDict('Saved', {'x': n}))
print('replacement-int', t.Union[int] is int)
t.NoReturn = t.Union
print('replacement-special', t.Union[t.Union] is t.Union)
print('replacement-bound', t.TypeVar('Special', bound=t.Union).__bound__ is t.Union)
print('replacement-named', t.NamedTuple('Special', [('x', t.Union)]).__annotations__['x'] is t.Union)
t.NoReturn = n
print('restored', t.TypeVar('Restored', bound=n).__bound__ is n)
