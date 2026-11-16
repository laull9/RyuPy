# 四个算术入口必须TypeError，不能将Add/Sub操作数值标签误派到Eq/Ne。
from collections import OrderedDict as OD

a = OD([('a', 1), ('b', 2)])
b = OD([('a', 1), ('b', 2)])
a.move_to_end('a')
b.move_to_end('a')
a_alias, b_alias = a, b
before_a, before_b = list(a.items()), list(b.items())
for label, operation in (
    ('add', lambda: a + b),
    ('sub', lambda: a - b),
    ('radd', lambda: 1 + a),
    ('rsub', lambda: 1 - a),
):
    try:
        operation()
    except TypeError as exc:
        print(label, type(exc).__name__, str(exc), exc.args)
    else:
        raise AssertionError('OrderedDict算术不得返回相等比较结果')
    print('unchanged', label, a_alias is a, b_alias is b, list(a.items()), list(b.items()))
    assert a_alias is a and b_alias is b
    assert list(a.items()) == before_a and list(b.items()) == before_b
print('DONE arithmetic_rejection')
