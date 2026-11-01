# M3.6：ContextDecorator 的 C# 异常捕获必须建立与 with 相同的当前异常上下文。
from contextlib import contextmanager

@contextmanager
def invalid():
    try:
        yield
    except ValueError:
        yield

body_error = ValueError("body")
@invalid()
def body():
    raise body_error

try:
    body()
except RuntimeError as error:
    print(str(error), error.__context__ is body_error)

@contextmanager
def cleanup_failure():
    try:
        yield
    finally:
        raise TypeError("cleanup")

@cleanup_failure()
def failing_body():
    raise body_error

outer = KeyError("outer")
try:
    raise outer
except KeyError:
    try:
        failing_body()
    except TypeError as error:
        print(str(error), error.__context__ is body_error)
    try:
        raise
    except KeyError as restored:
        print("restored", restored is outer)

@contextmanager
def invalid_cleanup():
    try:
        try:
            yield
        except ValueError:
            yield
    finally:
        raise TypeError("close cleanup")

@invalid_cleanup()
def invalid_body():
    raise body_error

try:
    invalid_body()
except TypeError as error:
    link = error.__context__
    print(type(link).__name__, type(link.__context__).__name__)
    print(link.__context__.__context__ is body_error)

@contextmanager
def no_yield():
    if False:
        yield
try:
    with no_yield():
        pass
except RuntimeError as error:
    print(type(error.__context__).__name__, error.__cause__ is None)
