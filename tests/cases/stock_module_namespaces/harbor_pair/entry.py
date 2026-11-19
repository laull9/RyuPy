# 两个真实模块共享各自权威globals；跨模块调用、外部重绑、builtin回退和函数身份必须同次CP差分。
import math
import sys
import harbor_owner as owner
import ridge_dispatch as dispatch
assert sys.modules[owner.__name__] is owner
assert globals() is sys.modules[__name__].__dict__
saved = owner.measure
namespace = owner.__dict__
print(namespace is owner.expose(), dispatch.run([1, 2, 3]), saved is owner.measure)
namespace['len'] = lambda value: 71
print(dispatch.run([0]), saved([0]))
del namespace['len']
print(dispatch.run([1, 2]), saved([1]), namespace is owner.__dict__)
owner.offset = 13
print(dispatch.run([1, 2, 3]))
print(dispatch.catch(ValueError), dispatch.catch(KeyError))
