import sys
import declared_demo
import declared_demo as alias
from declared_demo import add as saved


def fail_call(mode):
    if mode == 'type':
        saved(True, 0)
    else:
        saved(0, 0)


if len(sys.argv) > 1:
    fail_call(sys.argv[1])
assert alias is declared_demo
assert saved is declared_demo.add
try:
    declared_demo.__file__
except AttributeError:
    print('file-absent')

for left, right in ((0, 0), (-1, 4), (2147483647, 2147483647),
                    (-2147483648, -2147483648), (-2147483648, 2147483647)):
    print(saved(left, right))


def via_parameter(module):
    return module.add(1, 2)


assert via_parameter(alias) == 3


class IndexOnly:
    def __index__(self):
        print('FORBIDDEN INDEX')
        return 1


for value in (True, 1.5, '1', None, IndexOnly()):
    try:
        saved(value, 0)
    except TypeError:
        print('kind')
for value in (2147483648, -2147483649, 10 ** 100, -(10 ** 100)):
    try:
        saved(value, 0)
    except OverflowError:
        print('range')
try:
    saved()
except TypeError:
    print('arity0')
try:
    saved(1)
except TypeError:
    print('arity1')
try:
    saved(1, 2, 3)
except TypeError:
    print('arity3')
try:
    saved(left=1, right=2)
except TypeError:
    print('posonly')

seen = []


def observe(value):
    seen.append(value)
    return value


try:
    saved(observe(True), observe(0))
except TypeError:
    assert seen == [True, 0]
    print('evaluate-before-check')
try:
    saved(True, 2147483648)
except TypeError:
    print('left-before-right')


def replacement(left, right):
    return 99


def rebind():
    declared_demo.add = replacement
    return 0


assert declared_demo.add(rebind(), 0) == 0
assert declared_demo.add(1, 2) == 99
assert saved is not declared_demo.add
assert saved(2, 3) == 5
print('callee-frozen')
declared_demo.add = 7
try:
    declared_demo.add(1, 2)
except TypeError:
    print('noncallable')
print('binding-boundary-ok')
