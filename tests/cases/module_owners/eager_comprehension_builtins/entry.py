import subject
assert subject.result[0] == [10]
assert subject.result[1] == {10}
assert subject.result[2] == {0: 10}
assert next(subject.result[3]) == 20
print('eager comprehensions inherit the frame; generator expression captures globals')
