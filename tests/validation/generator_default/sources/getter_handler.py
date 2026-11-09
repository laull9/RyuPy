class Target:
    @property
    def error(self):
        try:
            raise TypeError('inside getter')
        except TypeError:
            return ValueError

def generate(target):
    try:
        yield 'ready'
        raise ValueError('caught')
    except target.error:
        yield 'handled'
it = generate(Target())
print(next(it))
print(next(it))
