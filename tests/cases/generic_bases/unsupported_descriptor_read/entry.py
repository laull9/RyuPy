# 独立追加公开入口审查：inherited_root_descriptor
from typing import Generic, TypeVar
T = TypeVar('T')
events = []
class Box(Generic[T]):
    pass
x = Box()
def changed(value):
    events.append('repr')
    return 'changed'
class Descriptor:
    def __get__(self, instance, owner):
        events.append('get')
        return changed
Generic.__repr__ = Descriptor()
try:
    print(Box.__repr__(x))
except Exception as error:
    print(type(error).__name__)
print(events)
