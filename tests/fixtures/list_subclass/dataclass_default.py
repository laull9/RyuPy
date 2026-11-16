from dataclasses import dataclass
class L(list):
    pass
try:
    @dataclass
    class D:
        xs: list = L([1])
    print('accepted', D().xs)
except ValueError as error:
    print('ValueError',str(error))
