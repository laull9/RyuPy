from typing import Literal, get_args, get_origin

print(repr(Literal), get_origin(Literal), get_args(Literal))
print(repr(Literal[()]), get_args(Literal[()]))
a = Literal[None, True, 1, False, 0, 'red', 'red']
print(repr(a), get_args(a), get_origin(a) is Literal)
print(repr(Literal[Literal[1, 'red'], True, 1]))
b = Literal['red', 0, False, 1, True, None]
print(a == b, hash(a) == hash(b), Literal[True] == Literal[1])
print(Literal[False] == Literal[0], Literal[None] == Literal['None'])
print(Literal[1, 'red'] is Literal[1, 'red'])
for value in (Literal, Literal[1]):
    try:
        value()
    except TypeError:
        print('not callable')
    try:
        isinstance(1, value)
    except TypeError:
        print('not runtime type')
