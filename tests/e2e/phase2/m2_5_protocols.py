# M2.5：__repr__ / __str__ / __eq__ / __hash__ / __bool__ / __len__ /
# __iter__ / __next__ / __getitem__ / 常用运算槽与包含判定。

class Vec:
    def __init__(self, x):
        self.x = x

    def __repr__(self):
        return f"Vec({self.x})"

    def __str__(self):
        return f"<{self.x}>"

    def __eq__(self, other):
        return self.x == other.x

    def __hash__(self):
        return hash(self.x)

    def __bool__(self):
        return self.x != 0

    def __len__(self):
        return abs(self.x)

    def __add__(self, other):
        return Vec(self.x + other.x)

    def __getitem__(self, index):
        return self.x + index

    def __contains__(self, value):
        return value == self.x


class Countdown:
    def __init__(self, n):
        self.n = n

    def __iter__(self):
        return self

    def __next__(self):
        if self.n <= 0:
            raise StopIteration
        self.n -= 1
        return self.n


zero = Vec(0)
two = Vec(2)
three = Vec(3)
print(repr(two), str(two))
print(two == Vec(2), two != three, hash(two) == hash(2))
print(bool(zero), bool(two), len(three))
print(two + three)
print(two[5], 2 in two, 3 in two)
print(list(Countdown(3)))
print(two in [Vec(2)])
