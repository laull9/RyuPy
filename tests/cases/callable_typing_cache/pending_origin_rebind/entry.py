import typing as t
from collections import abc
C=t.Callable
origin=abc.Callable
T=t.TypeVar('Origin')
abc.Callable=list
try:
    a=C[[T],int]
    assert a.__origin__ is origin and t.get_origin(a) is origin
finally:
    abc.Callable=origin
print('cold saved root keeps captured ABC origin: ok')
