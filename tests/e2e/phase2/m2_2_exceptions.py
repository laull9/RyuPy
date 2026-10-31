# ryupy: pending
# M2.2：异常匹配、重抛、异常链与异常变量清理。
def fail():
    try:
        raise ValueError("source")
    except ValueError as error:
        raise RuntimeError("outer") from error


try:
    fail()
except RuntimeError as caught:
    print(type(caught).__name__, caught.args)
    print(type(caught.__cause__).__name__, str(caught.__cause__))
try:
    print(caught)
except NameError:
    print("exception target cleared")
try:
    try:
        1.0 / 0.0
    except ZeroDivisionError:
        print("reraise")
        raise
except ArithmeticError:
    print("caught base")
try:
    int("bad")
except (TypeError, ValueError):
    print("tuple match")
else:
    print("unexpected success")
