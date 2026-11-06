import subject
left, left_read = subject.make_pair(0)
right, right_read = subject.make_pair(100)
assert left(3) == (3, 10)
assert left_read() == 3
assert right_read() == 100
assert right(2) == (102, 10)
assert left_read() == 3
subject.value = 20
assert left() == (4, 20)
assert right_read() == 102
fn = subject.factory()
assert fn(7) == 27
assert left.__globals__ is subject.__dict__
assert right.__globals__ is subject.__dict__
assert fn.__globals__ is subject.__dict__
print('cells remain separate from module owner')
