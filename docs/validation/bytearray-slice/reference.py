"""CPython 3.12.13 bytearray 切片写入：结果、回调顺序与失败副作用基线。"""
import sys
print(sys.version)
def run(name, f):
    global a, log
    a=bytearray(b'abcdef'); log=[]
    try:
        f()
        result='ok'
    except Exception as e: result=type(e).__name__+': '+str(e)
    print(name, result, bytes(a), log, sep=' | ')
def assign(key, value): a[key]=value
class Index:
    def __init__(self, name, value, action=None): self.name,self.value,self.action=name,value,action
    def __index__(self):
        log.append(self.name)
        if self.action: self.action()
        return self.value
class Source:
    def __init__(self, values, action=None): self.values,self.action=values,action
    def __iter__(self):
        log.append('iter')
        if self.action: self.action()
        return It(self.values)
    def __len__(self): log.append('len'); return len(self.values)
class It:
    def __init__(self, values): self.values=iter(values)
    def __iter__(self): log.append('iterator.iter'); return self
    def __next__(self): log.append('next'); return next(self.values)
    def __length_hint__(self): log.append('hint'); return 2
for name,key,val in [
 ('grow',slice(1,3),b'XYZ'),('shrink',slice(1,4),b'X'),('reverse-bound-insert',slice(4,1),b'XY'),
 ('extended',slice(None,None,2),b'XYZ'),('negative-step',slice(None,None,-2),b'XYZ'),
 ('extended-mismatch',slice(None,None,2),b'XY'),('extended-empty-deletes',slice(None,None,2),b''),
 ('negative-empty-deletes',slice(None,None,-2),[]),('empty-selection-insert',slice(3,3,2),b'X'),
 ('zero-step',slice(None,None,0),Source([65])),('int-rhs',slice(None),3),('bool-rhs',slice(None),True),
 ('str-rhs',slice(None),'XY'),('huge-step',slice(None,None,10**100),b'X'),
 ('huge-bounds',slice(-10**100,10**100),b'X'),('bad-element',slice(1,3),Source([65,256,66])),
 ('element-index',slice(1,3),Source([Index('element',65),66]))]:
    run(name,lambda key=key,val=val:assign(key,val))
run('self',lambda:assign(slice(1,3),a))
run('self-reverse',lambda:assign(slice(None,None,-1),a))
run('alias-view-overlap',lambda:assign(slice(1,5),memoryview(a)[:4]))
run('alias-view-resize',lambda:assign(slice(1,2),memoryview(a)[:4]))
run('view-c-raw',lambda:assign(slice(1,3),memoryview(b'XY').cast('c')))
run('view-noncontiguous',lambda:assign(slice(1,3),memoryview(b'WXYZ')[::2]))
run('index-order-bytes',lambda:assign(slice(Index('start',1),Index('stop',3),Index('step',1)),b'XY'))
run('index-order-iter',lambda:assign(slice(Index('start',1),Index('stop',3),Index('step',1)),Source([65,66])))
run('index-order-self',lambda:assign(slice(Index('start',1),Index('stop',3),Index('step',1)),a))
run('iter-mutates-grow',lambda:assign(slice(1,None),Source([65],lambda:a.extend(b'XYZ'))))
run('iter-mutates-shrink',lambda:assign(slice(1,None),Source([65],lambda:a.clear())))
run('iter-mutates-error',lambda:assign(slice(1,3),Source([65,256],lambda:a.extend(b'XYZ'))))
run('index-mutates-grow',lambda:assign(slice(Index('start',-2,lambda:a.extend(b'XYZ')),None),b'Q'))
run('index-mutates-clear',lambda:assign(slice(Index('start',1,lambda:a.clear()),None),b'Q'))
run('element-mutates-grow',lambda:assign(slice(1,None),[Index('element',65,lambda:a.extend(b'XYZ'))]))
run('index-twice-mutation',lambda:assign(slice(Index('start',1,lambda:a.extend(b'Z')),Index('stop',3),Index('step',1)),Source([65,66])))
for label,key,value in [('same',slice(1,3),b'XY'),('grow',slice(1,2),b'XY'),('delete',slice(1,3),b''),('noop',slice(2,2),b''),('extended',slice(None,None,2),b'XYZ'),('ext-delete',slice(None,None,2),b''),('ext-mismatch',slice(None,None,2),b'XY'),('invalid',slice(1,2),[256])]:
    def exported(key=key,value=value):
        view=memoryview(a)
        try: assign(key,value)
        finally: log.append(('view',view.tobytes())); view.release()
    run('export-'+label,exported)
def release_during_iter():
    view=memoryview(a)
    assign(slice(1,2),Source([65,66],view.release))
run('release-during-iter',release_during_iter)
def export_during_iter():
    views=[]
    try: assign(slice(1,2),Source([65,66],lambda:views.append(memoryview(a))))
    finally:
        for v in views: v.release()
run('acquire-during-iter',export_during_iter)
class IntOnly:
    def __int__(self): log.append('int'); return 65
class BytesOnly:
    def __bytes__(self): log.append('bytes'); return b'XY'
run('int-only-element',lambda:assign(slice(None),[IntOnly()]))
run('bytes-only-rhs',lambda:assign(slice(None),BytesOnly()))
run('index-only-rhs',lambda:assign(slice(None),Index('rhs-index',2)))
for kind in ['bytearray','bytes','self','view','list','tuple']:
    def cb_rhs(kind=kind):
        rhs={'bytearray':bytearray(b'XY'),'bytes':b'XY','self':a,'view':memoryview(b'XY'),'list':[65,66],'tuple':(65,66)}[kind]
        assign(slice(Index('start',1),Index('stop',3),Index('step',1)),rhs)
    run('callbacks-'+kind,cb_rhs)
class SelfIter:
    def __init__(self): self.n=0
    def __iter__(self): log.append('self-iter'); return self
    def __length_hint__(self): log.append('self-hint'); return 2
    def __next__(self):
        self.n+=1; log.append('self-next')
        if self.n>2: raise StopIteration
        return 65
run('self-iterator-hint',lambda:assign(slice(1,3),SelfIter()))
class Sequence:
    def __len__(self): log.append('sequence-len'); return 2
    def __getitem__(self,n):
        log.append(('getitem',n))
        if n==2: raise IndexError
        return 65
run('sequence-length',lambda:assign(slice(1,3),Sequence()))
class Twice:
    def __init__(self): self.n=0
    def __index__(self):
        self.n+=1; log.append(('twice',self.n))
        if self.n==2: raise RuntimeError('second index')
        return 1
run('second-index-fails',lambda:assign(slice(Twice(),3),Source([65],lambda:a.extend(b'Z'))))
def released_view():
    v=memoryview(b'XY'); v.release()
    assign(slice(Index('start',1),3),v)
run('released-view-order',released_view)
class NumberAndIter:
    def __index__(self): log.append('number-index'); return 3
    def __iter__(self): log.append('number-iter'); return iter([65])
run('number-and-iter',lambda:assign(slice(None),NumberAndIter()))
class CountIndex:
    def __init__(self,value,action): self.n,self.value,self.action=0,value,action
    def __index__(self):
        self.n+=1; log.append(('count-index',self.n)); self.action(self.n); return self.value
run('self-copy-between-index-rounds',lambda:assign(slice(CountIndex(1,lambda n:a.__setitem__(0,64+n)),3),a))
def exact_source_mutation():
    source=bytearray(b'XY')
    assign(slice(CountIndex(1,lambda n:source.__setitem__(0,90)),3),source)
run('exact-source-index-mutation',exact_source_mutation)
def view_release_second_round():
    v=memoryview(a)[:2]
    assign(slice(CountIndex(1,lambda n:v.release() if n==2 else None),3),v)
run('view-release-second-round',view_release_second_round)
