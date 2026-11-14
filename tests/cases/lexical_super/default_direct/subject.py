# 默认模式保留既有直写零参路径，不添加实例模块选项。
class Base:
    def label(self):
        return 'base'
class Child(Base):
    def label(self):
        return 'child:' + super().label()
