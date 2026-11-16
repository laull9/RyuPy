import subject
value = subject.Generic().read()
assert value is not subject.Generic
assert value.__name__ == '__class__'
print('pending PEP695 annotation scope shadows implicit class cell')
