# 普通模块见证：callee在实参前冻结，默认值保引用，global允许异类覆盖。
events = []
value = 10
seed = []
def original(argument):
    events.append('original')
    return argument
def replacement(argument):
    events.append('replacement')
    return argument
callee = original
def change():
    global callee
    events.append('argument')
    callee = replacement
    return 'payload'
def invoke():
    return callee(change())
def read():
    return value
def write(new_value):
    global value
    value = new_value

def decorate(function):
    events.append('decorate')
    return function
@decorate
def defaulted(argument=seed):
    return argument
