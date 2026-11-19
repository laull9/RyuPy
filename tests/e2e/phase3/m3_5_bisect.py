# bisect 的左右重复项、区间、key 和用户序列协议差分；不依赖第三方项目替身。
from bisect import bisect, bisect_left, bisect_right, insort, insort_left, insort_right

values = [1, 2, 2, 4]
print(bisect is bisect_right, insort is insort_right)
for x in [0, 1, 2, 3, 5]:
    print(bisect_left(values, x), bisect_right(values, x))
print(bisect_left(values, 2, 2, 3), bisect_right(values, 2, 1, 2))
print(bisect_left(values, 2, hi=-1), bisect_right(values, 2, hi=-2))
insort_left(values, 2)
insort_right(values, 3)
print(values)
records = [(1, 'a'), (2, 'old'), (3, 'c')]
events = []
def key(value):
    events.append(value[1])
    return value[0]
print(bisect_left(records, 2, key=key), events)
events.clear()
insort(records, (2, 'new'), key=key)
print(records, events)

class Index:
    def __index__(self):
        return 1

class Sequence:
    def __init__(self):
        self.values = [1, 3, 5]
    def __len__(self):
        print('len')
        return len(self.values)
    def __getitem__(self, index):
        print('get', index)
        return self.values[index]
    def insert(self, index, value):
        print('insert', index, value)
        self.values.insert(index, value)

seq = Sequence()
print(bisect_left(seq, 3, Index()))
insort(seq, 4)
print(seq.values)
for lo, hi in [(-1, None), (0, 10), (0.5, None)]:
    try:
        bisect_left([1], 2, lo, hi)
    except (ValueError, IndexError, TypeError) as error:
        print(type(error).__name__)

# 左右二分必须保持 < 的接收者方向，不能用 == 或交换比较替代。
class Item:
    def __init__(self, label, value):
        self.label = label
        self.value = value
    def __lt__(self, other):
        print('lt', self.label, other.label)
        return self.value < other.value
    def __eq__(self, other):
        raise RuntimeError('equality must not be used')

items = [Item('a', 1), Item('b', 2), Item('c', 2)]
needle = Item('x', 2)
print(bisect_left(items, needle))
print(bisect_right(items, needle))

# key 只在实际访问时调用；失败不能提前插入或复制序列。
unchanged = [1, 3, 5]
def fail_key(value):
    print('key', value)
    if value == 3:
        raise ValueError('key failed')
    return value
try:
    insort(unchanged, 4, key=fail_key)
except ValueError:
    print(unchanged)
