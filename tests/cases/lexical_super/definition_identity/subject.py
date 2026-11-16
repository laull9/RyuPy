# 导出真实旧方法供双实例及同字典重执行 Host 使用，不实现假 loader。
marker = 'initial'
class Base:
    def identify(self):
        return self
class Child(Base):
    def identify(self):
        return super().identify(), __class__, marker
class Leaf(Child):
    pass
class Foreign(Base):
    pass
original = Child
saved = Child.identify
Leaf.transplanted = saved
Foreign.transplanted = saved
Child.__module__ = 'changed class module'
saved.__module__ = 'changed function module'
Child = Foreign
