def outer(value='ready'):
    def inner():
        return value
    return inner()
print(outer())
print(__name__)
def generate(kind):
    try:
        yield 'ready'
        raise ValueError('caught')
    except kind:
        yield 'handled'
it = generate(ValueError)
print(next(it))
print(next(it))
