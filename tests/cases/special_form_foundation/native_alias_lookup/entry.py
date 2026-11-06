import typing as t
a=list[int]
old=t.Optional[a]
for i in range(130):
    t.Literal[i]
b=list[int]
print('precondition',a is b,a==b)
t._GenericAlias=None
try:
    print('native-warm-equal',t.Optional[b] is old)
except Exception as e:
    print('native-warm-equal',type(e).__name__,str(e))
