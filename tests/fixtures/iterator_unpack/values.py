# 非星号解包的真实来源与公共语法入口；每次使用独立有限迭代器。
header = ['x', '5', '4', '12', '-1', '3']
height, baseLine, maxLength, oldLayout, commentLines = map(int, header[1:6])
print('header', height, baseLine, maxLength, oldLayout, commentLines)

class Pair:
    def __init__(self, first):
        self.value = first
        self.end = first + 2
    def __iter__(self):
        return self
    def __next__(self):
        if self.value == self.end:
            raise StopIteration
        result = self.value
        self.value += 1
        return result

def pair(first):
    yield first
    yield first + 1

a, b = Pair(10)
c, d = pair(20)
e, f = iter([30, 31])
g, h = (40, 41)
i, j = [50, 51]
print('values', a, b, c, d, e, f, g, h, i, j)
a, b = zip([1, 2], [3, 4])
c, d = enumerate([5, 6])
e, f = {'left': 1, 'right': 2}
g, h = 'a😀'
print('sources', a, b, c, d, e, f, g, h)
for a, b in [Pair(1), Pair(3)]:
    print('for', a, b)
print('list', [a + b for a, b in [Pair(1), Pair(3)]])
print('set', sorted({a + b for a, b in [Pair(1), Pair(3)]}))
print('dict', {a: b for a, b in [Pair(1), Pair(3)]})
print('generator', list(a + b for a, b in [Pair(1), Pair(3)]))

class Enter:
    def __enter__(self):
        return Pair(60)
    def __exit__(self, kind, value, trace):
        print('exit', kind is None)
        return False

with Enter() as (a, b):
    print('with', a, b)
whole = a, b = Pair(70)
print('chain', a, b, next(whole, 'done'))
first, *middle, last = [1, 2, 3, 4]
print('star unchanged', first, middle, last)

# 旧式序列回退仅传递整数下标，不咨询可能撒谎或抛错的长度。
keys = []
class Indexed:
    def __len__(self):
        raise AssertionError('fallback len called')
    def __getitem__(self, key):
        keys.append(key)
        if key == 2:
            raise IndexError('end')
        return key + 80

a, b = Indexed()
print('indexed', a, b, keys)
