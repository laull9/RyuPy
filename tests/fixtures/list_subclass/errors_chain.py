
# list派生对象的参数绑定、错误优先级、清空边界和完整用户cause/context链。

class Plain(list):
    pass

def rejects(label, call, kind, message):
    try:
        call()
    except kind as error:
        assert str(error) == message
        print('list-subclass.error', label, kind.__name__, str(error))
    else:
        raise AssertionError(label)

value = Plain([10, 20])
rejects('constructor-many', lambda: Plain([], []), TypeError, 'list expected at most 1 argument, got 2')
rejects('constructor-keyword', lambda: Plain(iterable=[]), TypeError, 'list() takes no keyword arguments')
rejects('init-many', lambda: list.__init__(value, [], []), TypeError, 'list expected at most 1 argument, got 2')
rejects('init-keyword-first', lambda: list.__init__(value, [], [], wrong=1), TypeError, 'list() takes no keyword arguments')
assert value == [10, 20]
rejects('init-not-iterable', lambda: list.__init__(value, 1), TypeError, "'int' object is not iterable")
assert value == []
rejects('pop-empty', lambda: value.pop(), IndexError, 'pop from empty list')
rejects('pop-index-before-empty', lambda: value.pop(None), TypeError, "'NoneType' object cannot be interpreted as an integer")
value.extend([10, 20])
rejects('pop-many', lambda: value.pop(0, 1), TypeError, 'pop expected at most 1 argument, got 2')
rejects('pop-keyword', lambda: value.pop(index=0), TypeError, 'list.pop() takes no keyword arguments')
rejects('clear-many', lambda: value.clear(1), TypeError, 'list.clear() takes no arguments (1 given)')
rejects('copy-keyword', lambda: value.copy(wrong=1), TypeError, 'list.copy() takes no keyword arguments')
rejects('root-copy-receiver', lambda: list.copy(1), TypeError, "descriptor 'copy' for 'list' objects doesn't apply to a 'int' object")
rejects('root-len-receiver', lambda: list.__len__(1), TypeError, "descriptor '__len__' requires a 'list' object but received a 'int'")
rejects('root-init-receiver', lambda: list.__init__(1), TypeError, "descriptor '__init__' requires a 'list' object but received a 'int'")
rejects('root-new-non-type', lambda: list.__new__(1), TypeError, 'list.__new__(X): X is not a type object (int)')
rejects('root-new-wrong-type', lambda: list.__new__(int), TypeError, 'list.__new__(int): int is not a subtype of list')
rejects('index-type', lambda: value['wrong'], TypeError, 'list indices must be integers or slices, not str')
rejects('index-outside', lambda: value[20], IndexError, 'list index out of range')
rejects('unhashable', lambda: hash(value), TypeError, "unhashable type: 'Plain'")
assert value == [10, 20]

EVENTS = []
def mark(label, value):
    EVENTS.append(label)
    return value

rejects('evaluate-before-binding', lambda: value.pop(mark('first', 0), mark('second', 1), wrong=mark('keyword', 2)), TypeError, 'list.pop() takes no keyword arguments')
assert EVENTS == ['first', 'second', 'keyword'] and value == [10, 20]
EVENTS.clear()
rejects('evaluate-before-init-binding', lambda: list.__init__(mark('self', value), mark('source', []), wrong=mark('keyword', 1)), TypeError, 'list() takes no keyword arguments')
assert EVENTS == ['self', 'source', 'keyword'] and value == [10, 20]
print('list-subclass.error-evaluation', EVENTS, value)

def inner():
    return list.pop(Plain(), 'bad')
def middle():
    return inner()
def outer():
    return middle()

try:
    raise ValueError('list-subclass-cause')
except ValueError as cause:
    saved_cause = cause
try:
    raise RuntimeError('list-subclass-ambient') from saved_cause
except RuntimeError:
    print('list-subclass.chain')
    outer()
