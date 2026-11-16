# 字符串局部累加的差分门：身份、别名、动态回退，以及必须拒绝缓冲的观察路径。
def accumulate(initial, values):
    total = initial
    for i in range(len(values)):
        total += values[i]
    return total, total

seed = ''.join(['seed', '-value'])
piece = ''.join(['piece', '-value'])
for initial, values in [(seed, []), (seed, ['', '']), ('', [piece]), ('', ['', piece, ''])]:
    pair = accumulate(initial, values)
    expected = initial if initial else piece
    print(pair[0], pair[0] is expected, pair[0] is pair[1])
pair = accumulate(seed, ['a', '', 'b', ''])
print(pair[0], seed, pair[0] is seed, pair[0] is pair[1])
# 不输出裸代理码元，比较拼接内容；不能正规化或丢弃原始字符串内容。
for initial, values, expected in [
    ('汉', ['字', '🙂', 'e\u0301'], '汉字🙂e\u0301'),
    ('x', ['\ud800', 'y', '\udfff'], 'x\ud800y\udfff'),
]:
    pair = accumulate(initial, values)
    print(pair[0] == expected, pair[0] is pair[1])

class Shared:
    def __init__(self):
        self.value = ''
    def __iadd__(self, other):
        print('iadd', other)
        self.value += other
        return self

shared = Shared()
class Reverse:
    def __radd__(self, other):
        print('radd', other)
        shared.value = other
        return shared

pair = accumulate('start', ['a', 'b', Reverse(), 'tail'])
print(pair[0] is shared, pair[0] is pair[1], shared.value)

# 普通str子类必须走原动态协议，不能抹掉初始对象身份。
class Text(str):
    pass

plain = Text('seed')
print(accumulate(plain, [])[0] is plain)
print(accumulate(plain, ['suffix'])[0], accumulate('prefix', [Text('rhs')])[0])

class Raises:
    def __radd__(self, other):
        print('raising', other)
        raise ValueError('fallback-error')

for values in [['a', None, 'unreached'], ['a', Raises(), 'unreached']]:
    try:
        accumulate('seed', values)
    except TypeError:
        print('type-error')
    except ValueError as error:
        print(str(error))

# 循环内旧值别名、rhs读取累加槽、locals观察都不能当成无观察累加。
def aliases(initial):
    total = initial
    seen = []
    for i in range(3):
        seen.append(total)
        total += 'a'
    return total, seen

print(aliases('s'))

def self_append(initial):
    total = initial
    for i in range(3):
        total += total
    return total

print(self_append('ab'))

def observe_locals(initial):
    total = initial
    for i in range(3):
        print('locals', locals()['total'])
        total += 'a'
    return total

print(observe_locals('s'))

# cell、异常处理、生成器和多出口仍沿用完整动态路径。
def closure(initial):
    total = initial
    def read():
        return total
    for i in range(3):
        print('cell', read())
        total += 'a'
    return read()

print(closure('s'))

def protected(initial, values):
    total = initial
    try:
        for i in range(len(values)):
            total += values[i]
    except TypeError:
        return total
    return total

print(protected('s', ['a', 'b', None]))

def generated(initial):
    total = initial
    for i in range(3):
        yield total
        total += 'a'

print(list(generated('s')))

def early_exit(initial, count):
    total = initial
    for i in range(count):
        if i == 2:
            return total
        total += 'a'
    return total

print(early_exit('s', 0), early_exit('s', 5))
