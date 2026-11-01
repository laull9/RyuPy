# 生成器逻辑异常区域需跨暂停保存；不得恢复时跳入CLR try中部。
def catch_and_finally():
    try:
        value = yield "ready"
        print("received", value)
        raise ValueError("inner")
    except ValueError as error:
        yield str(error)
    finally:
        print("cleanup")
    return 7


it = catch_and_finally()
print(next(it), it.send(4))
try:
    next(it)
except StopIteration as error:
    print(error.value)


def injected():
    try:
        yield 1
    except ValueError as error:
        yield str(error)
        raise
    finally:
        print("injected cleanup")


it = injected()
print(next(it), it.throw(ValueError("sent")))
try:
    next(it)
except ValueError as error:
    print("reraised", str(error))

# 暂停的except不能污染调用者；恢复时也不能丢自身正在处理的异常。
def context():
    try:
        raise ValueError("own")
    except ValueError:
        yield "paused"
        raise


it = context()
print(next(it))
try:
    raise TypeError("caller")
except TypeError:
    try:
        next(it)
    except ValueError as error:
        print("own", str(error))
    try:
        raise
    except TypeError as error:
        print("caller", str(error))


def closing():
    try:
        yield "close"
    finally:
        print("closed cleanup")


it = closing()
print(next(it), it.close(), next(it, "done"))

# 新建close/throw不执行body；非法close产出仍留在新暂停点。
def unopened():
    print("不应启动")
    yield 1


it = unopened()
print(it.close(), next(it, "unopened"))
it = unopened()
try:
    it.throw(ValueError("early"))
except ValueError:
    print("early throw")


def ignores_exit():
    try:
        yield 1
    except GeneratorExit:
        yield 2
        print("continued")


it = ignores_exit()
print(next(it))
try:
    it.close()
except RuntimeError:
    print("ignored exit")
print(next(it, "done"))
