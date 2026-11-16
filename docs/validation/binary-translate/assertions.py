"""CP3.12.13候选域核心合同断言，不把单例策略强加给RyuPy。"""
TBL=bytes(range(256));EMPTY=b''
def error(fn,cls,msg):
 try: fn()
 except Exception as e:assert type(e) is cls and str(e)==msg,(type(e),str(e),msg)
 else:raise AssertionError('expected exception')
for T in (bytes,bytearray):
 s=T(b'abca');f=s.translate;p=T.__name__
 for fn,msg in [
 (lambda:f(),'translate() takes at least 1 positional argument (0 given)'),
 (lambda:f(None,b'',b''),'translate() takes at most 2 arguments (3 given)'),
 (lambda:f(table=None),'translate() takes at least 1 positional argument (0 given)'),
 (lambda:f(None,table=None),"'table' is an invalid keyword argument for translate()"),
 (lambda:f(None,oops=1),"'oops' is an invalid keyword argument for translate()"),
 (lambda:f(None,b'',delete=b''),'translate() takes at most 2 arguments (3 given)'),
 (lambda:f(1),"a bytes-like object is required, not 'int'"),
 (lambda:f(None,None),"a bytes-like object is required, not 'NoneType'"),
 (lambda:f(None,'x'),"a bytes-like object is required, not 'str'"),
 (lambda:T.translate(),f'unbound method {p}.translate() needs an argument'),
 (lambda:T.translate(1,None,b'',bad=0),f"descriptor 'translate' for '{p}' objects doesn't apply to a 'int' object")]:error(fn,TypeError,msg)
 error(lambda:f(b'x',1),ValueError,'translation table must be 256 characters long')
 for data in (b'',b'a',b'abc'):
  s=T(data)
  for table,delete in ((None,b''),(TBL,b''),(None,b'Z'),(TBL,b'Z')):
   r=s.translate(table,delete);assert r==s;assert (r is s)==(T is bytes)
  r=s.translate(None,data);assert r==T();assert (r is s)==(T is bytes and not data)
 print(p,'binding and identity assertions passed')
s=b'a'; table=bytearray(TBL);table[97]=98;r=s.translate(table)
print('changed single byte: is source',r is s,'is cached b',r is bytes([98]))
s=b'abc';r=s.translate(None,b'abc');print('delete all: is source',r is s,'is empty singleton',r is EMPTY)
