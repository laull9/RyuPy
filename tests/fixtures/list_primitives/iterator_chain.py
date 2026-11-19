
# 精确list迭代器在未耗尽时活读，耗尽后不因append复活；末尾保完整六帧cause/context链。

values = [10, 20]
cursor = iter(values)
assert next(cursor) == 10
values.append(30)
assert next(cursor) == 20
values[2] = 31
assert next(cursor) == 31
values.append(40)
assert next(cursor) == 40
assert next(cursor, 'end') == 'end'
values.append(50)
assert next(cursor, 'still-end') == 'still-end'
assert next(cursor, 'again-end') == 'again-end'
print('list-primitives.iterator-append-and-exhaust', values)

values = [1, 2, 3, 4]
cursor = iter(values)
assert next(cursor) == 1
del values[0]
assert next(cursor) == 3
values[2] = 40
assert next(cursor) == 40
values.clear()
assert next(cursor, 'end') == 'end'
values.extend([7, 8, 9, 10])
assert next(cursor, 'still-end') == 'still-end'
print('list-primitives.iterator-live-delete', values)

values = []
cursor = iter(values)
values.append(1)
assert next(cursor) == 1
assert next(cursor, 'end') == 'end'
values.append(2)
assert next(cursor, 'still-end') == 'still-end'
empty = []
empty_cursor = iter(empty)
assert next(empty_cursor, 'empty') == 'empty'
empty.append(3)
assert next(empty_cursor, 'still-empty') == 'still-empty'
print('list-primitives.iterator-initially-empty', values, empty)

class FailingCursor:
    def __init__(self, target):
        self.target = target
        self.position = 0

    def __iter__(self):
        return self

    def __next__(self):
        if self.position == 0:
            self.position = 1
            return 20
        assert self.target == [10, 20]
        print('list-primitives.chain-prefix', self.target)
        raise TypeError('list-primitives-next')

values = [10]
def inner():
    return values.extend(FailingCursor(values))
def outer():
    return inner()

try:
    raise ValueError('list-primitives-cause')
except ValueError as cause:
    saved_cause = cause
try:
    raise RuntimeError('list-primitives-ambient') from saved_cause
except RuntimeError:
    print('list-primitives.chain')
    outer()
