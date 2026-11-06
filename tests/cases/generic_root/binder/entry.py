# CP已知非法值域：错误类别与完整消息，不替换合法未知域。
import typing as t
def check(label, call):
    try:
        call()
    except Exception as error:
        print(label, type(error).__name__, str(error))
    else:
        assert False
T = t.TypeVar('T')
A = t.Generic[T]
check('empty', lambda: t.Generic[()])
check('int', lambda: t.Generic[int])
check('none', lambda: t.Generic[None])
check('number', lambda: t.Generic[3])
check('text', lambda: t.Generic['T'])
check('list', lambda: t.Generic[[T]])
check('nested', lambda: t.Generic[((T,),)])
check('duplicate', lambda: t.Generic[T, T])
check('mixed', lambda: t.Generic[T, int, T])
check('root-pos', lambda: t.Generic(1))
check('root-kw', lambda: t.Generic(x=1))
check('alias-pos', lambda: A(1))
check('alias-kw', lambda: A(x=1))
check('resubscribe', lambda: A[int])
check('isinstance', lambda: isinstance(1, A))
check('issubclass', lambda: issubclass(int, A))
check('first-alias', lambda: issubclass(A, t.Generic))
