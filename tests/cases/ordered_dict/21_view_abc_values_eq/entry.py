# 独立完整view/iterator ABC与values身份比较合同；原混合独审探针另存不裁。
from collections import OrderedDict as OD
from collections.abc import Iterable, Iterator, Sized, Container, Collection, Set

od = OD([('a', 1), ('b', 2)])
od.move_to_end('a')
for view in (od.keys(), od.values(), od.items()):
    print('view_type', type(view).__name__, isinstance(view, object), issubclass(type(view), object))
    for abc in (Iterable, Iterator, Sized, Container, Collection, Set):
        print('view_abc', abc.__name__, isinstance(view, abc), issubclass(type(view), abc))
    for backward in (False, True):
        cursor = reversed(view) if backward else iter(view)
        print('cursor_type', backward, type(cursor).__name__, isinstance(cursor, object), issubclass(type(cursor), object), iter(cursor) is cursor)
        for abc in (Iterable, Iterator, Sized, Container, Collection, Set):
            print('cursor_abc', abc.__name__, isinstance(cursor, abc), issubclass(type(cursor), abc))
        print('cursor_unconsumed', list(cursor))

values = od.values()
same = values
other = od.values()
other_owner = OD([('b', 2), ('a', 1)]).values()
foreign = object()
print('same_explicit', values.__eq__(same), values.__ne__(same))
assert values.__eq__(same) is True and values.__ne__(same) is False
for label, target in (('same_owner_new_view', other), ('other_owner', other_owner), ('foreign', foreign)):
    eq = values.__eq__(target)
    ne = values.__ne__(target)
    print('distinct_explicit', label, eq, ne, eq is NotImplemented, ne is NotImplemented)
    assert eq is NotImplemented and ne is NotImplemented
    print('distinct_expression', label, values == target, values != target, target == values, target != values)
    assert (values == target) is False and (values != target) is True
print('same_expression', values == same, values != same, same == values, same != values)
od['b'] = 20
print('live_after_compare', list(values), list(other), list(other_owner), list(od.items()))
assert list(values) == [20, 1] and list(other) == [20, 1]
print('DONE view_abc_values_eq')
