# 数值consumer按现Python协议优先；末尾保留真实调用者/上下文错误链。
events = []
class S(str):
    pass
assert int(S('123')) == 123
assert int(S('ff'), 16) == 255
assert float(S('1.25')) == 1.25
assert ord(S('A')) == 65

class Numeric(str):
    def __int__(self):
        events.append('int')
        return 7
    def __float__(self):
        events.append('float')
        return 2.5
    def __index__(self):
        events.append('index')
        return 9
    def __str__(self):
        events.append('unexpected-str')
        raise AssertionError('raw payload must not call str')

value = Numeric('10')
assert int(value) == 7
assert float(value) == 2.5
assert int(value, 16) == 16
assert ord(Numeric('B')) == 66
assert events == ['int', 'float']

class Indexed(str):
    def __index__(self):
        events.append('index')
        return 11
assert int(Indexed('2')) == 11
assert float(Indexed('2')) == 11.0
assert events == ['int', 'float', 'index', 'index']

calls = []
def mark(value):
    calls.append(value)
    return value

try:
    str(mark(1), mark(2), mark(3), mark(4))
except TypeError:
    pass
else:
    raise AssertionError('arity')
assert calls == [1, 2, 3, 4]
assert str(*[123]) == '123'
assert str(object=123) == '123'
assert str(**{'object': 123}) == '123'
try:
    str(123, object=mark(5))
except TypeError:
    pass
else:
    raise AssertionError('duplicate')
assert calls == [1, 2, 3, 4, 5]

class Bad:
    def __str__(self):
        return 123

def convert():
    return str(Bad())

print('str-numeric-binding-errors')
try:
    raise ValueError('outer')
except ValueError:
    convert()
