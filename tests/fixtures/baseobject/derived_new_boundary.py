# Ryu A阶段专用边界：不能把未实现的派生分配伪装为成功。
class Derived:
    pass
try:
    object.__new__(Derived)
except NotImplementedError:
    print('derived-new-pending')
else:
    raise AssertionError('unsupported derived allocation silently accepted')
