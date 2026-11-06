# eq抛TypeError后执行不缓存正文，保留原成功项，不能把失败结果插入缓存。
import typing as t
log=[]
class Key:
    def __init__(self,name): self.name=name
    def __hash__(self): log.append('hash:'+self.name); return 9001
    def __eq__(self,other):
        if isinstance(other,Key):
            log.append('eq:'+self.name+':'+other.name)
            raise TypeError('eq marker')
        return False
    def __repr__(self): return 'Key('+self.name+')'
a=Key('a')
b=Key('b')
first=t.Tuple[a]
log.clear()
second=t.Tuple[b]
assert second.__args__[0] is b
print('eq-typeerror-body-fallback',log)
log.clear()
third=t.Tuple[b]
assert second is not third
print('fallback-result-not-cached',log)
log.clear()
assert t.Tuple[a] is first
print('original-entry-remains',log)
