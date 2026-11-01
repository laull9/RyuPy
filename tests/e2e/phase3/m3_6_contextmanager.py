# M3.6：生成器上下文必须保留异常身份及清理顺序。
from contextlib import contextmanager

@contextmanager
def resource(suppress=False):
    print("enter")
    try:
        yield 7
    except ValueError:
        print("caught")
        if not suppress:
            raise
    finally:
        print("exit")

with resource() as value:
    print(value)
with resource(True):
    raise ValueError("hidden")
error = ValueError("same")
try:
    with resource():
        raise error
except ValueError as caught:
    print(caught is error)
try:
    with resource():
        raise StopIteration("body")
except StopIteration:
    print("body stop preserved")
@contextmanager
def empty():
    if False:
        yield 1
@contextmanager
def twice():
    try:
        yield 1
        yield 2
    finally:
        print("twice cleanup")
for factory in [empty, twice]:
    try:
        with factory():
            pass
    except RuntimeError:
        print("invalid yield count")
@resource()
def decorated():
    print("decorated")
decorated()
decorated()
