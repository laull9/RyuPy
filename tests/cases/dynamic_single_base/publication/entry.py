# 单基也必须延后发布；类体和装饰器看到旧值，失败不得泄漏半成品。
from bases import Diamond
old = object()
Target = old
events = []
def decorate(cls):
    events.append(('decorator', Target is old, cls.seen is old))
    return cls
@decorate
class Target(Diamond):
    seen = Target
    events.append(('body', Target is old))
print(events, Target is old, Target.seen is old, Target().chain())

Target = old
events = []
def fail_body():
    raise ValueError('body-failed')
try:
    class Target(Diamond):
        events.append('body')
        fail_body()
except ValueError as exc:
    print(str(exc), Target is old, events)

def fail_decorator(cls):
    events.append(('decorator', Target is old))
    raise ValueError('decorator-failed')
events = []
try:
    @fail_decorator
    class Target(Diamond):
        events.append('body')
except ValueError as exc:
    print(str(exc), Target is old, events)

# 原来无绑定时，类体读取自身应失败，之后仍然未绑定。
try:
    class Missing(Diamond):
        seen = Missing
except NameError:
    print('body name missing')
try:
    Missing
except NameError:
    print('name remains missing')

# 无效实际基值在执行类体之前失败，同样保持旧名。
InvalidBase = 42
events = []
try:
    class Target(InvalidBase):
        events.append('unreachable')
except TypeError:
    print('invalid base', Target is old, events)

# 下标求值本身抛错也不得发布或开始执行类体。
class FailingSelector:
    def __getitem__(self, key):
        events.append(('base', key))
        raise ValueError('base-failed')
selector = FailingSelector()
try:
    class Target(selector['bad']):
        events.append('unreachable')
except ValueError as exc:
    print(str(exc), Target is old, events)
