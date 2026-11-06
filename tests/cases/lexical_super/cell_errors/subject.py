# 同名参数、局部、global 及 own cell 不能假装真实 free cell。
alias = super
__class__ = 'module class value'
class Base:
    pass
class Child(Base):
    def local(self):
        __class__ = Base
        return super()
    def parameter(self, __class__):
        return super()
    def global_value(self):
        global __class__
        return super()
    def own_cell(self):
        __class__ = Base
        def capture():
            return __class__
        capture()
        return super()
def no_arguments():
    return alias()
def missing_cell(value):
    return alias()
