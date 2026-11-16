import typing as t
C=t.Callable
T=t.TypeVar('T')
for value in (t.ClassVar[int],t.Generic[T],t.Generic,t.Union,t.Optional,t.Literal,(),42,True):
    a=C[[value],int]
    assert a.__args__==(value,int)
    print('args-accepted',repr(value),tuple(repr(p) for p in a.__parameters__))
for value in (t.ClassVar[int],t.Generic[T],t.Generic,t.Union,()):
    try: C[[],value]
    except TypeError as error: print('result-error',str(error))
    else: assert False
# conversion 本身只转 None 和 string，没有输入类别禁止。
a=C[[None,'T'],None]
assert a.__args__[0] is type(None) and isinstance(a.__args__[1],t.ForwardRef)
assert a.__args__[1].__forward_arg__=='T' and a.__args__[2] is type(None)
print('forward-ref result',repr(a))
P=t.ParamSpec('P'); Ts=t.TypeVarTuple('Ts')
for a in (C[P,int], C[t.Concatenate[T,P],str], C[[t.Unpack[Ts]],int], C[...,int], C[[...],int]):
    print('deferred-form',repr(a),tuple(repr(p) for p in a.__parameters__))
assert C[...,int] == C[[...],int] and C[...,int] is not C[[...],int]
print('conversion is not type-check: ok')
