# 显式类 cell 的独立合同；若 Legacy 尚未开放，不混入正例成绩。
from bases import Diamond
old = object()
Child = old
events = []
def decorate(cls):
    events.append((Child is old, cls().owner() is cls))
    return cls
@decorate
class Child(Diamond):
    def owner(self):
        return __class__
    def chain(self):
        return ['Child'] + super().chain()
print(events, Child().owner() is Child, Child().chain())
