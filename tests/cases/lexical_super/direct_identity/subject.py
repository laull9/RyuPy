# 普通继承、未绑定调用和真实 super 类型身份。
canonical = super
class Base:
    def identify(self):
        return self
    def label(self):
        return 'base'
class Child(Base):
    def identify(self):
        return super().identify()
    def proxy(self):
        return super()
    def label(self):
        return 'child:' + super().label()
class Leaf(Child):
    pass
saved = Child.identify
