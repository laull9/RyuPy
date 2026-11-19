# CPython 3.12.13 slice 公共合同参考；一次有界执行，不导入项目。
import sys, operator
print(sys.version)
def probe(label, fn):
    try: print(label, '=>', repr(fn()))
    except BaseException as e: print(label, '=>', type(e).__name__, str(e))
for n in range(5):
    probe('ctor '+str(n), lambda n=n: slice(*range(n)))
    probe('ctor kw '+str(n), lambda n=n: slice(*range(n), stop=1))
x=[]; s=slice(x,x,x)
print('identity', s.start is x, s.stop is x, s.step is x, type(s) is slice, isinstance(s,slice), isinstance(slice,type), issubclass(slice,object))
for attr in ('start','stop','step','other','__class__'):
    probe('set '+attr, lambda attr=attr: setattr(s,attr,1))
    probe('del '+attr, lambda attr=attr: delattr(s,attr))
probe('subclass', lambda: type('S',(slice,),{}))
log=[]
class Index:
    def __init__(self, name, value): self.name,self.value=name,value
    def __index__(self):
        log.append(self.name)
        if isinstance(self.value,BaseException): raise self.value
        return self.value
for length,step,start,stop in [(10,2,1,9),(-1,0,1,9),(10,0,1,9),(10,1,'bad',9),('bad',0,1,9),(10,True,1,9)]:
    log.clear(); probe('indices order '+repr((length,step,start,stop)), lambda: slice(Index('start',start),Index('stop',stop),Index('step',step)).indices(Index('length',length))); print('log',log)
for length in (None, 1.5,-1,0,10,10**100):
    probe('indices '+str(length),lambda length=length:slice(-10**101,10**101,10**90).indices(length))
for triple in [(None,None,-10**100),(None,None,10**100),(-1,None,-1),(None,-1,-1),(True,False,True)]:
    probe('wide '+repr(triple),lambda triple=triple:slice(*triple).indices(10**100))
for fn in [lambda:s.indices(),lambda:s.indices(1,2),lambda:s.indices(length=2),lambda:slice.indices(1,2),lambda:slice.__new__(slice),lambda:slice.__new__(slice,1,x=2)]: probe('methods',fn)
class Token:
    def __bool__(self): log.append('truth-token'); return False
    def __repr__(self): return 'TOKEN'
token=Token()
class Cmp:
    def __init__(self,name): self.name=name
    def __eq__(self,other):log.append(self.name+'.eq');return token
    def __ne__(self,other):log.append(self.name+'.ne');return token
    def __lt__(self,other):log.append(self.name+'.lt');return token
    def __le__(self,other):log.append(self.name+'.le');return token
    def __gt__(self,other):log.append(self.name+'.gt');return token
    def __ge__(self,other):log.append(self.name+'.ge');return token
    def __hash__(self):log.append(self.name+'.hash');return 11
for name,op in [('eq',operator.eq),('ne',operator.ne),('lt',operator.lt),('le',operator.le),('gt',operator.gt),('ge',operator.ge)]:
    log.clear(); probe('compare '+name,lambda op=op:op(slice(Cmp('a'),2,3),slice(Cmp('b'),2,3)));print('log',log)
c=Cmp('same');log.clear();probe('same fields',lambda:slice(c,c,c)==slice(c,c,c));print('log',log)
for name,op in [('eq',operator.eq),('lt',operator.lt)]:probe('mixed '+name,lambda op=op:op(slice(1),1))
for name in ('__eq__','__ne__','__lt__'):probe('slot mixed '+name,lambda name=name:getattr(slice(1),name)(1))
log.clear();s=slice(Cmp('start'),Cmp('stop'),Cmp('step'));probe('hash1',lambda:hash(s));probe('hash2',lambda:hash(s));print('log',log)
log.clear();probe('hash fail middle',lambda:hash(slice(Cmp('start'),[],Cmp('step'))));print('log',log)
print('hash tuple equality',hash(slice(1,2,3))==hash((1,2,3)))
