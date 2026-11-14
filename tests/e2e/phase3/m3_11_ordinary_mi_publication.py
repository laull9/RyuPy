# ryupy: pending
# 仅静态普通MI类名发布：不改旧普通类域。
class A:
    pass
class B:
    pass
events = []
try:
    class Missing(A, B):
        events.append("body")
        seen = Missing
except NameError as exc:
    print("mi_unbound_self", repr([type(exc).__name__, str(exc), events, "Missing" in globals()]))
old = object()
Target = old
events = []
def decorate(cls):
    events.append(("decorator", Target is old, cls.seen is old))
    return cls
@decorate
class Target(A, B):
    seen = Target
    events.append(("body", Target is old))
print("mi_old_binding_until_publish", repr([events, Target is old, Target.seen is old]))
