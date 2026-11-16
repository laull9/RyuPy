# 完整split合同，index事件在separator校验前且每次恰好一次。
events=[]
class Limit:
    def __index__(self):
        events.append('index')
        return 1
class S(str):
    def split(self, *args, **kw): return ['override']
for fn in [lambda: 'a=b=c'.split('=',Limit()), lambda: 'a=b=c'.split(sep='=',maxsplit=Limit()),lambda: str.split(S('a=b=c'),'=',Limit())]:
    events.clear()
    assert fn()==['a','b=c'] and events==['index']
assert S('a=b').split('=')==['override']
saved='a=b=c'.split
assert saved('=',1)==['a','b=c']
for separator, expected in [(42,TypeError),('',ValueError)]:
    events.clear()
    try: ''.split(separator,Limit())
    except expected: pass
    else: raise AssertionError()
    assert events==['index']
assert 'a\ud83db'.split('\ud83d')==['a','b']
assert '😀x😀'.split('\ud83d')==['😀x😀']
assert '😀x😀'.split('😀')==['','x','']
assert 'a,,'.split(',')==['a','','']
assert '  a  b  '.split(None,1)==['a','b  ']
assert '  a  b  '.split(None,0)==['a  b  ']
assert '  '.split(None,0)==[]
for n in [2**100,-2**100]:
    try: ''.split('',n)
    except OverflowError: pass
    else: raise AssertionError()
print('split-binding-and-boundaries: ok')
