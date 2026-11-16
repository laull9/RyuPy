# ryupy: pending
# CP 的 __class__ 代理回退不属于普通真实实例检查。
class Base:
    def identify(self):
        return self
class Child(Base):
    def identify(self):
        return super().identify()
class Proxy:
    @property
    def __class__(self):
        return Child
