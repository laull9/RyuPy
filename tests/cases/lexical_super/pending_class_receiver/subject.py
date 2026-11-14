# ryupy: pending
# classmethod 的类对象接收者留待专门协议实现。
class Base:
    @classmethod
    def identify(cls):
        return cls
class Child(Base):
    @classmethod
    def identify(cls):
        return super().identify()
