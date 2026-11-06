# 保存receiver与绑定方法的独立有限合同：只写helper，不读取/恢复私有导出。
# 此程序独立进程结束；全部TypeVar与真实origin在任何helper改绑之前保存。
import typing as t
from collections import abc
C=t.Callable
method=C.__getitem__
origin=abc.Callable
T=t.TypeVar('Warm'); U=t.TypeVar('SavedCold')
V=t.TypeVar('UnusedConstructor'); W=t.TypeVar('UnusedArity')
warm=C[[T],int]
args=warm.__args__
parameters=warm.__parameters__
t.Callable=list
assert C[(T,),int] is warm and method(([T],int)) is warm
cold=method(([U],str))
assert cold.__args__==(U,str) and cold.__origin__ is origin
assert C[(U,),str] is cold and method(((U,),str)) is cold
# 只设置这两个真实未消费的helper，冷构造不应读取它们。
setattr(t,'_CallableType',None)
a=method(([V],bytes))
assert a.__args__==(V,bytes) and a.__origin__ is origin
setattr(t,'_check_generic',None)
b=C[[W],float]
assert b.__args__==(W,float) and b.__origin__ is origin
assert method(((V,),bytes)) is a and method(((W,),float)) is b
# 后续均同一安全raw键；每次外层快照后应暖命中，不执行冷helper。
for name in ('_type_check','_type_convert','_CallableGenericAlias','_TypingEllipsis','_collect_parameters'):
    setattr(t,name,None)
    assert C[[T],int] is warm and method(((T,),int)) is warm
    assert warm.__args__ is args and warm.__parameters__ is parameters
    assert args==(T,int) and parameters==(T,)
    assert warm.__origin__ is origin
print('saved root/method; cold origin; unused helper writes; warm stable metadata: ok')
