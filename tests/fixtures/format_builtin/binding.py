# ryupy: pending
# direct、别名、一等参数、局部遮蔽与完整位置参数绑定优先级。
saved = format
assert format(42, 'x') == '2a'
assert saved(42, 'x') == '2a'
def apply(fn, value, spec):
    return fn(value, spec)
assert apply(saved, 42, 'x') == '2a'
def shadow(format):
    return format(42)
assert shadow(lambda value: value + 1) == 43
assert format(*[42, 'x']) == '2a'
assert saved(*[42], **{}) == '42'

class Text(str):
    pass
result = Text('identity')
class Custom:
    def __format__(self, spec):
        return result
assert apply(saved, Custom(), '') is result

def failure(label, action):
    try:
        action()
    except TypeError as error:
        print(label, type(error).__name__, str(error))
    else:
        raise AssertionError(label)
failure('zero', lambda: format())
failure('three', lambda: saved(1, '', 2))
failure('keyword', lambda: format(value=1))
failure('spec-keyword', lambda: saved(1, format_spec=''))
failure('arity-keyword', lambda: format(1, '', 2, z=3))
failure('type-keyword', lambda: saved(Custom(), None, z=3))
for bad in (None, 1, b'x', Custom()):
    failure('bad-spec', lambda: saved(Custom(), bad))

events = []
def mark(label, value):
    events.append(label)
    return value
failure('evaluation', lambda: saved(mark('value', 1), mark('spec', ''), z=mark('kw', 2)))
assert events == ['value', 'spec', 'kw']
print('format-binding-ok')
# mixed * 求值顺序、重复 ** key 是现存共享调用债，不纳入本切片通过声明。

# 同一驱动内做MIR结构检查，不增加第五个编译应用。
def numeric(n: int) -> int:
    total: int = 0
    for i in range(n):
        total += i
    return total
assert numeric(5) == 10
