# 独立pending：CP合法的宽/多维buffer、继承index、bool返回警告与flags长尾不混入主验收。
import re
import warnings

for view in (memoryview(b'a,b,').cast('H'), memoryview(b'a,b,').cast('B', shape=[2, 2])):
    assert re.split(b',', view) == [b'a', b'b', b'']
    print('wide-buffer', re.split(b',', view))
class Base:
    def __index__(self):
        return 1
class Derived(Base):
    pass
assert re.split(',', 'a,b,c', Derived()) == ['a', 'b,c']
print('inherited-index', re.split(',', 'a,b,c', Derived()))
class BooleanIndex:
    def __index__(self):
        return True
with warnings.catch_warnings(record=True) as caught:
    warnings.simplefilter('always')
    assert re.split(',', 'a,b,c', BooleanIndex()) == ['a', 'b,c']
assert len(caught) == 1
assert type(caught[0].message).__name__ == 'DeprecationWarning'
print('bool-warning', str(caught[0].message))
for flags in (None, Base()):
    try:
        re.split(',', 'a,b', flags=flags)
    except TypeError as error:
        print('flags-long-tail', type(error).__name__, str(error))
    else:
        assert False

# 既有Compile输入域的旧边界：从v1主应用拆出，CP期望不变。
for label, pattern in [('pattern-int', 1), ('pattern-memoryview', memoryview(b','))]:
    try:
        re.split(pattern, 'a,b' if label == 'pattern-int' else b'a,b')
    except TypeError as error:
        print(label, type(error).__name__, str(error), [])
    else:
        assert False
