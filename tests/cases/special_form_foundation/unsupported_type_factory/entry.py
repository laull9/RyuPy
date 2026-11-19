import typing as t
# 不调用候选未公开的CP cleanup，独立进程天然空SpecialForm缓存。
cs=[type('C'+str(i),(),{}) for i in range(65)]
first=t.Optional[cs[0]]
for c in cs[1:64]: t.Optional[c]
print('inner-touch',t.Union[cs[0],type(None)] is first)
t.Optional[cs[64]]
print('outer-miss-inner-hit',t.Optional[cs[0]] is first)
for c in cs[1:65]: t.Optional[c]
print('both-evicted',t.Optional[cs[0]] is first)
a=t.Callable[[int,str],bytes]
print('callable',a.__args__ is a.__args__,t.get_args(a) is t.get_args(a),len(a.__args__),len(t.get_args(a)))
print('types',type(t.Union).__module__,type(t.Union).__name__,type(t.Literal).__name__,type(t.Callable).__name__)
