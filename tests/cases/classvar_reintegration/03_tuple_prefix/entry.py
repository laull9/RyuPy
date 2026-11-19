# ryupy: pending
# Tuple空参数不消费checker；所有尾Ellipsis之前的前缀仍顺序校验。
import typing as t
CV=t.ClassVar
cv=CV[int]
key=t.List[int]
for raw,wanted in (
    ((CV,), 'Plain typing.ClassVar is not valid as type argument'),
    ((cv,), 'typing.ClassVar[int] is not valid as type argument'),
    ((int,CV,...), 'Plain typing.ClassVar is not valid as type argument'),
    ((int,str,cv,...), 'typing.ClassVar[int] is not valid as type argument'),
    ((cv,CV,...), 'typing.ClassVar[int] is not valid as type argument'),
):
    try: t.Tuple[raw]
    except TypeError as error:
        assert str(error)==wanted
        print('tuple-prefix-error',str(error))
    else: assert False
try:
    t.ClassVar=list
    empty=t.Tuple[()]
    assert empty.__args__==()
    for raw in ((key,), (int,key,...), (int,str,key,...)):
        try: t.Tuple[raw]
        except TypeError as error:
            assert str(error)=='typing.List[int] is not valid as type argument'
        else: assert False
    t.ClassVar=None
    none=t.Tuple[None,str,...]
    none_type=t.Tuple[type(None),str,...]
    assert none==none_type and none is not none_type
    assert none.__args__==(type(None),str,...)
    assert t.Tuple[()] is empty
finally: t.ClassVar=CV
assert t.Tuple[None,str,...] is none
assert t.Tuple[type(None),str,...] is none_type
print('tuple-empty-prefix-none',True)
