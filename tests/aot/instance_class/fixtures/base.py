"""Legacy父类与真实hook事件；不作为Instance准入源。"""
events = []
class Base:
    def inherited(self):
        return 5
class Descriptor:
    def __set_name__(self, owner, name):
        events.append('set-name')
def hook(cls):
    events.append('init-subclass')
def mark(name, fail):
    events.append(name)
    if fail:
        raise ValueError(name)
    return 7
def choose(fail):
    mark('base', fail)
    return Base
def descriptor(enabled):
    mark('field', False)
    if enabled:
        return Descriptor()
    return 9
