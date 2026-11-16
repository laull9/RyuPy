
d = {'old': 0}
def pairs():
    print('yield-first')
    yield ('first', 1)
    print('yield-invalid', d)
    yield ('bad',)
    print('must-not-resume')
try:
    d.update(pairs())
except ValueError as e:
    print('caught', str(e))
print('after', d)
