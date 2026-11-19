
import json
from collections import Counter,defaultdict
from types import MappingProxyType
log=[]
class D(dict):
    def items(self): log.append('items');return [('virtual',3)]
    def keys(self): log.append('keys');return ['other']
    def get(self,key,default=None):log.append(['get',key]);return 7
    def __missing__(self,key):log.append(['missing',key]);return 'fallback'
d=D(real=2)
for label,call in [('json',lambda:json.dumps(d)),('jsonindent',lambda:json.dumps(d,indent=1)),('emptyjson',lambda:json.dumps(D())),('counterempty',lambda:Counter(d)),('counterfull',lambda:(lambda c:(c.update(d),dict(c)))(Counter(base=1))),('counterminus',lambda:(lambda c:(c.subtract(d),dict(c)))(Counter())),('defaultdict',lambda:dict(defaultdict(int,d))),('proxyget',lambda:MappingProxyType(d).get('real')),('proxyitem',lambda:MappingProxyType(d)['absent']),('proxykeys',lambda:list(MappingProxyType(d).keys())),('percent',lambda:'%(absent)s' % d)]:
    log.clear()
    try: result=call();print(label,repr(result),repr(log))
    except Exception as e: print(label,type(e).__name__,str(e),repr(log))
