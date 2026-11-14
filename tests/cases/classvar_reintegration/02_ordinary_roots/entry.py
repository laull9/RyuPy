# ryupy: pending
# 十一个根均在真冷逐参校验后检查arity；ClassVar=None不再是absence NI。
import typing as t
CV=t.ClassVar
cv=CV[int]
roots=((t.List,1),(t.Dict,2),(t.Set,1),(t.FrozenSet,1),
       (t.Iterable,1),(t.Iterator,1),(t.Sequence,1),(t.Mapping,2),
       (t.MutableMapping,2),(t.Collection,1),(t.Container,1))
for root,arity in roots:
    for value,message in ((CV,'Plain typing.ClassVar is not valid as type argument'),
                          (cv,'typing.ClassVar[int] is not valid as type argument')):
        # 增加一个参数仍先得到真实type-check错误，不能提前报arity。
        raw=(value,int) if arity==1 else (int,value,str)
        try: root[raw]
        except TypeError as error:
            assert str(error)==message
            print(root.__name__,'validation-before-arity',str(error))
        else: assert False
    if arity==2:
        try: root[(cv,CV)]
        except TypeError as error: assert str(error)=='typing.ClassVar[int] is not valid as type argument'
        else: assert False
    key=t.TypeVar('PlainCold')
    raw=(key,) if arity==1 else (key,int)
    try:
        t.ClassVar=None
        alias=root[raw]
        assert alias.__args__==raw
    finally: t.ClassVar=CV
    assert root[raw] is alias
    print(root.__name__,'classvar-none-cold-success')
print('ordinary-roots',len(roots))
