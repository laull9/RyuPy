import subject
box = subject.Box()
assert box.read() == 80
assert subject.Box.static_read() == 10
assert subject.Box.class_read() == (70, 10)
assert box.current == 10
subject.value = 20
assert box.read(2) == 22
assert subject.Box.static_read() == 20
assert subject.Box.class_read() == (70, 20)
assert box.current == 20
assert subject.Box.seen == [10, 20]
assert subject.Box.read.__globals__ is subject.__dict__
assert subject.Box.__module__ == 'subject'
print('class namespace and method globals are distinct')
