# 原始scalar/单tuple、None/NoneType、空Tuple及多前缀尾Ellipsis分别保留真实身份。
import typing as t
T=t.TypeVar('T')
a=t.Tuple[T]
b=t.Tuple[(T,)]
assert a is not b and a == b and hash(a) == hash(b)
assert t.Tuple[T] is a and t.Tuple[(T,)] is b
n=t.Tuple[None]
nt=t.Tuple[type(None)]
nw=t.Tuple[(None,)]
ntw=t.Tuple[(type(None),)]
assert n == nt == nw == ntw
assert len({id(n),id(nt),id(nw),id(ntw)}) == 4
x=t.Tuple[None,...]
y=t.Tuple[type(None),...]
assert x is not y and x == y
empty=t.Tuple[()]
assert t.Tuple[()] is empty and empty.__args__ == ()
assert repr(empty) == 'typing.Tuple[()]'
assert t.Tuple[int,str,...].__args__ == (int,str,...)
print('raw-shapes-none-empty-and-many-prefixes',True)
