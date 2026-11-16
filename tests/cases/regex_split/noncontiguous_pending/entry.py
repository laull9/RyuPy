# 独立pending：既有非连续view构造未支持；保存v2拆出的CP错误合同。
import re

parent = memoryview(b'abcd')
view = parent[::2]
try:
    re.split(',', view, -1)
except TypeError as error:
    assert str(error) == "expected string or bytes-like object, got 'memoryview'"
    print('negative-noncontiguous', type(error).__name__, str(error), [])
else:
    assert False
view.release()
parent.release()
source = bytearray(b'a,b,c')
parent = memoryview(source)
view = parent[::2]
try:
    re.compile(b',').split(view)
except TypeError as error:
    assert str(error) == "expected string or bytes-like object, got 'memoryview'"
    print('noncontiguous', type(error).__name__, str(error))
else:
    assert False
view.release()
parent.release()
source.extend(b'!')
print('error-release', bytes(source))
