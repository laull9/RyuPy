class Target:
    def __init__(self, original):
        self.original = original
    @property
    def error(self):
        try:
            raise self.original
        except ValueError:
            pass
        return TypeError

def generate(target, original):
    try:
        yield 'ready'
        raise original
    except target.error:
        yield 'wrong'
original = ValueError('original')
it = generate(Target(original), original)
print(next(it))
next(it)
