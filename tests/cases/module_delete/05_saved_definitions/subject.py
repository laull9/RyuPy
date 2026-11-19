# 删除函数和类的名字，不销毁其旧对象，也不允许编译时直调或基类捷径绕过。
events = []

def work(value: int) -> int:
    events.append('work')
    return value + 1

class Base:
    marker = 'old'

function_alias = work
class_alias = Base

def invoke(value: int) -> int:
    return work(value)

def invoke_zero():
    return work(0)

def derive():
    class Child(Base):
        events.append('class body')
    return Child

def remove():
    global work, Base
    del work, Base

# 调用目标先于实参求值，名字已删除时参数副作用也不能发生。
def argument():
    events.append('argument')
    return 5

def invoke_effect():
    return work(argument())
