# 对08错误断言的独立修正版；08源与两次失败原流保留。
import typing as t
C=t.Callable
key=t.List[int]
warm=C[[key],int]
warm_result=C[[],key]
t.ClassVar=list
assert C[[key],int] is warm
assert C[(key,),int] is warm
cold_args=C[key,int]
assert cold_args==warm and cold_args is not warm
assert C[[key],str].__args__==(key,str)
assert C[[],key] is warm_result
# List新原键检查int，不检查产生的List alias；因此这里必须成功。
equal_key=t.List[(int,)]
assert equal_key==key and equal_key is not key
# Callable inner真实eq命中已暖原键，同值不同alias身份不是冷键。
assert C[(),equal_key] is warm_result
try: C[[str],key]
except TypeError as error: print('cold-result-error',str(error))
else: assert False
# ordinary真正冷检查List alias的反例仍失败。
try: t.List[(key,)]
except TypeError as error: print('ordinary-cold-error',str(error))
else: assert False
print('ClassVar rebind: warm list/tuple shared; scalar args cold allowed; equal result key hits; cold result rejected: ok')
