# 同一键命中仍hash，等价不同键仍eq，未知用户回调不得静默跳过。
import typing as t
log=[]
class Key:
    def __init__(self,token): self.token=token
    def __hash__(self): log.append('hash:'+self.token); return 701
    def __eq__(self,other):
        log.append('eq:'+self.token)
        return isinstance(other,Key) and self.token==other.token
    def __repr__(self): return 'Key('+self.token+')'
k=Key('a')
a=t.Tuple[k]
log.clear()
assert t.Tuple[k] is a
assert log==['hash:a']
print('same-hit',log)
log.clear()
assert t.Tuple[Key('a')] is a
print('equal-hit',log)
assert log==['hash:a','eq:a']
class Bad:
    def __hash__(self): raise ValueError('hash marker')
try:
    t.Tuple[Bad()]
except ValueError as e:
    print('hash-valueerror',str(e))
else:
    assert False
