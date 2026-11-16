# ryupy: pending
# 真正的立即求值注解保持 RHS/参数副作用；ClassVar 不禁止实例覆写类变量。
import typing as t
events = []

def mark(label):
    events.append(label)
    return int

def initial():
    events.append('rhs')
    return 3

class Record:
    first: t.ClassVar[mark('first')] = initial()
    second: t.ClassVar[mark('second')] = 5

assert events == ['rhs', 'first', 'second']
assert Record.__annotations__['first'] is t.ClassVar[int]
assert Record.__annotations__['second'] is t.ClassVar[int]
record = Record()
record.first = 9
assert Record.first == 3 and record.first == 9
assert Record.second == record.second == 5
assert events == ['rhs', 'first', 'second']
print('eager', events, Record.__annotations__, Record.first, record.first)
