# ryupy: pending
# 既有Optional/Union、TypeVar bound、NamedTuple按当前导出比较真实alias origin。
import typing as t
CV=t.ClassVar

def consume(kind,value):
    if kind=='optional': return t.Optional[value]
    if kind=='union': return t.Union[value,str]
    if kind=='bound': return t.TypeVar('Bound',bound=value)
    return t.NamedTuple('Pair',[('subject',value)])

count=0
try:
    for mode,replacement in (('canonical',CV),('list',list),('string','changed'),('none',None)):
        t.ClassVar=replacement
        for kind in ('optional','union','bound','namedtuple'):
            # 每个消费者新alias，防先前成功缓存掩盖真实冷阶段。
            token=t.TypeVar('Fresh')
            cv=CV[token]; regular=t.List[token]
            for label,value in (('root',CV),('cv',cv),('list',regular),('plain',int),('native',list[cv])):
                wanted=None
                if label=='root': wanted='Plain typing.ClassVar is not valid as type argument'
                elif mode=='canonical' and label=='cv': wanted=repr(value)+' is not valid as type argument'
                elif mode=='list' and label=='list': wanted=repr(value)+' is not valid as type argument'
                try:
                    result=consume(kind,value)
                except TypeError as error:
                    assert wanted is not None and str(error)==wanted,(mode,kind,label,str(error))
                    print(mode,kind,label,'TypeError',str(error))
                else:
                    assert wanted is None,(mode,kind,label)
                    if kind=='bound': assert result.__bound__ is value
                    if kind=='namedtuple': assert result.__annotations__['subject'] is value
                    print(mode,kind,label,'ok')
                count+=1
            constrained=t.TypeVar('Choices',CV,cv)
            assert constrained.__constraints__==(CV,cv)
finally: t.ClassVar=CV
assert count==80
print('typecheck-consumers',count,'constraints-preserved')
