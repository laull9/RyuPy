def generate(kind):
    try:
        yield 'ready'
        raise ValueError('original')
    except kind:
        yield 'wrong'
it = generate(TypeError)
print(next(it))
next(it)
