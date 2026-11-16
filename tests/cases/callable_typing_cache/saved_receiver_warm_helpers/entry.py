# 独立支持范围；研究05完整冷helper错误程序另以pending原样保留。
import typing as t
from collections import abc
C=t.Callable
method=C.__getitem__
T=t.TypeVar('Saved'); U=t.TypeVar('Cold')
warm=C[[T],int]
t.Callable=list
assert C[[T],int] is warm and method(((T,),int)) is warm
cold=method(([U],str))
assert cold.__args__==(U,str) and cold.__origin__ is abc.Callable
assert C[(U,),str] is cold
for name in ('_CallableType','_check_generic'):
    original=getattr(t,name)
    setattr(t,name,None)
    key=t.TypeVar('UnusedHelper')
    a=method(([key],bytes))
    assert a.__args__==(key,bytes) and a.__origin__ is abc.Callable
    setattr(t,name,original)
# 每次使用同一safe raw键，不允许先消费helper再返回缓存。
for name in ('_type_check','_type_convert','_CallableGenericAlias','_TypingEllipsis','_collect_parameters'):
    original=getattr(t,name)
    setattr(t,name,None)
    assert C[[T],int] is warm and method(((T,),int)) is warm
    assert warm.__args__==(T,int) and warm.__parameters__==(T,)
    setattr(t,name,original)
print('saved receiver/method; cold origin; unused helpers; warm bypass: ok')
