# ryupy: pending
# 即使展开后为空，源码星号调用仍不进入 S1 零参入口。
class Base:
    def identify(self):
        return self
class Child(Base):
    def star(self):
        return super(*()).identify()
    def keywords(self):
        return super(**{}).identify()
