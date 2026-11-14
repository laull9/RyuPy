class Target:
    pass
def identity(value):
    return value
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
identity(Target())
def generate(kind):
    try:
        yield 'ready'
        raise ValueError('caught')
    except kind:
        yield 'handled'
it = generate(ValueError)
print(next(it))
print(next(it))
