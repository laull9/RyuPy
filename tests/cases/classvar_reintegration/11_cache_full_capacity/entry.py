# ryupy: pending
# C#新增四池容量facts的逐项CP配对：160个不同错误各重试两次后，全部128旧项仍存活。
import typing as t
CV=t.ClassVar

def fresh(): return t.TypeVar('C1FailureKey')

def success(family,key):
    if family=='Callable': return t.Callable[[],key]
    if family=='Union': return t.Union[key,str]
    if family=='List': return t.List[key]
    return t.Tuple[key]

def result(family,alias,marker):
    if family=='Callable': return t.Callable[[marker],alias]
    if family=='Union': return t.Union[alias,marker]
    if family=='List': return t.List[(alias,)]
    return t.Tuple[(alias,)]

for family in ('List','Tuple','Callable','Union'):
    # 错误alias提前全部创建，避免污染之后目标池128项的可观测状态。
    bad=[]; markers=[]
    for i in range(160):
        bad.append(t.List[fresh()]); markers.append(fresh())
    keys=[]; old=[]
    for i in range(128):
        key=fresh();keys.append(key);old.append(success(family,key))
    try:
        t.ClassVar=list
        for i in range(160):
            for repeat in range(2):
                try: result(family,bad[i],markers[i])
                except TypeError as error:
                    assert str(error)==repr(bad[i])+' is not valid as type argument'
                else: assert False
        for i in range(128):
            assert success(family,keys[i]) is old[i]
    finally: t.ClassVar=CV
    recovered=result(family,bad[0],markers[0])
    assert result(family,bad[0],markers[0]) is recovered
    assert recovered.__args__[0 if family=='Union' else -1] is bad[0]
    # 首次恢复成功恰好挤掉最旧0项，不能预先占坑、重复插入或错驱逐第1项。
    assert success(family,keys[1]) is old[1]
    rebuilt=success(family,keys[0])
    assert rebuilt is not old[0] and rebuilt==old[0]
    print(family,'320-failures-retain-all-128;first-success-evicts-one',True)
