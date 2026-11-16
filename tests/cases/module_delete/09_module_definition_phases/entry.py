# ryupy: pending
# 独立窄例：函数和所有 class 语句均处于模块作用域，不依赖函数内 class 支持。
events = []

def work(value):
    return value + 1

def argument():
    events.append('argument')
    return 5

class Base:
    marker = 'old'

saved_function = work
saved_class = Base
old_instance = Base()
assert saved_function(2) == 3 and old_instance.marker == 'old'

class Before(Base):
    pass

assert Before().marker == 'old'
del work, Base
try:
    work(argument())
except NameError as error:
    assert str(error) == "name 'work' is not defined"
    print('module callee missing', type(error).__name__, str(error))
else:
    assert False
assert events == []
try:
    Base(argument())
except NameError as error:
    assert str(error) == "name 'Base' is not defined"
    print('module constructor missing', type(error).__name__, str(error))
else:
    assert False
assert events == []
try:
    class Failed(Base):
        events.append('failed class body')
except NameError as error:
    assert str(error) == "name 'Base' is not defined"
    print('module base missing', type(error).__name__, str(error))
else:
    assert False
assert events == []
assert saved_function(4) == 5
assert saved_class().marker == 'old' and type(old_instance) is saved_class

# 重绑原名字后直接调用和新的基类求值都必须读取新值，旧alias保持原身份。
def replacement(value):
    return value + 100

class Replacement:
    marker = 'new'

work = replacement
Base = Replacement
assert work(argument()) == 105 and events == ['argument']
assert Base().marker == 'new'

class After(Base):
    events.append('new class body')

assert After().marker == 'new'
assert events == ['argument', 'new class body']
assert saved_function is not work and saved_function(4) == 5
assert saved_class is not Base and old_instance.marker == 'old'
assert saved_class().marker == 'old' and Before().marker == 'old'
print('module saved aliases and rebinding', True)
print('module definition phases', events)
