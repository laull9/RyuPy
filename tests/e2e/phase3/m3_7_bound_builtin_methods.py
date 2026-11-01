# 清理/包装回调必须真正绑定接收者，不能只支持 obj.method(...) 的专用语法。
from functools import partial

values = []
append = values.append
print(append.__self__ is values, append.__name__, append.__qualname__, append.__module__)
append(1)
partial(append, 2)()
print(values, append == values.append, hash(append) == hash(values.append))
print(append == [].append, append is values.append)
upper = 'abc'.upper
print(upper(), upper.__self__)
get = {'a': 1}.get
print(get('a'), get('missing', 9))
clear = values.clear
try:
    clear(3)
except TypeError:
    print(values)
clear()
print(values)
try:
    missing = values.missing_method
except AttributeError:
    print('missing')
