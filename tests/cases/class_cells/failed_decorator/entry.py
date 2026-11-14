import helpers
try:
    import subject
except ValueError as exc:
    assert str(exc) == 'class decorator failed'
else:
    assert False
original = helpers.escaped[0]
assert original.ident(None) is original
print('failed class decorator preserves filled cell')
