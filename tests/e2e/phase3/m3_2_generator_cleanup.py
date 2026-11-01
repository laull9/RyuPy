# M3.2：暂停期间保留 with / finally，关闭和意外 StopIteration 可观察。
class Resource:
    def __enter__(self):
        print("enter")
        return self

    def __exit__(self, kind, value, traceback):
        print("exit", kind.__name__ if kind else None)


def stream():
    with Resource():
        try:
            yield "value"
        finally:
            print("finally")


def broken():
    yield 1
    raise StopIteration("unexpected")


iterator = stream()
print(next(iterator))
iterator.close()
bad = broken()
print(next(bad))
try:
    next(bad)
except RuntimeError as error:
    print(type(error.__cause__).__name__)
