original = __builtins__
first = {'token': 10}
second = {'token': 20}
__builtins__ = first

def read():
    return [token for n in [0]], {token for n in [0]}, {n: token for n in [0]}, (token for n in [0])

__builtins__ = second
result = read()
__builtins__ = original
