# OD对OD看顺序、对普通dict忽略顺序；值相等先于顺序且eq/ne独立。
from collections import OrderedDict as OD

left = OD(a=1, b=2)
right = OD(b=2, a=1)
plain = {'b': 2, 'a': 1}
print('order', left == right, left != right, left == plain, plain == left, left != plain, plain != left)
print('explicit', left.__eq__(right), left.__ne__(right), left.__eq__(plain), left.__ne__(plain))
print('foreign_builtin', left.__eq__([]) is NotImplemented, left.__ne__([]) is NotImplemented, left == [], left != [])
class Value:
    def __init__(self, name):
        self.name = name
    def __eq__(self, other):
        print('value_eq', self.name, other.name)
        return True
    def __ne__(self, other):
        raise AssertionError('映射值比较只调用eq')
a = OD(a=Value('la'), b=Value('lb'))
b = OD(b=Value('rb'), a=Value('ra'))
print('values_before_order_eq', a == b)
print('values_before_order_ne', a != b)
shared = Value('shared')
print('identity_shortcut', OD(a=shared) == OD(a=shared))
class Verdict:
    def __bool__(self):
        print('value_truth')
        return True
class TruthValue:
    def __eq__(self, other):
        print('value_result')
        return Verdict()
print('value_truth_protocol', OD(a=TruthValue()) == OD(a=TruthValue()))
class Foreign:
    def __eq__(self, other):
        print('foreign_eq')
        return True
    def __ne__(self, other):
        print('foreign_ne')
        return True
print('independent_eq', OD() == Foreign())
print('independent_ne', OD() != Foreign())
try:
    left < right
except TypeError as exc:
    print('ordering', type(exc).__name__, str(exc))

# 内容比较的value回调依raw插入序；普通dict在左仍由OD优先比较。
raw_left = OD(a=Value('La'), b=Value('Lb'), c=Value('Lc'))
raw_right = OD(a=Value('Ra'), b=Value('Rb'), c=Value('Rc'))
raw_left.move_to_end('a')
raw_right.move_to_end('a')
print('raw_same_public_order', list(raw_left), list(raw_right))
print('raw_same_eq', raw_left == raw_right)
raw_right.move_to_end('b')
print('raw_different_public_order', list(raw_left), list(raw_right))
print('raw_different_eq', raw_left == raw_right)
print('raw_different_ne', raw_left != raw_right)
plain_values = {'c': Value('Dc'), 'b': Value('Db'), 'a': Value('Da')}
print('od_left_plain', raw_left == plain_values)
print('plain_left_od', plain_values == raw_left)
print('od_left_plain_ne', raw_left != plain_values)
print('plain_left_od_ne', plain_values != raw_left)
print('DONE equality')
