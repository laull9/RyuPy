def generate(kind):
    try:
        raise ValueError('dead')
    except kind:
        raise TypeError('caught')
    yield 'dead'
next(generate(ValueError))
