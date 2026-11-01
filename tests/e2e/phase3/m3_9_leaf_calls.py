# 纯叶调用的守卫特化不能修改公开函数身份、实参求值顺序或通用调用语义。
def twice(value):
    return value + value


def add(left, right):
    return left + right

alias = twice
alias_add = add
original = alias


def invoke(value):
    return alias(value)


def invoke_add(left, right):
    return alias_add(left, right)

for value in [3, True, 0.5, 1 << 62, -(1 << 63), 1 << 80, 'ab', [1]]:
    result = invoke(value)
    print(result, type(result).__name__)
print(alias is original, alias is twice)

class Left:
    def __add__(self, other):
        print('left-add')
        return NotImplemented

class Right:
    def __radd__(self, other):
        print('right-radd')
        return 'reflected'

print(invoke_add(Left(), Right()))
items = [1]
combined = invoke_add(items, [2])
print(items, combined, combined is items)


def other(value):
    return value + 100


def rebind_argument():
    global alias
    print('argument')
    alias = other
    return '12345'


def invoke_rebinding():
    # callee在实参之前读取；参数回调重绑定不能改变本次已捕获函数。
    return alias(len(rebind_argument()))

print(invoke_rebinding())
print(invoke(5))
alias = original
print(invoke(5), alias is twice)
print(alias(value=4))
try:
    alias()
except TypeError:
    print('missing-argument')
try:
    alias(1, 2)
except TypeError:
    print('extra-argument')

class Raises:
    def __add__(self, other):
        print('original-add')
        raise ValueError('original-error')

try:
    invoke(Raises())
except ValueError as error:
    print(str(error))

# 已证明的 I64 装箱实参才进入默认候选；上面的泛动态参数仍走原通路。
def known_argument():
    return alias(7)


def known_overflow():
    return alias(9223372036854775807)


def known_pair():
    return alias_add(12, 30)

print(known_argument(), known_pair())
print(known_overflow())
alias = other
print(known_argument())
alias = original
print(known_argument())
