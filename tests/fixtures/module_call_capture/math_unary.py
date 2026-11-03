# 受控原生一参函数保原对象与求值次序；不把用户同名函数当原生资格。
import math

assert math.sqrt(9) == 3.0
assert math.fabs(-2) == 2.0
assert math.exp(0) == 1.0
assert math.sin(0) == 0.0
assert math.cos(0) == 1.0
assert math.tan(0) == 0.0
assert math.isfinite(1) is True
assert math.isinf(math.inf) is True
assert math.isnan(math.nan) is True
assert math.floor(1.75) == 1
assert math.ceil(1.25) == 2
assert math.trunc(-1.75) == -1
assert math.log10(100) == 2.0
print('thirteen-unary-ok')

saved = math.sqrt
math.saved_unary = saved
assert math.saved_unary is saved
assert saved(25) == 5.0
assert math.saved_unary(36) == 6.0
log = []

# 名字与math.sqrt完全相同，签名和行为却是普通用户函数。
def sqrt(value, extra=100):
    log.append('user')
    return value + extra


def change():
    log.append('argument')
    math.sqrt = sqrt
    return 9


assert math.sqrt(change()) == 3.0
assert log == ['argument']
assert math.sqrt(9) == 109
assert math.sqrt(value=2, extra=3) == 5
assert log == ['argument', 'user', 'user']
assert math.saved_unary is saved
math.sqrt = saved
print('saved-and-rebound-ok')

class Reentrant:
    def __float__(self):
        log.append('float')
        assert math.saved_unary(16) == 4.0
        math.sqrt = sqrt
        return 9.0


log = []
assert math.sqrt(Reentrant()) == 3.0
assert log == ['float']
assert math.sqrt(1) == 101
assert log == ['float', 'user']
math.sqrt = saved
print('reentrant-conversion-once')

# 快路径不得改变原参数绑定；StarArg继续展开一次且完整走旧调用合同。
errors = []
try:
    math.sqrt()
except TypeError:
    errors.append('zero')
try:
    math.sqrt(1, 2)
except TypeError:
    errors.append('two')
try:
    math.sqrt(x=9)
except TypeError:
    errors.append('keyword')
assert math.sqrt(*[9]) == 3.0
try:
    math.sqrt(*[1, 2])
except TypeError:
    errors.append('star-two')
assert errors == ['zero', 'two', 'keyword', 'star-two']
print('binder-fallbacks-ok')
