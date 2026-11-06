original = __builtins__
first = {'token': 10}
second = {'token': 20}
third = {'token': 30}
__builtins__ = first

def old():
    return token

def outer():
    def inner():
        return token
    return inner

__builtins__ = second

def fresh():
    return token

def change_defaults():
    global __builtins__
    __builtins__ = second
    return 7

def decorate(f):
    global __builtins__
    __builtins__ = third
    return f

__builtins__ = first

@decorate
def ordered(a=change_defaults()):
    return token, a

nested = outer()
__builtins__ = original
first['token'] = 11

def local_scope(__builtins__):
    return __builtins__
