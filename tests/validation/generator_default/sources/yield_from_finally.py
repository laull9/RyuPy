def generate(kind):
    try:
        yield from [1, 2]
        raise ValueError('caught')
    except kind:
        yield 3
    finally:
        print('closed')
print(list(generate(ValueError)))
