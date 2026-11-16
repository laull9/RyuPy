# 上下文隐式协议绕过实例覆盖；装饰器公开重建入口保持副作用和异常。
from contextlib import contextmanager
from importlib.resources import as_file
from pathlib import Path

path = Path("missing")
cm = as_file(path)
cm.__enter__ = lambda: 99
cm.__exit__ = lambda *args: True
print("explicit", cm.__enter__(), cm.__exit__(None, None, None))
with cm as value:
    print("implicit", value is path)
print("entered", hasattr(cm, "args"))

trace = []
@contextmanager
def resource():
    trace.append("enter")
    try:
        yield path
    finally:
        trace.append("close")

cm = resource()
cm.__enter__ = lambda: 99
cm.__exit__ = lambda *args: True
error = ValueError("body")
try:
    with cm as value:
        print("exception", value is path)
        raise error
except ValueError as caught:
    print("identity", caught is error)
print(trace)

class Manager:
    def __enter__(self):
        trace.append("class enter")
        return 42
    def __exit__(self, typ, value, tb):
        trace.append("class exit")
        return False
m = Manager()
m.__enter__ = lambda: 0
m.__exit__ = lambda *args: True
with m as value:
    print("class", value)
print(trace)

cm = as_file(path)
def recreate():
    trace.append("recreate")
    replacement = resource()
    replacement.__enter__ = lambda: 0
    replacement.__exit__ = lambda *args: True
    return replacement
cm._recreate_cm = recreate
@cm
def body(fail):
    trace.append("body")
    if fail:
        raise error
    return 7
print("decorated", body(False))
try:
    body(True)
except ValueError as caught:
    print("decorated error", caught is error)
print(trace)
def failed_recreate():
    trace.append("failed recreate")
    raise error
cm._recreate_cm = failed_recreate
try:
    body(False)
except ValueError as caught:
    print("recreate error", caught is error)
print(trace)

# 类型描述符绑定顺序，以及进入前固定的退出方法。
class Described:
    @property
    def __enter__(self):
        trace.append("bind enter")
        return lambda: 5
    @property
    def __exit__(self):
        trace.append("bind exit")
        return lambda *args: False
with Described() as value:
    print("descriptor", value)
print(trace[-2:])

# 缺协议时不执行body，不向实例__getattr__兜底。
class Missing:
    def __getattr__(self, name):
        trace.append("unexpected getattr")
        return lambda *args: True
try:
    with Missing():
        print("unexpected body")
except TypeError as caught:
    print(str(caught))
