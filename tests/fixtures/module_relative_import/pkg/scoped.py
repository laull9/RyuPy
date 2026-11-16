
def typed() -> int:
    from .plain import value
    return value['x']

def outer():
    def inner():
        from .plain import value
        return value['x']
    return inner

class Reader:
    def read(self):
        from .plain import value
        return value['x']

def stream():
    from .plain import value
    yield value['x']
    from .plain import value
    yield value['x']
