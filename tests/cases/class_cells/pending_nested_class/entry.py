import subject
assert subject.first is not subject.second
assert subject.first().read() is subject.first
assert subject.second().read() is subject.second
print('pending function-local class cell')
