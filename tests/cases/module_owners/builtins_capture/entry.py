import subject
assert subject.old() == 11
assert subject.fresh() == 20
assert subject.nested() == 30
assert subject.ordered() == (20, 7)
assert subject.local_scope(99) == 99
assert subject.old.__builtins__ is subject.first
assert subject.fresh.__builtins__ is subject.second
assert subject.nested.__builtins__ is subject.third
assert subject.ordered.__builtins__ is subject.second
assert subject.old.__globals__ is subject.__dict__
print('definition captures current globals builtins in source order')
