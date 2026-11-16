class Target:
    @property
    def kind(self):
        return TypeError


def generate(target):
    try:
        yield "ready"
        raise TypeError("original")
    except target.kind:
        yield "caught"


target = Target()
first = generate(target)
print(next(first))
print(next(first))
second = generate(target)
print(next(second))
print(next(second))
