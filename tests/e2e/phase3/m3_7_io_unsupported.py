# io.UnsupportedOperation 同时属于 OSError/ValueError，按源码分支顺序捕获。
import io
from io import UnsupportedOperation as Unsupported

print(issubclass(Unsupported, OSError), issubclass(Unsupported, ValueError))
error = Unsupported("blocked")
print(isinstance(error, OSError), isinstance(error, ValueError), str(error), error.args)
for value in [error, ValueError("value"), OSError("os")]:
    try:
        raise value
    except Unsupported:
        print("unsupported")
    except ValueError:
        print("value")
    except OSError:
        print("os")
try:
    raise Unsupported("first")
except ValueError:
    print("value first")
except OSError:
    print("wrong")
try:
    raise Unsupported("second")
except OSError:
    print("os first")
except ValueError:
    print("wrong")
class Child(Unsupported):
    pass
for base in [OSError, ValueError]:
    print(issubclass(Child, base), isinstance(Child("child"), base))
try:
    raise Child("child")
except ValueError:
    print("child value")
def generator():
    yield 1
    try:
        raise Child("child")
    except ValueError:
        yield 2
print(list(generator()))
for stream in [io.StringIO(), io.BytesIO()]:
    try:
        stream.fileno()
    except Unsupported as caught:
        print(str(caught), isinstance(caught, ValueError), isinstance(caught, OSError))
try:
    raise Child("tuple")
except (ValueError, ValueError, Unsupported):
    print("tuple once")
try:
    raise Child("os")
except OSError:
    print("child os")
def injected():
    try:
        yield "ready"
    except ValueError:
        yield "handled"
iterator = injected()
print(next(iterator), iterator.throw(Child("injected")))
