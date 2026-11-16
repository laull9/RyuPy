
d = {'old': 0}
def pairs():
    print('yield-first')
    yield ('first', 1)
    print('before-raise', d)
    raise ValueError('source-failed')
try:
    d.update(pairs())
except ValueError as e:
    print('caught', str(e))
print('after', d)
