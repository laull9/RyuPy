# 临时tuple标量替换负载：交换/固定索引与必须保留真实tuple的逃逸对照并列。
# 输入对象和逃逸容器在计时外建立；返回对象/元素身份与每轮逃逸对象独立性均校验。
import sys
import time


def tuple_swap(n, left, right):
    for index in range(n):
        left, right = right, left
    return left, right


def tuple_read(n, left, right):
    result = left
    for index in range(n):
        result = (left, right)[0]
    return result


def tuple_escape(n, left, right, saved):
    for index in range(n):
        saved.append((left, right))
    return saved[-1]


validate = '--validate' in sys.argv
count = 21 if validate else 20001
left = [1]
right = [2]
saved = []


def checked(label, result):
    if label == 'swap':
        assert type(result).__name__ == 'tuple'
        assert len(result) == 2
        assert result[0] is right
        assert result[1] is left
    elif label == 'read':
        assert result is left
    else:
        assert len(saved) == count
        assert result is saved[-1]
        assert type(result).__name__ == 'tuple'
        assert result[0] is left
        assert result[1] is right
        assert saved[0] is not saved[-1]
        assert saved[0][0] is left
        assert saved[0][1] is right
    return True


# 同时锁空/偶数轮及返回tuple不可错误缓存；这些在正式计时外。
zero = tuple_swap(0, left, right)
even = tuple_swap(2, left, right)
assert zero[0] is left and zero[1] is right
assert even[0] is left and even[1] is right
assert zero is not even
assert tuple_read(0, left, right) is left

for label, function in [('swap', tuple_swap), ('read', tuple_read), ('escape', tuple_escape)]:
    saved.clear()
    result = function(count, left, right, saved) if label == 'escape' else function(count, left, right)
    print('semantics', label, checked(label, result))
    if not validate:
        for warm in range(3):
            saved.clear()
            result = function(count, left, right, saved) if label == 'escape' else function(count, left, right)
            checked(label, result)
        for repeat in range(7):
            saved.clear()
            start = time.perf_counter()
            result = function(count, left, right, saved) if label == 'escape' else function(count, left, right)
            elapsed = time.perf_counter() - start
            print('sample', label, elapsed, checked(label, result))
