# ryupy: pending
# 实例局部删除预检仍拒绝，不借 super 扩张 del 支持域。
class Base:
    pass
class Child(Base):
    def deleted(self):
        del self
        return super()
