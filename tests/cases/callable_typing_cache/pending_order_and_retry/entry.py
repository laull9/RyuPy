import typing as t
C=t.Callable
events=[]
original_check=t._type_check
original_convert=t._type_convert
original_collect=t._collect_parameters
original_alias=t._CallableGenericAlias
class Args(list):
    def __iter__(self):
        events.append('list.iter')
        return super().__iter__()
def check(value,*a,**kw):
    events.append('check:'+value.__name__)
    return original_check(value,*a,**kw)
def convert(value,*a,**kw):
    events.append('convert:'+value.__name__)
    return original_convert(value,*a,**kw)
def alias(*a,**kw):
    events.append('copy_with')
    return original_alias(*a,**kw)
def collect(*a,**kw):
    events.append('collect')
    return original_collect(*a,**kw)
t._type_check=check; t._type_convert=convert; t._CallableGenericAlias=alias; t._collect_parameters=collect
x=C[Args([int,str]),float]
assert events==['list.iter','check:float','convert:float','convert:int','convert:str','copy_with','collect']
print('cold',events)
events.clear(); assert C[Args([int,str]),float] is x
assert events==['list.iter']; print('warm',events)
events.clear()
def fail(value,*a,**kw):
    events.append('check-fail')
    raise TypeError('body-error')
t._type_check=fail
try: C[Args([bytes]),complex]
except TypeError as error: assert str(error)=='body-error'
else: assert False
assert events==['list.iter','check-fail','check-fail']
print('body-retry',events)
events.clear()
class BadArgs(list):
    def __iter__(self):
        events.append('outer-fail')
        raise TypeError('outer-error')
try: C[BadArgs([int]),complex]
except TypeError as error: assert str(error)=='outer-error'
else: assert False
assert events==['outer-fail']; print('outer-no-retry',events)
