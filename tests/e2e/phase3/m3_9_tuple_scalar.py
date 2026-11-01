# 新鲜临时tuple的标量替换不能改变元素求值、交换、引用身份与逃逸时的真实对象。
def swap(left, right):
    left, right = right, left
    return left, right

left = [1]
right = [2]
result = swap(left, right)
print(result[0] is right, result[1] is left)


def first(left, right):
    return (left, right)[0]


def last(left, right):
    return (left, right)[-1]

print(first(left, right) is left, last(left, right) is right)

# 原构造点必须冻结值；不能在后续读取时重读已改写的源local。
def freeze(left, right):
    values = (left, right)
    left = right
    return values[0], left

frozen = freeze(left, right)
print(frozen[0] is left, frozen[1] is right)


def mark(label, value):
    print(label)
    return value


def ordered():
    return (mark('first', left), mark('second', right))[0]

print(ordered() is left)

# 逃逸和身份观察仍需真实tuple；不能因为其后只有固定索引就忽略此前观察。
saved = []
def observe(value):
    saved.append(value)
    print(type(value).__name__, len(value))


def escape(left, right):
    pair = (left, right)
    observe(pair)
    return pair, pair[0]

escaped = escape(left, right)
print(escaped[0] is saved[0], escaped[1] is left)


def identity(left, right):
    pair = (left, right)
    alias = pair
    return pair is alias, id(pair) == id(alias)

print(identity(left, right))


def suspended(left, right):
    pair = (left, right)
    yield pair
    yield pair

stream = suspended(left, right)
print(next(stream) is next(stream))

# 越界与解包失败先求全部元素，不提前写目标；保留原异常而非静态拒绝。
def invalid(left, right):
    return (mark('bad-first', left), mark('bad-second', right))[9]

try:
    invalid(left, right)
except IndexError:
    print('index-error')

one = 'old-one'
two = 'old-two'
try:
    one, two = (mark('short', left),)
except ValueError:
    print(one, two)
