# 首参必须是当前绑定，callee 求值先于读取 own cell。
events = []
class Base:
    def identify(self):
        return self
class Child(Base):
    def rebound(self, other):
        self = other
        return super().identify()
    def own_cell(self, other):
        def replace():
            nonlocal self
            self = other
        replace()
        return super().identify()
    def callee_first(self, other):
        def select():
            nonlocal self
            events.append('select')
            self = other
            return super
        return select()().identify()
    def positional_only(receiver, /, other):
        receiver = other
        return super().identify()
