# 捕获案例在模块平铺，末尾保留模块handler到用户callback的完整异常链。
class Boundary:
    def __init__(self, phase, failure):
        self.phase = phase
        self.failure = failure
        self.iterations = 0
        self.index = 0
    def __iter__(self):
        self.iterations += 1
        if self.phase == 'first' or (self.phase == 'second' and self.iterations == 2):
            raise self.failure
        return self
    def __len__(self):
        if self.phase == 'len':
            raise self.failure
        raise TypeError('no length')
    def __length_hint__(self):
        if self.phase == 'hint':
            raise self.failure
        return 0
    def __next__(self):
        self.index += 1
        if self.phase == 'prefix' and self.index == 1:
            raise self.failure
        if self.phase == 'tail' and self.index == 2:
            raise self.failure
        if self.index > 3:
            raise StopIteration('end')
        return self.index

for phase in ['first', 'second', 'len', 'hint', 'prefix', 'tail']:
    failure = RuntimeError(phase)
    try:
        first, *middle, last = Boundary(phase, failure)
    except RuntimeError as error:
        print('identity', phase, error is failure, str(error))

try:
    raise ValueError('ambient')
except ValueError as ambient:
    first, *middle, last = Boundary('ok', None)
    print('success in handler', first, middle, last)
    for phase in ['prefix', 'tail']:
        try:
            first, *middle, last = Boundary(phase, StopIteration('consumed'))
        except ValueError as short:
            print('short context', phase, str(short), short.__context__ is ambient)
    try:
        raise
    except ValueError as restored:
        print('ambient restored', restored is ambient)

failure = RuntimeError('tail failed')
def callback():
    raise failure

class Bad:
    def __init__(self):
        self.index = 0
    def __iter__(self):
        return self
    def __next__(self):
        self.index += 1
        if self.index == 1:
            return 10
        return callback()

def unpack():
    first, *middle, last = Bad()

try:
    raise ValueError('outer')
except ValueError:
    unpack()
