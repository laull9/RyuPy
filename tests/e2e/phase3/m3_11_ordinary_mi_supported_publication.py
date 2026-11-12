# ryupy: pending
# 有界支持域独立用例；不覆盖原始反射/直接raise/globals红项。
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
    missing_error = [type(exc).__name__, str(exc), events]
try:
    Missing
    missing_published = True
except NameError:
    missing_published = False
print("mi_unbound_self", repr(missing_error + [missing_published]))
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
