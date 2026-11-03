# abs 的单参数协议、完整绑定及实参求值，不扩其它数值构造器。
events = []
shared = []
class Value:
    def __abs__(self):
        events.append('abs')
        return shared
class NoneValue:
    def __abs__(self):
        events.append('none')
        return None
value = Value()
assert abs(value) is shared
assert events == ['abs']
events.clear()
assert abs(NoneValue()) is None
assert events == ['none']
events.clear()
saved = abs
def invoke(function, item):
    return function(item)
assert saved(value) is shared
assert invoke(saved, value) is shared
assert events == ['abs', 'abs']
events.clear()
assert abs(*[value]) is shared
assert abs(*[], *[value], **{}) is shared
assert events == ['abs', 'abs']
events.clear()
def mark(label, item):
    events.append(label)
    return item
def reject(label, operation, expected):
    events.clear()
    try:
        operation()
    except TypeError:
        assert events == expected
        print(label, events)
    else:
        raise AssertionError(label)
reject('zero', lambda: abs(), [])
reject('many', lambda: abs(mark('a', value), mark('b', 2), mark('c', 3)), ['a', 'b', 'c'])
reject('keyword', lambda: abs(mark('a', value), x=mark('x', 2)), ['a', 'x'])
reject('only-keyword', lambda: abs(x=mark('x', value)), ['x'])
reject('star-empty', lambda: abs(*mark('star', [])), ['star'])
reject('star-many', lambda: abs(*mark('star', [value, 2])), ['star'])
reject('star-invalid', lambda: abs(*mark('star', 3)), ['star'])
reject('double', lambda: abs(mark('a', value), **mark('kw', {'x': 2})), ['a', 'kw'])
reject('double-invalid', lambda: abs(mark('a', value), **mark('kw', 3)), ['a', 'kw'])
reject('alias-many', lambda: saved(mark('a', value), mark('b', 2)), ['a', 'b'])
reject('bad-operand', lambda: abs('text'), [])
def shadow(abs):
    return abs(7, 8)
def replacement(left, right):
    return left + right
assert shadow(replacement) == 15
def numeric(value: int) -> int:
    return abs(value)
def floating(value: float) -> float:
    return abs(value)
assert numeric(-7) == 7
assert floating(-2.5) == 2.5
assert abs(True) == 1
assert abs(-9223372036854775808) == 9223372036854775808
print('abs-ok', numeric(-7), floating(-2.5), abs(-123456789012345678901234567890))
