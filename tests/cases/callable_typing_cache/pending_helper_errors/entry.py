import typing as t
C=t.Callable
T=t.TypeVar('T'); old=t._type_convert
warm=C[[],int]
events=[]
def convert(value,*a,**kw):
    events.append('convert')
    return old(value,*a,**kw)
t._type_convert=convert
assert C[[],int] is warm and events==[]
# result 的_type_check也调用_convert；empty args无额外转换。
a=C[[],str]
assert events==['convert']; print('empty cold',events)
events.clear(); b=C[[T,T],str]
assert events==['convert','convert','convert']; print('nonempty cold',events)
# result 失败在args conversion之前，但整个inner TypeError会回退再执行。
events.clear()
try: C[[T],()]
except TypeError as error: print('result tuple error',str(error))
else: assert False
assert events==['convert','convert']; print('failed result conversion count',len(events))
