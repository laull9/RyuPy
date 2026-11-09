def identity(value):
    return value
def generate(kind):
    try:
        yield identity('ready')
        raise ValueError('caught')
    except kind:
        yield 'handled'
it = generate(ValueError)
print(next(it))
print(next(it))
