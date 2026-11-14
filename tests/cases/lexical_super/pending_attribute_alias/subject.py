# ryupy: pending
# 属性解析后取得 canonical 对象仍不属于本切值调用入口。
class Holder:
    pass
holder = Holder()
holder.alias = super
class Base:
    def identify(self):
        return self
class Child(Base):
    def identify(self):
        __class__
        return holder.alias().identify()
