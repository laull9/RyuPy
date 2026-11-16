import subject
assert subject.obj.read() is subject.Mutable
subject.obj.write('changed')
assert subject.obj.read() == 'changed'
subject.obj.delete()
try:
    subject.obj.read()
except NameError:
    pass
else:
    assert False
print('pending nonlocal class cell mutation and deletion')
