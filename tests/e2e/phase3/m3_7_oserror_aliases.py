# Python 3 的旧异常名称是同一类对象，不是派生包装；捕获与用户继承共用真实身份。
print(IOError is OSError, EnvironmentError is OSError)
print(IOError.__name__, EnvironmentError.__name__)
# 直接调用复用 OSError 的既有构造入口；一等类对象调用另保 pending。
error = IOError('message')
print(type(error) is OSError, error.args, isinstance(error, EnvironmentError))
try:
    raise EnvironmentError('caught')
except IOError as caught:
    print(type(caught) is OSError, str(caught))

class Child(IOError):
    pass

try:
    raise Child('child')
except OSError as error:
    print(type(error) is Child, isinstance(error, EnvironmentError))

# 生成器逻辑异常匹配也使用相同规范身份，不依赖显示名称。
def guarded():
    try:
        yield 1
    except IOError as error:
        yield str(error)

iterator = guarded()
print(next(iterator), iterator.throw(OSError('injected')))
iterator.close()

# 普通局部绑定仍优先于内建别名解析，不按拼写强制变成异常类。
def shadow(IOError, EnvironmentError):
    return IOError, EnvironmentError

print(shadow(7, 'local'))
