import typing as t
C=t.Callable
wrappers=(type(t.List).__getitem__,type(t.Tuple).__getitem__,type(C).__getitem_inner__,type(t.Union).__getitem__)
pools=[t._caches[w.__wrapped__] for w in wrappers]
assert len({id(pool) for pool in pools})==4
for pool in pools:
    assert pool.cache_parameters()=={'maxsize':128,'typed':False}
    pool.cache_clear()
pool=pools[2]
vars=[t.TypeVar('V'+str(i)) for i in range(160)]
first=C[[vars[0]],int]
for v in vars[1:128]: C[[v],int]
assert pool.cache_info().currsize==128
assert C[(vars[0],),int] is first
second=C[[vars[1]],int]
for v in vars[128:130]: C[[v],int]
assert C[[vars[0]],int] is first and C[[vars[1]],int] is second
# 错误正文不新增或驱逐；保留当前最旧条目作证。
old=C[[vars[4]],int]
size=pool.cache_info().currsize
try: C[[],()]
except TypeError: pass
else: assert False
assert pool.cache_info().currsize==size and C[[vars[4]],int] is old
# 普通/Tuple/SpecialForm churn 不驱逐 Callable。
for v in vars:
    t.List[v]; t.Tuple[v]; t.Union[v,int]
assert C[[vars[0]],int] is first
# 反向隔离，不将旧 Literal/native 驱逐策略暗示为已正确。
la=t.List[vars[0]]; tu=t.Tuple[vars[0]]; un=t.Union[vars[0],int]
for v in vars: C[[v],str]
assert t.List[vars[0]] is la and t.Tuple[vars[0]] is tu and t.Union[vars[0],int] is un
assert C[[vars[0]],int] is not first
print('four distinct 128 typed=False pools; true touch; failure no insert; bidirectional isolation; eviction fresh: ok')
