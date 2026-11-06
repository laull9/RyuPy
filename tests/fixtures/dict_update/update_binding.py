
d = {'old': 0}
try:
    print('empty', d.update())
except TypeError as e:
    print('empty-error', str(e))
try:
    print('kwargs', d.update(first=1))
except TypeError as e:
    print('kwargs-error', str(e))
try:
    d.update([('x', 2)], [('y', 3)])
except TypeError as e:
    print('arity-error', str(e))
print('after', d)
print('constructor', dict([('a', 1)], b=2))
