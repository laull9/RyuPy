# 嵌套调用使用自己的位置形参，而非外层方法接收者。
class Base:
    def identify(self):
        return self
class Child(Base):
    def functions(self):
        def own(other):
            return super().identify()
        def absent():
            return super().identify()
        def keyword_only(*, other):
            return super().identify()
        def variadic(*other):
            return super().identify()
        return own, absent, keyword_only, variadic
