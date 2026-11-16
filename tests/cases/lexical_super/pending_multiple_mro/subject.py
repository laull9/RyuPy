# ryupy: pending
# 多继承查找应遵循实际接收者 MRO，不能只取定义类父链。
class Base:
    def label(self):
        return 'base'
class Left(Base):
    def label(self):
        return 'left:' + super().label()
class Right(Base):
    def label(self):
        return 'right:' + super().label()
class Child(Left, Right):
    pass
