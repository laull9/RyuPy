# 全局别名不会凭接收者补出词法类 cell。
alias = super
class Base:
    def identify(self):
        return self
class Child(Base):
    def local(self):
        selected = super
        return selected().identify()
    def global_with_cell(self):
        __class__
        return alias().identify()
    def global_without_cell(self):
        return alias().identify()
    def expression(self):
        return (super if True else None)().identify()
