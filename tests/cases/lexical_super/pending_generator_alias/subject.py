# ryupy: pending
# 生成器用 alias 也不得绕过 frame 能力边界。
alias = super
class Base:
    def identify(self):
        return self
class Child(Base):
    def values(self):
        __class__
        yield alias().identify()
