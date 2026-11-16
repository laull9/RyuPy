import sys, types
from dataclasses import dataclass
print(sys.version)
def test(label, fn):
    try: print(label, 'RETURN', repr(fn()))
    except BaseException as e: print(label, type(e).__name__, str(e))
class C: pass
c=C(); c.x=1
for args in [(),(c,), (c,'x',1), (c,1),(c,None),(c,b'x')]:
    test('arity/type '+str(len(args))+' '+type(args[-1]).__name__ if args else 'arity0',lambda a=args: delattr(*a))
for args in [(),(c,), (c,'x'), (c,1),(c,'x',1)]:
    test('keyword '+str(len(args)),lambda a=args: delattr(*a,name='x'))
class S(str):
    def __str__(self): raise RuntimeError('STR')
    def __eq__(self,o): raise RuntimeError('EQ')
    def __hash__(self): raise RuntimeError('HASH')
c.x=1; test('str subclass ordinary', lambda: delattr(c,S('x')))
class D:
    def __delattr__(self,n): print('override arg',type(n).__name__,n is name); return 42
name=S('x'); test('override return/raw name',lambda: delattr(D(), name))
class E:
    def __delattr__(self,n): raise ValueError('custom')
test('override exception',lambda: delattr(E(),'x'))
e=E(); e.__delattr__=lambda n: print('instance hook'); test('instance hook ignored', lambda: delattr(e,'x'))
class P:
    @property
    def x(self): return 1
    @x.deleter
    def x(self): print('deleter'); return 7
p=P(); test('property delete',lambda: delattr(p,'x'))
class Q:
    @property
    def x(self): return 1
test('property no deleter',lambda: delattr(Q(),'x'))
class B: x=1
class A(B): pass
a=A(); test('inherited instance',lambda: delattr(a,'x')); test('inherited class',lambda: delattr(A,'x')); A.x=2; test('class own',lambda: delattr(A,'x')); print('class fallback',A.x)
test('missing',lambda: delattr(c,'x')); c.__dict__['']=3;test('empty name',lambda:delattr(c,'')); c.__dict__['a b']=2;test('nonidentifier',lambda:delattr(c,'a b'))
m=types.ModuleType('m');m.x=1;test('module own',lambda:delattr(m,'x'));test('module missing',lambda:delattr(m,'x'));test('module dict',lambda:delattr(m,'__dict__'))
def f(): pass
f.x=1
for n in ['x','x','__module__','__module__','__doc__','__annotations__','__name__','__qualname__','__dict__','__defaults__','__kwdefaults__','__code__','__globals__','__closure__']:
    test('function '+n,lambda n=n:delattr(f,n))
print('function after',f.__module__,f.__doc__,f.__annotations__)
for o,n in [(object(),'x'),(1,'x'),([],'append'),(int,'x'),(slice(1),'start')]: test('readonly '+type(o).__name__+' '+n,lambda o=o,n=n:delattr(o,n))
@dataclass(frozen=True)
class F: x:int=1
test('frozen',lambda:delattr(F(),'x'))
class Desc:
    def __get__(self,o,t): return 1
    def __delete__(self,o): print('descriptor delete');return 5
class DD: x=Desc()
test('descriptor',lambda:delattr(DD(),'x'))
class SetOnly:
    def __set__(self,o,v): pass
class SO: x=SetOnly()
test('descriptor set only',lambda:delattr(SO(),'x'))
class Slot: __slots__=('x',)
s=Slot();s.x=1;test('slot',lambda:delattr(s,'x'));test('slot missing',lambda:delattr(s,'x'))
class Meta(type):
    def __delattr__(self,n):print('meta override');return 9
class MC(metaclass=Meta):x=1
test('metaclass',lambda:delattr(MC,'x'))
