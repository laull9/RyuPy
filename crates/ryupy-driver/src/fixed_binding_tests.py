import declared_demo
import declared_demo as again
from declared_demo import add as saved

assert declared_demo is again
assert saved is declared_demo.add
try:
    declared_demo.__file__
except AttributeError:
    print('file-absent')
marker = []
declared_demo.__file__ = marker
assert declared_demo.__file__ is marker

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

# 本A测试故意不存在C库：有效参数须真正走PInvoke并窄映射ImportError。
# 不将它当成功原生调用；成功值由B三平台真实shared/static链验收。
try:
    saved(0, 0)
except ImportError:
    print('missing-library')

def replacement(left, right):
    return 99

def rebind():
    declared_demo.add = replacement
    return 0

try:
    declared_demo.add(rebind(), 0)
except ImportError:
    print('callee-frozen')
assert declared_demo.add(1, 2) == 99
assert saved is not declared_demo.add
try:
    saved(False, 0)
except TypeError:
    print('saved-identity')
declared_demo.add = 7
try:
    declared_demo.add(1, 2)
except TypeError:
    print('noncallable')
print('binding-boundary-ok')
