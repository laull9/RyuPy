# ryupy: pending
# Callable的args只转换；result先严格校验，不能统一插入ClassVar guard。
import typing as t
CV=t.ClassVar
cv=CV[int]
for value in (CV,cv):
    a=t.Callable[[value],int]
    assert a.__args__==(value,int)
    assert t.get_args(a)==([value],int)
    assert t.Callable[(value,),int] is a
    scalar=t.Callable[value,int]
    assert scalar==a and scalar is not a
    wanted=('Plain typing.ClassVar is not valid as type argument' if value is CV else
            'typing.ClassVar[int] is not valid as type argument')
    for arguments in ([],[int],['[']):
        # '['若先转换会产生SyntaxError；真实result错误必须遮蔽该未开放参数。
        try: t.Callable[arguments,value]
        except TypeError as error:
            assert str(error)==wanted
            print('callable-result-first',str(error))
        else: assert False
try:
    t.ClassVar=None
    cold=t.TypeVar('Result')
    a=t.Callable[[CV,cv,None],cold]
    assert a.__args__==(CV,cv,type(None),cold)
    assert t.Callable[(CV,cv,None),cold] is a
finally: t.ClassVar=CV
print('callable-canonical-args-only',True)
