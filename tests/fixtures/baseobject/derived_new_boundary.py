# B正式支持普通类分配：保A原Derived案例，新增不init与真实布局守卫。
log = []
class Derived:
    def __init__(self):
        log.append('init')
value = object.__new__(Derived)
assert type(value) is Derived and isinstance(value, Derived)
assert log == []
value.attribute = 7
assert value.attribute == 7
assert type(Derived()) is Derived and log == ['init']
try:
    object.__new__(int)
except TypeError:
    pass
else:
    raise AssertionError('builtin int layout incorrectly allocated')
print('derived-new-ok')
