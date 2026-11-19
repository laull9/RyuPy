import helpers
try:
    import subject
except ValueError as exc:
    assert str(exc) == 'class body failed'
else:
    assert False
old = helpers.escaped[0]
try:
    old(None)
except NameError as exc:
    assert str(exc) == "cannot access free variable '__class__' where it is not associated with a value in enclosing scope"
else:
    assert False
helpers.fail = False
import subject
assert helpers.escaped[1](None) is subject.Subject
try:
    old(None)
except NameError:
    pass
else:
    assert False
print('failed class body leaves escaped cell empty across retry')
