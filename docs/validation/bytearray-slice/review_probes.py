"""第一版实现审查：精确 list 的实时变异与下标/删除算法穷举。"""
from itertools import product
class I:
    def __init__(self,action): self.action=action
    def __index__(self): self.action(); return 65
for mode in ['replace','append','clear','shrink']:
    a=bytearray(b'abcdef'); source=[]
    def action():
        if mode=='replace': source[1]=90
        elif mode=='append': source.append(90)
        elif mode=='clear': source.clear()
        else: del source[1:]
    source[:]=[I(action),66]
    a[1:3]=source
    print('list-'+mode,bytes(a))
MIN=-(2**63);MAX=2**63-1
values=[None,MIN-10,MIN,-15,-8,-3,-1,0,1,3,8,15,MAX,MAX+10]
def sat(x): return max(MIN,min(MAX,x))
def bounds(sl,n):
    step=1 if sl.step is None else sat(sl.step)
    if step==0: raise ValueError
    if step==MIN: step=-MAX
    start=(MAX if step<0 else 0) if sl.start is None else sat(sl.start)
    stop=(MIN if step<0 else MAX) if sl.stop is None else sat(sl.stop)
    def bound(x):
        if x<0:x+=n
        if x<0:return -1 if step<0 else 0
        if x>=n:return n-1 if step<0 else n
        return x
    start,stop=bound(start),bound(stop)
    count=(1+(stop-start-1)//step if stop>start else 0) if step>0 else (1+(start-stop-1)//-step if start>stop else 0)
    return start,stop,step,count
cases=0
for n,s,e,k in product(range(9),values,values,values):
    if k==0: continue
    sl=slice(s,e,k); start,stop,step,count=bounds(sl,n)
    selected=list(range(n))[sl]
    assert selected==[start+i*step for i in range(count)], (n,sl,selected,(start,stop,step,count))
    if step!=1 and count:
        original=list(range(n)); content=original[:]
        chosen=start if step>0 else start+(count-1)*step
        stride=abs(step); write=chosen; remaining=count
        for read in range(write,n):
            if remaining>0 and read==chosen:
                remaining-=1
                if remaining:chosen+=stride
            else:content[write]=content[read];write+=1
        content=content[:n-count]
        expected=bytearray(original);expected[sl]=b''
        assert content==list(expected),(n,sl,content,expected)
    cases+=1
print('normalization/deletion translated algorithm cases:',cases)
