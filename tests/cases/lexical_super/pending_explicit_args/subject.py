# ryupy: pending
# 显式一参未绑定代理与二参构造仍是独立能力边界。
class Base:
    def identify(self):
        return self
class Child(Base):
    def one(self):
        return super(Child)
    def two(self):
        return super(Child, self).identify()
