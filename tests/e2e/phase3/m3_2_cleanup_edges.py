# 关闭、抑制、嵌套finally及委托错误的退出状态不能互相覆盖。
class Resource:
    def __init__(self, suppress):
        self.suppress = suppress

    def __enter__(self):
        print("enter")
        return self

    def __exit__(self, kind, value, traceback):
        print("exit", kind.__name__ if kind else None)
        return self.suppress


def suppressed():
    with Resource(True):
        yield 1
    yield 2


it = suppressed()
print(next(it))
try:
    it.close()
except RuntimeError:
    print("suppressed then yielded")
print(next(it, "end"))


class BadResource:
    def __enter__(self):
        return self

    def __exit__(self, kind, value, traceback):
        raise TypeError("exit failed")


def bad_resource():
    with BadResource():
        yield "value"


it = bad_resource()
print(next(it))
try:
    it.close()
except TypeError as error:
    print(str(error), type(error.__context__).__name__)
print(next(it, "closed"))


def yields_in_finally():
    try:
        yield 1
    finally:
        yield 2
        print("cleanup resumed")


it = yields_in_finally()
print(next(it))
try:
    it.close()
except RuntimeError:
    print("finally paused")
try:
    next(it)
except GeneratorExit:
    print("exit resumed")
print(next(it, "closed"))

# 参数错误不改变暂停状态，闭合throw保留传入异常身份。
def simple():
    yield 1
    yield 2


it = simple()
print(next(it))
try:
    it.throw(7)
except TypeError:
    print("bad throw")
print(next(it))
it.close()
error = ValueError("same")
try:
    it.throw(error)
except ValueError as caught:
    print(caught is error)

# GeneratorExit派生类构造与捕获不泄漏CLR构造缺失。
class MyExit(GeneratorExit):
    pass


def exit_type():
    try:
        yield 1
    except GeneratorExit as error:
        yield type(error).__name__


it = exit_type()
print(next(it), it.throw(MyExit("custom")))
it.close()

# 普通委托没有throw时向父生成器抛回同一异常。
def delegated():
    try:
        yield from [1, 2]
    except ValueError as error:
        yield str(error)


it = delegated()
print(next(it), it.throw(ValueError("parent catches")))
print(next(it, "done"))

# 清理异常覆盖原return，而不是恢复旧终值。
def replaces_return():
    try:
        yield 1
        return 9
    finally:
        raise TypeError("replacement")


it = replaces_return()
print(next(it))
try:
    next(it)
except TypeError as error:
    print(str(error))
