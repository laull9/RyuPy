values = [[1, 2], (1, 2), {'a': 1}, {1, 2}, frozenset([1, 2]), b'ab', bytearray(b'ab'), '😀a']
for value in values:
    method = value.__len__
    print(method(), value.__len__(), method.__self__ is value, method.__name__)
    print(method == value.__len__, hash(method) == hash(value.__len__))
    print(type(method).__name__, method.__objclass__ is type(value))
    try:
        print(method.__module__)
    except AttributeError:
        print("no module")
    try:
        method(1)
    except TypeError:
        print('extra positional')
    try:
        method(unexpected=1)
    except TypeError:
        print('unexpected keyword')
items = []
length = items.__len__
print(length())
items.append(3)
print(length())
mapping = {}
for view in (mapping.keys(), mapping.values(), mapping.items()):
    bound = view.__len__
    print(bound())
    mapping['x'] = 1
    print(bound())
    mapping.clear()
print([].__len__ == [].__len__)
