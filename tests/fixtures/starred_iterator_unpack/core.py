# 五组有限来源：内置、组合器/生成器、用户双iter与提示、getitem、公共语法入口。
events = []

def unpack(source):
    first, *middle, last = source
    return [first, middle, last]

def result(label, source, expected=None):
    events.clear()
    try:
        print(label, 'ok', unpack(source))
    except Exception as error:
        print(label, type(error).__name__, str(error), error is expected)
    print('events', events)

class Text(str):
    pass

# 精确容器名字同样经过通用helper；Unicode元素按码点。
for source in [[1, 2, 3], (1, 2, 3), 'A😀B', Text('A😀B'), b'abc', bytearray(b'abc'),
               range(3), map(int, ['1', '2', '3']), zip([1, 2, 3], [4, 5, 6]),
               enumerate([7, 8, 9]), (x for x in [1, 2, 3]),
               {'a': 1, 'b': 2, 'c': 3}.keys()]:
    print('builtin', unpack(source))
leaf = []
source = [leaf]
*middle, = source
*other, = source
empty = []
*empty1, = empty
*empty2, = empty
print('fresh', type(middle) is list, middle is not source, middle is not other,
      middle[0] is leaf, type(empty1) is list, empty1 is not empty2)

class Cursor:
    def __init__(self, values, name, length, hint):
        self.values = values
        self.name = name
        self.index = 0
        self.iter_calls = 0
        self.length = length
        self.hint = hint
        self.replacement = None
    def __iter__(self):
        self.iter_calls += 1
        events.append([self.name, 'iter', self.iter_calls])
        if self.iter_calls > 1 and self.replacement is not None:
            return self.replacement
        return self
    def __next__(self):
        index = self.index
        self.index += 1
        events.append([self.name, 'next', index])
        if index >= len(self.values):
            raise StopIteration('done')
        value = self.values[index]
        if isinstance(value, BaseException):
            raise value
        return value
    def __len__(self):
        events.append([self.name, 'len', self.index])
        if isinstance(self.length, BaseException):
            raise self.length
        return self.length
    def __length_hint__(self):
        events.append([self.name, 'hint', self.index])
        if isinstance(self.hint, BaseException):
            raise self.hint
        return self.hint

result('double iter', Cursor([10, 20, 30, 40], 'I', 4, AssertionError('unused')))
wrapped = Cursor([10, 20, 30, 40], 'W', TypeError('no len'), 0)
events.clear()
source = iter(wrapped)
first, *middle, last = source
print('wrapped', first, middle, last, events)
original = Cursor([10, 20], 'I0', 2, AssertionError('unused'))
replacement = Cursor([70, 80, 90], 'I1', AssertionError('wrong hint owner'), 0)
original.replacement = replacement
result('replacement', original)
print('positions', original.index, replacement.index)

class Source:
    def __init__(self, value):
        self.value = value
    def __iter__(self):
        events.append('source iter')
        return self.value
    def __len__(self):
        raise AssertionError('source length is not iterator length')
    def __length_hint__(self):
        raise AssertionError('source hint is not iterator hint')
result('source', Source(Cursor([1, 2, 3], 'S', TypeError('no len'), 0)))

class Index:
    def __index__(self):
        events.append('index conversion')
        return 2

# len可用index，hint不行；只用确定在分配前失败的越界整数。
big = 18446744073709551616
for label, length, hint in [
    ('len index', Index(), 0), ('len bool', True, 0),
    ('len fallback', 'bad', 0), ('len negative', -1, 0),
    ('len big negative', -big, 0), ('len overflow', big, 0),
    ('hint index', TypeError('len'), Index()),
    ('hint bool', TypeError('len'), True),
    ('hint negative', TypeError('len'), -1),
    ('hint big negative', TypeError('len'), -big),
    ('hint overflow', TypeError('len'), big),
    ('hint bad', TypeError('len'), 'bad'),
    ('hint notimplemented', TypeError('len'), NotImplemented),
    ('hint ignored', TypeError('len'), TypeError('hint')),
]:
    result(label, Cursor([1, 2, 3], 'H', length, hint))

class HintProperty(Cursor):
    @property
    def __length_hint__(self):
        events.append('hint property')
        if self.hint == 'lookup':
            raise self.failure
        return self.hint_call
    def hint_call(self):
        events.append('hint call')
        if self.hint == 'call':
            raise self.failure
        if self.hint == 'bad':
            return 'bad'
        return 0
for mode in ['lookup', 'call', 'bad', 'valid']:
    item = HintProperty([1, 2, 3], 'P', TypeError('len'), mode)
    item.failure = TypeError('property ' + mode)
    result('property ' + mode, item, item.failure)

class LenProperty(Cursor):
    @property
    def __len__(self):
        events.append('len property')
        if self.length == 'lookup':
            raise self.failure
        return self.len_call
    def len_call(self):
        events.append('len call')
        if self.length == 'call':
            raise self.failure
        return 3
for mode in ['lookup', 'call', 'valid']:
    item = LenProperty([1, 2, 3], 'L', mode, 0)
    item.failure = TypeError('len property ' + mode)
    result('len property ' + mode, item)

for mode in ['lookup', 'call']:
    item = LenProperty([1, 2, 3], 'L', mode, 0)
    item.failure = ValueError('len property ' + mode)
    result('len property failure ' + mode, item, item.failure)

# 仅有__call__的类槽对象是合法callable，不是需要拒绝的自定义descriptor。
class HintCallable:
    def __call__(self):
        events.append('callable hint')
        return 0
class CallableSlot(Cursor):
    __length_hint__ = HintCallable()
result('callable slot', CallableSlot([1, 2, 3], 'callable', TypeError('len'), 0))

class MutatingHint(Cursor):
    def __length_hint__(self):
        events.append('hint mutates replacement')
        self.replacement.index = 1
        return 0
original = MutatingHint([10, 20], 'M0', TypeError('len'), 0)
original.replacement = Cursor([50, 60], 'M1', 2, 0)
result('mutating hint', original)

class ReentrantHint(Cursor):
    def __length_hint__(self):
        events.append('enter nested unpack')
        nested = Cursor([7, 8, 9], 'nested', 0, 0)
        self.nested = unpack(nested)
        events.append('leave nested unpack')
        return 0
item = ReentrantHint([1, 2, 3], 'outer', TypeError('len'), 0)
result('reentrant', item)
print('nested result', item.nested)

# 类级方法动态替换只影响随后推进，不缓存整个消费周期的next正文。
class Changing(Cursor):
    def __length_hint__(self):
        events.append('replace next')
        Changing.__next__ = changed_next
        return 0
def changed_next(self):
    events.append('new next')
    return Cursor.__next__(self)
result('change next', Changing([1, 2, 3], 'C', TypeError('len'), 0))

# 旧式回退只有整数getitem，源hint不等于隐藏sequence iterator的hint。
class Indexed:
    def __getitem__(self, key):
        events.append(['getitem', key, type(key).__name__])
        if key >= 4:
            raise IndexError('end')
        return key + 10
    def __length_hint__(self):
        raise AssertionError('source hint should not run')
class SizedIndexed(Indexed):
    def __len__(self):
        events.append('indexed len')
        return 4
result('indexed no length', Indexed())
result('indexed length', SizedIndexed())
class NextIndexed(Indexed):
    def __next__(self):
        events.append('next indexed prefix')
        return 70
result('second getitem fallback', Source(NextIndexed()))
class Disabled:
    __iter__ = None
    def __getitem__(self, index):
        raise AssertionError('disabled iter cannot fall back')
try:
    unpack(Disabled())
except TypeError as error:
    print('disabled', type(error) is TypeError)

def shadow():
    raise AssertionError('instance shadow')
item = Cursor([1, 2, 3], 'shadow', TypeError('len'), 0)
item.__iter__ = shadow
item.__length_hint__ = shadow
result('class slots', item)

# 本次耗尽不能永久关闭用户next适配器或map。
item = Cursor([1, StopIteration('temporary'), 3, 4], 'resume', 0, 0)
first, *rest = item
print('temporary stop', first, rest, next(item))
def callback(value):
    if value == 2:
        raise StopIteration('map stop')
    return value
item = map(callback, [1, 2, 3, 4])
first, *rest = item
print('map resume', first, rest, next(item))
for short_values in [[], [1], [1, 2]]:
    item = Cursor(short_values, 'short', 0, 0)
    events.clear()
    try:
        first, *middle, second, last = item
    except ValueError as error:
        print('short', str(error), events)

# 公共目标入口全部复用本层准备；每次使用独立的有限来源。
def values(start):
    yield start
    yield start + 1
    yield start + 2
for first, *rest in [values(1), values(4)]:
    print('for', first, rest)
print('list', [(first, rest) for first, *rest in [values(1), values(4)]])
print('set', sorted({first + len(rest) for first, *rest in [values(1), values(4)]}))
print('dict', {first: rest for first, *rest in [values(1), values(4)]})
print('generator', list((first, rest) for first, *rest in [values(1), values(4)]))
class Enter:
    def __enter__(self):
        return values(10)
    def __exit__(self, kind, value, trace):
        print('exit', kind is None)
        return False
with Enter() as (first, *rest):
    print('with', first, rest)
whole = first, *rest = values(20)
print('chain', first, rest, next(whole, 'done'))
