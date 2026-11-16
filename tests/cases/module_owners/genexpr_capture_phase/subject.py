original = __builtins__
events = []

def token1():
    return 'token1'

def token2():
    return 'token2'

def token3():
    return 'token3'

first = {'token': token1}
second = {'token': token2}
third = {'token': token3}

def emit(value):
    events.append(('element', value))
    return value

def rebind():
    global __builtins__
    __builtins__ = third

__builtins__ = first

def outer_iterable():
    events.append(('iterable-before-rebind', token()))
    rebind()
    events.append(('iterable-after-rebind', token()))
    return [0, 1]

def generator_case():
    return (emit(token()) for item in outer_iterable())

__builtins__ = second
result = generator_case()
