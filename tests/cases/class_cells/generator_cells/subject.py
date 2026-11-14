class Original:
    def generator(self):
        yield __class__
        yield lambda: __class__
    def expression(self):
        return (__class__ for value in [1, 2])
saved = Original
obj = Original()
it = obj.generator()
expr = obj.expression()
first = next(it)
first_expr = next(expr)
class Other:
    pass
Original = Other
