# ryupy: pending
# 无基类同名属性时回退到代理字段；有同名属性时先按 super 查找。
class Base:
    pass
class Child(Base):
    def proxy(self):
        return super()
class ShadowBase:
    __thisclass__ = 'base start'
    __self__ = 'base receiver'
    __self_class__ = 'base receiver class'
    __class__ = 'base class attribute'
class ShadowChild(ShadowBase):
    def proxy(self):
        return super()
