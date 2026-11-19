"""CPython 3.12.13 bytes/bytearray.translate 独立合同探针；不运行第三方项目。"""
import sys, json, array
print(sys.version)
N=0
EMPTY=b""
sys.unraisablehook=lambda info: print(json.dumps({"unraisable":type(info.exc_value).__name__,"message":str(info.exc_value)},ensure_ascii=False))

def run(name, fn):
 global N
 N+=1
 try:
  value=fn()
  print(json.dumps({'case':name,'ok':repr(value)},ensure_ascii=False))
 except BaseException as e:
  print(json.dumps({'case':name,'error':type(e).__name__,'message':str(e)},ensure_ascii=False))

identity=bytes(range(256)); inverse=identity[::-1]
for T in (bytes,bytearray):
 p=T.__name__; s=T(b'a\x00b\xffaa'); f=s.translate
 for name,args,kw in [
 ('missing',(),{}),('none',(None,),{}),('identity',(identity,),{}),('inverse',(inverse,),{}),
 ('delete',(None,b'a\xff'),{}),('delete_kw',(None,),{'delete':b'a'}),
 ('delete_original',(inverse,b'a'),{}),('delete_duplicates',(None,b'aaaa'),{}),
 ('table_kw',(),{'table':None}),('table_kw_with_pos',(None,),{'table':None}),
 ('delete_only',(),{'delete':b'a'}),('extra',(None,b'',b''),{}),
 ('unknown',(None,),{'oops':1}),('extra_unknown',(None,b'',b''),{'oops':1}),
 ('duplicate',(None,b''),{'delete':b''}),('badtable_baddelete',(b'x',1),{}),
 ('badtype_baddelete',(1,1),{}),('none_delete_none',(None,None),{}),
 ('badtable_unknown',(1,),{'oops':1})]:
  run(p+'.'+name,lambda args=args,kw=kw:f(*args,**kw))
 for val in (b'',b'a',b'abc',bytes(range(256))):
  src=T(val)
  for label,table,delete in [('none',None,b''),('id',identity,b''),('absent',None,b'z'),('deleteall',None,val),('inverse',inverse,b'')]:
   run(p+'.identity.'+val.hex()+'.'+label,lambda src=src,table=table,delete=delete:(lambda r:(type(r).__name__,bytes(r).hex(),r is src,r is EMPTY))(src.translate(table,delete)))
 for role in ('table','delete'):
  candidates={'bytes':identity,'bytearray':bytearray(identity),'viewB':memoryview(identity),'viewc':memoryview(identity).cast('c'),'viewH':memoryview(identity).cast('H'),'view2D':memoryview(identity).cast('B',(16,16)),'strided':memoryview(identity)[::2],'emptystrided':memoryview(identity)[0:0:2],'arrayH':array.array('H',[0]*128),'text':'abc','list':[1,2],'int':256}
  dead=memoryview(identity);dead.release();candidates['released']=dead
  for label,v in candidates.items():
   run(p+'.buffer.'+role+'.'+label,lambda role=role,v=v:s.translate(v,b'') if role=='table' else s.translate(None,v))
 for size in (0,1,255,256,257):
  for source in (T(),T(b'abc')):
   run(p+'.length.'+str(size)+'.empty'+str(not source),lambda source=source,size=size:source.translate(bytes(size)))
 for label,args,kw in [('missing',(),{}),('badself',(1,),{}),('cross',(bytearray(b'a') if T is bytes else b'a',None),{}),('badself_extra',(1,None,b'',0),{'bad':1}),('goodself_missing',(s,),{}),('goodself',(s,None),{}),('self_kw',(),{'self':s,'table':None})]:
  run(p+'.root.'+label,lambda args=args,kw=kw:T.translate(*args,**kw))
 src=T(identity);run(p+'.source_table_alias',lambda:src.translate(src));run(p+'.source_delete_alias',lambda:src.translate(None,src));run(p+'.all_alias',lambda:src.translate(src,src))
 if T is bytearray:
  src=bytearray(b'abc');bound=src.translate;src[:]=b'xyz';run(p+'.bound_live',lambda:bound(None,b'x'))
  tab=bytearray(identity); view=memoryview(tab);run(p+'.table_delete_view_alias',lambda:bytearray(b'abc').translate(tab,view));view.release();run(p+'.alias_export_released',lambda:(tab.append(0),len(tab)))

class Noise:
 def __bytes__(self):events.append('bytes');return identity
 def __iter__(self):events.append('iter');return iter(identity)
 def __index__(self):events.append('index');return 256
 def __len__(self):events.append('len');return 256
 def __getitem__(self,k):events.append('getitem');return k
class Buffer:
 def __init__(self,name,data,acquire=None,release=None):self.name=name;self.data=data;self.acquire=acquire;self.release=release
 def __buffer__(self,flags):
  events.append(('get',self.name,flags))
  if self.acquire:self.acquire()
  return memoryview(self.data)
 def __release_buffer__(self,view):
  events.append(('release',self.name))
  if self.release:self.release()

def callback_case(T,mode):
 global events
 events=[]; src=T(b'abca');tab=bytearray(identity)
 def resize():src.extend(b'z')
 def mutate():src[0]=ord('z')
 table=Buffer('table',tab)
 delete=Buffer('delete',b'a')
 if mode=='badtable':table=Buffer('table',b'x')
 if mode=='baddelete':delete=Noise()
 if mode=='noise':table=Noise()
 if mode=='resize_acquire':delete.acquire=resize
 if mode=='mutate_acquire':delete.acquire=mutate
 if mode=='table_mutate_acquire':delete.acquire=lambda:tab.__setitem__(ord('b'),ord('Z'))
 if mode=='release_resize':table.release=resize
 if mode=='release_raise':table.release=lambda:(_ for _ in ()).throw(RuntimeError('release boom'))
 if mode=='acquire_raise':delete.acquire=lambda:(_ for _ in ()).throw(RuntimeError('acquire boom'))
 try:r=src.translate(table,delete); result=('ok',bytes(r).hex(),bytes(src).hex())
 except BaseException as e:result=(type(e).__name__,str(e),bytes(src).hex())
 return result,events
for T in (bytes,bytearray):
 for mode in ('normal','badtable','baddelete','noise','resize_acquire','mutate_acquire','table_mutate_acquire','release_resize','release_raise','acquire_raise'):
  run(T.__name__+'.callback.'+mode,lambda T=T,mode=mode:callback_case(T,mode))

class BS(bytes):
 def __len__(self):events.append('sub_len');return 1
 def __getitem__(self,k):events.append('sub_get');return 0
 def __bytes__(self):events.append('sub_bytes');return b'WRONG'
class BA(bytearray):
 def __len__(self):events.append('sub_len');return 1
 def __getitem__(self,k):events.append('sub_get');return 0
for T in (BS,BA):
 for name,table in [('none',None),('id',identity)]:
  src=T(b'abc');events=[]
  run(T.__name__+'.source.'+name,lambda src=src,table=table:(lambda r:(type(r).__name__,r,r is src,events))(src.translate(table)))
for T in (bytes,bytearray):
 for C in (BS,BA):
  events=[];run(T.__name__+'.subbuffer.'+C.__name__,lambda T=T,C=C:(T(b'abc').translate(C(identity),C(b'a')),events))
# 绑定必须在获取buffer前完成；总实参和必填posonly的优先级。
for T in (bytes,bytearray):
 for label,args,kw in [('zero_manykw',(),{'table':None,'x':0,'y':0}),('two_unknown',(None,b''),{'x':0}),('missing_unknown',(),{'x':0}),('one_two_kw',(None,),{'x':0,'delete':b''})]:
  run(T.__name__+'.binding_extra.'+label,lambda T=T,args=args,kw=kw:T(b'').translate(*args,**kw))
 for label in ('success','badtable','baddelete','deaddelete'):
  def lifecycle(T=T,label=label):
   tab=bytearray(identity if label!='badtable' else b'x'); view=memoryview(tab)
   delete=memoryview(bytearray(b'a'))
   if label=='deaddelete':delete.release()
   try:r=T(b'abc').translate(view,1 if label=='baddelete' else delete);status=('ok',bytes(r).hex())
   except Exception as e:status=(type(e).__name__,str(e))
   view.release();delete.release();tab.append(0)
   return status,len(tab)
  run(T.__name__+'.lifecycle.'+label,lifecycle)

def export_observation(mode):
 global events
 events=[];tab=bytearray(identity);view=memoryview(tab)
 def attempt(fn):
  try:fn();events.append('action_ok')
  except Exception as e:events.append((type(e).__name__,str(e)))
 def get():
  if mode=='release_view':attempt(view.release)
  elif mode=='resize_table':attempt(lambda:tab.append(0))
 delete=Buffer('delete',b'a',acquire=get)
 r=b'abc'.translate(view if mode=='release_view' else tab,delete)
 view.release();tab.append(0)
 return r,events,len(tab)
for mode in ('release_view','resize_table'):run('export_observation.'+mode,lambda mode=mode:export_observation(mode))

def saved_export():
 src=bytearray(b'abc');bound=src.translate
 src.append(100);r=bound(None);src.append(101)
 return r,src
run('bound_no_retained_export',saved_export)
print('CASES',N)
