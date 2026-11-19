print(__spec__ is None)
def generate(kind):
    try:
        yield 'ready'
        raise ValueError('caught')
    except kind:
        yield 'handled'
it = generate(ValueError)
print(next(it))
print(next(it))
