import subject
method = subject.Subject.ident
for action in ['set', 'delete']:
    try:
        if action == 'set':
            setattr(method, '__closure__', None)
        else:
            delattr(method, '__closure__')
    except AttributeError as exc:
        assert str(exc) == 'readonly attribute'
    else:
        assert False
method.__dict__['__closure__'] = 'fake'
real = getattr(method, '__closure__')
assert real[0].cell_contents is subject.Subject
assert method(None) is subject.Subject
print('pending real closure reflection cannot be replaced with ordinary attributes')
