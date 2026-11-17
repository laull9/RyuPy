# ryupy: pending
import os.path

def anchor(x):
    return __file__

assert anchor(None) == __file__
path = os.path.join(os.path.dirname(__file__), 'samples', 'tide.txt')
with open(path, encoding='utf-8') as stream:
    print(stream.read(), end='')
original = __file__
replacement = []
__file__ = replacement
assert anchor(None) is replacement
__file__ = original
assert anchor(None) == original
print('estuary-resource-ok')
