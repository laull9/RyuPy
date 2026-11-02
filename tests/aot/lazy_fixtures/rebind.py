# 重复导入不能重建模块并丢掉用户公开重绑定。
import math

original = math.pi
try:
    math.pi = 'lazy-module-marker'
    import math as repeated
    print(math is repeated)
    print(repeated.pi)
    print(repeated.sqrt(9))
finally:
    math.pi = original
print(math.pi == original)
