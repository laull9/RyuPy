# ryupy: pending
from pathlib import Path

def source_file(x):
    return __file__

assert source_file(None) == __file__
print(Path(__file__).with_name('harvest.txt').read_text(encoding='utf-8'), end='')
saved = __file__
marker = []
__file__ = marker
assert source_file(None) is marker
__file__ = saved
assert source_file(None) == saved
print('orchard-resource-ok')
