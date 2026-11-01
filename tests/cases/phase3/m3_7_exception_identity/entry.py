# 依赖模块的同名用户异常不能覆盖入口的内建类型引用。
import left
import right
import io

try:
    raise ValueError('内建')
except ValueError as error:
    print('内建捕获', str(error))

try:
    raise io.UnsupportedOperation('双父')
except ValueError as error:
    print('内建额外父', str(error))

left.run()
right.run(left.raise_error)
try:
    left.raise_error()
except ValueError:
    print('错误：内建捕获用户同名类')
except Exception as error:
    print('入口拒绝用户类', str(error))

first = left.paused()
print(next(first))
print(first.throw(left.ValueError('左注入')))
second = left.paused()
print(next(second))
right.throw_error(second)

left.inherited()
