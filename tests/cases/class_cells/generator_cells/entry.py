import subject
assert subject.first is subject.saved
assert subject.first_expr is subject.saved
assert next(subject.it)() is subject.saved
assert next(subject.expr) is subject.saved
print('generator and genexpr retain class cell across suspension')
