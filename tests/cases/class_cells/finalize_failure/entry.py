import helpers
try:
    import subject
except TypeError:
    pass
else:
    assert False
try:
    helpers.escaped[0](None)
except NameError as exc:
    assert str(exc) == "cannot access free variable '__class__' where it is not associated with a value in enclosing scope"
else:
    assert False
print('failed ordinary finalization leaves escaped class cell empty')
