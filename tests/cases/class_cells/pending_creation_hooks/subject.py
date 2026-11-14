events = []
class Descriptor:
    def __set_name__(self, owner, name):
        events.append(('descriptor', owner.ident(None) is owner))
class Base:
    def __init_subclass__(cls):
        events.append(('parent', cls.ident(None) is cls))
class Child(Base):
    descriptor = Descriptor()
    def ident(self):
        return __class__
