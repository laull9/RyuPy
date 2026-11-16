# ABC边界审查：位置专参、异常原值、lazy view与factory实参身份。
from collections.abc import MutableMapping, MutableSet, KeysView, Set, ItemsView, ValuesView
x={}
try:
    MutableMapping.update(x, other=3, self=4)
    print('update', x)
except Exception as e:
    print('update-error', type(e).__name__)
try:
    MutableSet.remove(set(), 'x')
except KeyError as e:
    print('remove', e.args)
v=KeysView({'old': 1})
i=iter(v)
v._mapping={'new': 2}
print('keys', list(i))
class S(Set):
    def __contains__(self, x): return False
    def __iter__(self): return iter(())
    def __len__(self): return 0
    def _from_iterable(self, it):
        print('factory-list', isinstance(it,list))
        return set(it)
print('sub', S()-[1])
class TimingSet(Set):
    def __contains__(self,x): return False
    def __len__(self): return 0
    def __iter__(self):
        print('iter-self')
        return iter(())
    def _from_iterable(self,it):
        print('factory')
        return 7
class I:
    def __iter__(self):
        print('iter-other')
        return iter(())
print(TimingSet() & I())
print(TimingSet() - set())
for typ in (ItemsView, ValuesView):
    v=typ({'a':1,'b':2})
    i=iter(v)
    print(next(i))
    v._mapping={'a':10,'b':20}
    print(next(i))
class RebindingPair:
    def __iter__(self):
        item_view._mapping = {'x': 20}
        return iter(('x', 20))
item_view = ItemsView({'x': 2})
print('items-rebind', RebindingPair() in item_view)
class RebindingValue:
    def __eq__(self, other):
        value_view._mapping = {'a': 10, 'b': 20}
        return False
value_view = ValuesView({'a': RebindingValue(), 'b': 2})
print('values-rebind', 20 in value_view)
