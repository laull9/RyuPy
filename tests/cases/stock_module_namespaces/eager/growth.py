# 急切推导式使用当前frame环境；普通def仍在定义点捕获真实builtins。
import builtins
import sys

events = []
module = sys.modules[__name__]
item = 'module-item'
factor = 3

def source():
    events.append('source')
    return [1, 2, 3]

def keep(value):
    events.append(('filter', value))
    return value != 2

def tagged(label, value):
    events.append(label)
    return value

listed = [(item, factor) for item in source() if keep(item)]
keys = {tagged(('key', item), item): tagged(('value', item), item * factor)
        for item in [1, 2]}
selected = {item + outer for outer in [10, 20] for item in [1, 2] if outer != 20}

def before(value):
    return len(value)

original = builtins.__dict__
replacement = original.copy()

def alternate_len(value):
    return 100 + before(value)

replacement['len'] = alternate_len
__builtins__ = replacement
frame_sizes = [len(value) for value in ['a', 'bb']]

def after(value):
    return len(value)

def under_function(values):
    return [len(value) for value in values]

# 删除globals键也不能改变当前frame捕获的builtins。
del __builtins__
after_delete = {len(value) for value in ['a', 'bb']}
__builtins__ = original

def current(values):
    return [(value, factor) for value in values]

failure = RuntimeError('source-failure')

def broken():
    events.append('broken-source')
    raise failure

try:
    discarded = [tagged('unreachable', value) for value in broken()]
except RuntimeError as caught:
    failed_same = caught is failure

same_globals = before.__globals__ is module.__dict__
origin = (before.__module__, after.__module__)
