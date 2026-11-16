# 新类型名的基本isinstance/issubclass身份边，不扩完整MRO协议。
import typing as t
c=t.Callable[[int],str]; ordinary=t.List[int]
assert type(c).__name__=='_CallableGenericAlias'
assert type(ordinary).__name__=='_GenericAlias'
assert isinstance(c,type(ordinary)) and issubclass(type(c),type(ordinary))
assert not isinstance(ordinary,type(c)) and not issubclass(type(ordinary),type(c))
assert isinstance(c,type(c)) and issubclass(type(c),object)
print('Callable subtype identity; ordinary reverse false: ok')
