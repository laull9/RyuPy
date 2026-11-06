import typing as t
C=t.Callable
events=[]
class Outer(tuple):
    def __len__(self): events.append('outer.len'); return super().__len__()
    def __iter__(self): events.append('outer.iter'); return super().__iter__()
class Args(tuple):
    def __iter__(self): events.append('args.iter'); return super().__iter__()
    def __hash__(self): events.append('args.hash'); return super().__hash__()
a=C[Outer((Args((int,)),str))]
assert events==['outer.len','outer.iter','args.hash','args.iter']
print('custom-cold',events)
events.clear(); assert C[Outer((Args((int,)),str))] is a
assert events==['outer.len','outer.iter','args.hash']; print('custom-warm',events)
# unhashable args item: cached hashing fails; uncached body succeeds, no insertion.
b=C[[[int]],str]; c=C[[[int]],str]
assert b==c and b is not c
print('unhashable-inner accepted and uncached: ok')
# 构造期就读取元数据，不等到第一次__parameters__。
class Item:
    @property
    def __parameters__(self): events.append('metadata'); return ()
item=Item(); events.clear(); C[[item],bytes]
assert events==['metadata']; print('constructor-consumption',events)
