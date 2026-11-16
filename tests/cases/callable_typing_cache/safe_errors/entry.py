import typing as t
C=t.Callable
T=t.TypeVar('ResultError')
invalid=(
    ((), 'Callable[args, result]: result must be a type. Got ().'),
    ((int,), "Callable[args, result]: result must be a type. Got (<class 'int'>,)."),
    (t.Generic, "Plain <class 'typing.Generic'> is not valid as type argument"),
    (t.Union, 'Plain typing.Union is not valid as type argument'),
    (t.Optional, 'Plain typing.Optional is not valid as type argument'),
    (t.Literal, 'Plain typing.Literal is not valid as type argument'),
    (t.Generic[T], 'typing.Generic[~ResultError] is not valid as type argument'),
)
for result,message in invalid:
    for repeat in range(2):
        try: C[['NeverConvertThisForwardName'],result]
        except TypeError as error: assert str(error)==message
        else: assert False
for arg in (t.Generic,t.Union,t.Optional,t.Literal):
    a=C[[arg],int]
    assert a.__args__==(arg,int)
print('result checked first; precise errors; argument forms pass conversion: ok')
