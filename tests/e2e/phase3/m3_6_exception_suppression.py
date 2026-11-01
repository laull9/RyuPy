# 共享异常修复：显式cause保留原异常身份；from None只隐藏报告，不删除Context。
original = ValueError("same")
cause = KeyError("cause")
try:
    raise original from cause
except ValueError as caught:
    print(caught is original, caught.__cause__ is cause, caught.__suppress_context__)

outer = KeyError("outer")
try:
    raise outer
except KeyError:
    try:
        raise original from None
    except ValueError as caught:
        print(caught is original, caught.__context__ is outer)
        print(caught.__cause__ is None, caught.__suppress_context__)
        caught.__suppress_context__ = False
        print(caught.__suppress_context__)
        try:
            caught.__suppress_context__ = 1
        except TypeError:
            print("bool required")

inner = TypeError("inner")
try:
    raise cause
except KeyError:
    try:
        raise inner from cause
    except TypeError as caught:
        print(caught is inner, caught.__context__ is cause, caught.__cause__ is cause)
