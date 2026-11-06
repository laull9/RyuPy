# ryupy: pending
# genexpr 自己的隐藏首参是迭代器，不能借外层 self。
alias = super
class Base:
    pass
class Child(Base):
    def values(self):
        return ((__class__, alias()) for unused in [0])
