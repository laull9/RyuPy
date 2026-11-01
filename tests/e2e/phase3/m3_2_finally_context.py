# 正常finally与覆盖return均须恢复原异常上下文，不清空外层except。
def ordinary():
    try:
        raise ValueError("outer")
    except ValueError:
        try:
            pass
        finally:
            pass
        raise


try:
    ordinary()
except ValueError as error:
    print(str(error))


def suspended():
    try:
        raise ValueError("generator")
    except ValueError:
        try:
            yield 1
        finally:
            yield 2
        raise


it = suspended()
print(next(it), next(it))
try:
    next(it)
except ValueError as error:
    print(str(error))

# 被finally return吞掉的异常不能留给调用者后续raise。
def swallowed():
    try:
        raise ValueError("discarded")
    finally:
        return 9


try:
    raise TypeError("caller")
except TypeError:
    print(swallowed())
    try:
        raise
    except TypeError as error:
        print(str(error))

# finally break抑制异常后，不给后续异常留下假的__context__。
for number in [1]:
    try:
        raise ValueError("suppressed")
    finally:
        break
try:
    raise TypeError("after break")
except TypeError as error:
    print("clean break", error.__context__ is None)


def break_context():
    for number in [1]:
        try:
            yield "before break"
            raise ValueError("suppressed in generator")
        finally:
            break
    try:
        raise TypeError("after generator break")
    except TypeError as error:
        yield error.__context__ is None


print(list(break_context()))

# except里的return也不能把已处理异常泄漏到调用者。
def except_return():
    try:
        raise ValueError("handled")
    except ValueError as error:
        return str(error)


print(except_return())
try:
    raise TypeError("new")
except TypeError as error:
    print("clean return", error.__context__ is None)

# 普通运行时抛错也需在Retry清调用帧之前挂上当前异常。
def failing_helper():
    return [][0]


def runtime_context():
    try:
        raise ValueError("original")
    except ValueError as original:
        try:
            failing_helper()
        except IndexError as error:
            yield error.__context__ is original
            raise


it = runtime_context()
print(next(it))
try:
    next(it)
except IndexError:
    print("runtime reraised")

# 无任何逻辑Region的生成器同样需要继承caller的活动异常。
def no_region():
    yield 1
    [][0]


it = no_region()
print(next(it))
try:
    raise ValueError("caller active")
except ValueError as original:
    try:
        next(it)
    except IndexError as error:
        print("caller context", error.__context__ is original)
