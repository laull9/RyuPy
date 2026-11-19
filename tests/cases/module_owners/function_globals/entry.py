import subject

def replacement_len(seq):
    return 42

saved = subject.read
assert saved.__globals__ is subject.__dict__
assert saved(2) == 12
assert subject.bump(3) == 13
assert saved() == 14
saved.__module__ = 'other.owner'
saved.__name__ = 'renamed'
assert saved(0) == 13
old_builtins = subject.measure.__builtins__
new_builtins = {'len': replacement_len}
subject.__dict__['__builtins__'] = new_builtins
assert subject.measure([1, 2]) == 2
new = subject.make_measure()
assert new.__globals__ is subject.__dict__
assert new.__builtins__ is new_builtins
assert new([1, 2]) == 42
assert subject.measure.__builtins__ is old_builtins
subject.__dict__['value'] = 91
assert saved(1) == 92
print('real function globals and definition capture')
