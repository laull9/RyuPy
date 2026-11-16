# 仅补新 C3 槽选择涉及的 __new__ 三观察，不扩元类／原生布局。
events = []
class Root:
    pass
class Left(Root):
    pass
class Right(Root):
    def __new__(cls):
        events.append(('new', cls.__name__))
        return object.__new__(cls)
    def __init__(self):
        events.append(('init', type(self).__name__))
class Combined(Left, Right):
    pass
x = Combined()
print('new_right_branch', repr([type(x).__name__, events]))
events.clear()
class Child(Combined):
    pass
y = Child()
print('single_child_new_init', repr([type(y).__name__, events]))
events.clear()
sentinel = object()
class Other(Root):
    def __new__(cls):
        events.append(('new-other', cls.__name__))
        return sentinel
    def __init__(self):
        events.append(('unexpected-init', type(self).__name__))
class ReturnsOther(Left, Other):
    pass
z = ReturnsOther()
print('new_foreign_skips_init', repr([z is sentinel, events]))
