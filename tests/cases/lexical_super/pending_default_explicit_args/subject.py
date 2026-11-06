# ryupy: pending
# 默认旧调用路径也不得静默忽略显式位置实参。
class Base:
    def label(self):
        return 'base'
class Middle(Base):
    def label(self):
        return 'middle'
class Child(Middle):
    def label(self):
        return super(Middle, self).label()
