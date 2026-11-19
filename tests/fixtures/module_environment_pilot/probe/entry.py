# 同一原源分别观察默认旧路径与现有实例路径；失败不会被改写为golden。
import subject
print('initial', subject.read())
subject.value = 'changed'
assert subject.read() == 'changed'
print('attribute type rebound', subject.read())
saved = subject.read
alias = saved.__globals__
assert type(alias) is dict and alias is subject.__dict__
saved.__module__ = 'unrelated'
marker = []
alias['value'] = marker
assert saved() is marker
marker.append('in-place')
assert saved() == ['in-place']
alias['value'] = marker
assert saved() is marker
alias.update({'value': ['updated']})
assert saved() == ['updated']
assert alias.setdefault('value', 100) is saved()
removed = alias.pop('value')
assert removed == ['updated']
try:
    saved()
except NameError:
    print('dict mutations observed')
else:
    assert False
alias.setdefault('value', None)
assert saved() is None
subject.remove()
alias['value'] = 31
generator = subject.stream()
assert next(generator) == 31
alias['value'] = 32
assert next(generator) == 32
old_builtins = {'token': 7}
alias['__builtins__'] = old_builtins
old = subject.capture()
assert old.__globals__ is alias and old.__builtins__ is old_builtins
alias['__builtins__'] = {'token': 8}
new = subject.capture()
assert old() == 7 and new() == 8
old_builtins['token'] = 9
assert old() == 9 and new() == 8
print('owner and captured builtins', old(), new())
alias.clear()
try:
    saved()
except NameError:
    print('clear observed through escaped owner')
else:
    assert False
print('environment probe complete')
