# ryupy: pending
# CP 允许别名元数据写入，且 args 与 parameters 是独立槽；C1 不实现时必须明确 NI。
import typing as t
CV = t.ClassVar
T = t.TypeVar('T')
alias = CV[T]
parameters = alias.__parameters__
alias.__args__ = (str,)
assert alias.__args__ == (str,)
assert alias.__parameters__ is parameters
assert CV[T] is alias
assert repr(alias) == 'typing.ClassVar[str]'
print('independent-slots', repr(alias), alias.__parameters__)
alias.__parameters__ = ()
assert alias.__parameters__ == ()
alias.__module__ = 'custom_module'
assert alias.__module__ == 'custom_module'
alias.__origin__ = list
assert t.get_origin(alias) is list
print('mutable-origin', repr(alias), alias.__module__)
saved_name = CV._name
CV._name = 'Renamed'
assert repr(CV) == 'typing.Renamed'
assert CV.__name__ == CV.__qualname__ == 'Renamed'
print('mutable-root-name', repr(CV))
CV._name = saved_name
assert repr(CV) == 'typing.ClassVar'
