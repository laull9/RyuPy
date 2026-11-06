import subject
assert subject.classes[0] is not subject.classes[1]
assert subject.classes[1] is not subject.classes[2]
for index in range(3):
    assert subject.methods[index](None) is subject.classes[index]
print('each execution of class statement allocates a new shared cell')
