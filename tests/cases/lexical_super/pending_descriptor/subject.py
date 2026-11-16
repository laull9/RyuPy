# ryupy: pending
# 任意 __get__ 需真实传入 receiver 及其类型，不是返回原属性。
class Descriptor:
    def __get__(self, instance, owner):
        return instance, owner
class Base:
    value = Descriptor()
class Child(Base):
    def read(self):
        return super().value
