import pkg.scoped
import other.plain
source = pkg.scoped
assert source.typed() == 7
inner = source.outer()
reader = source.Reader()
assert inner() == 7 and reader.read() == 7
stream = source.stream()
assert next(stream) == 7
source.__package__ = 'other'
source.__spec__ = None
assert source.typed() == 21
assert inner() == 21 and reader.read() == 21
assert next(stream) == 21
print('typed nested method generator environments preserved')
